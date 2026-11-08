using System.IO;

namespace Gengfu.App.Speech;

/// <summary>
/// Reads audio files through Media Foundation, Windows' media stack: MP3, WAV, AAC, WMA, FLAC and
/// whatever else Windows plays. Windows N editions lack it until the Media Feature Pack is added,
/// and Windows Server until its feature is. Call it off the UI thread.
/// </summary>
internal static unsafe class MediaFoundation
{
    private const uint Version = 0x00020070;
    private const uint FilesOnly = 0x1;
    private const uint AllStreams = 0xFFFFFFFE;
    private const uint FirstAudioStream = 0xFFFFFFFD;
    private const uint ReadFailed = 0x1;
    private const uint EndOfStream = 0x2;

    // Samples under -80 dBFS before the sound starts are silence, skipped for up to a minute, so the
    // limit counts from where the sound starts. Sound that only starts later is no call.
    private const float Silent = 0.0001f;
    private static readonly int MaxLeadIn = 60 * Pcm.Rate * 2;

    /// <summary>
    /// The file as 48 kHz stereo, interleaved, from where its sound starts to a little past
    /// <paramref name="limit"/>, and whether the file goes on past that. Throws when Windows can't read it.
    /// </summary>
    public static (float[] Samples, bool Cut) Read(string path, TimeSpan limit)
    {
        Interop.Check(Interop.MFStartup(Version, FilesOnly), "start Media Foundation");
        try
        {
            Interop.Check(Interop.MFCreateSourceReaderFromURL(Path.GetFullPath(path), IntPtr.Zero, out var reader), "open the file");
            try
            {
                AskForFloatStereo(reader);
                return ReadSamples(reader, 2 * ((int)(limit.TotalSeconds * Pcm.Rate) + Pcm.FrameSamples));
            }
            finally
            {
                Interop.Release(reader);
            }
        }
        finally
        {
            _ = Interop.MFShutdown();
        }
    }

    // IMFSourceReader: 4 SetStreamSelection, 7 SetCurrentMediaType. Media Foundation decodes and
    // resamples to the type asked for.
    private static void AskForFloatStereo(IntPtr reader)
    {
        Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint, int, int>)Interop.Slot(reader, 4))(reader, AllStreams, 0), "skip the other streams");
        Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint, int, int>)Interop.Slot(reader, 4))(reader, FirstAudioStream, 1), "find the audio");
        Interop.Check(Interop.MFCreateMediaType(out var type), "describe the output");
        try
        {
            var format = new Interop.WaveFormatEx
            {
                FormatTag = 3,
                Channels = 2,
                SamplesPerSec = Pcm.Rate,
                AvgBytesPerSec = Pcm.Rate * 8,
                BlockAlign = 8,
                BitsPerSample = 32,
            };
            Interop.Check(Interop.MFInitMediaTypeFromWaveFormatEx(type, format, (uint)sizeof(Interop.WaveFormatEx)), "describe the output");
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr, IntPtr, int>)Interop.Slot(reader, 7))(reader, FirstAudioStream, IntPtr.Zero, type), "convert the audio");
        }
        finally
        {
            Interop.Release(type);
        }
    }

    // IMFSourceReader: 9 ReadSample. IMFSample: 41 ConvertToContiguousBuffer. IMFMediaBuffer: 3 Lock, 4 Unlock.
    private static (float[] Samples, bool Cut) ReadSamples(IntPtr reader, int most)
    {
        var samples = new List<float>();
        var skipped = 0;
        while (true)
        {
            uint stream, flags;
            long time;
            IntPtr sample;
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint, uint, uint*, uint*, long*, IntPtr*, int>)Interop.Slot(reader, 9))(
                reader, FirstAudioStream, 0, &stream, &flags, &time, &sample), "read the audio");
            if (sample != IntPtr.Zero)
            {
                try
                {
                    skipped += Append(sample, samples);
                }
                finally
                {
                    Interop.Release(sample);
                }
            }
            if ((flags & ReadFailed) != 0)
            {
                throw new InvalidDataException("Windows stopped reading the file partway.");
            }
            if ((flags & EndOfStream) != 0)
            {
                return ([.. samples], false);
            }
            if (samples.Count >= most)
            {
                return ([.. samples], true);
            }
            if (samples.Count == 0 && skipped >= MaxLeadIn)
            {
                return ([], false);
            }
        }
    }

    // Adds the sample's audio, but none of the silence before the sound starts; returns how much it skipped.
    private static int Append(IntPtr sample, List<float> samples)
    {
        IntPtr buffer;
        Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(sample, 41))(sample, &buffer), "join the audio");
        try
        {
            byte* data;
            int capacity, length;
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, byte**, int*, int*, int>)Interop.Slot(buffer, 3))(buffer, &data, &capacity, &length), "lock the audio");
            try
            {
                var audio = new ReadOnlySpan<float>(data, length / sizeof(float));
                var start = 0;
                if (samples.Count == 0)
                {
                    // From the start of the stereo frame the sound starts in.
                    start = audio.IndexOfAnyExceptInRange(-Silent, Silent);
                    start = start < 0 ? audio.Length : start - (start % 2);
                }
                samples.AddRange(audio[start..]);
                return start;
            }
            finally
            {
                _ = ((delegate* unmanaged[Stdcall]<IntPtr, int>)Interop.Slot(buffer, 4))(buffer);
            }
        }
        finally
        {
            Interop.Release(buffer);
        }
    }
}
