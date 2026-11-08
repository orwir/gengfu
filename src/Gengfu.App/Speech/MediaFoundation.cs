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

    /// <summary>
    /// The file as 48 kHz stereo, interleaved, decoded up to a little past <paramref name="limit"/>.
    /// Throws when Windows can't read it.
    /// </summary>
    public static float[] Read(string path, TimeSpan limit)
    {
        Interop.EnterMultithreadedApartment();
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
    private static float[] ReadSamples(IntPtr reader, int most)
    {
        var samples = new List<float>();
        while (samples.Count < most)
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
                    Append(sample, samples);
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
                break;
            }
        }
        return [.. samples];
    }

    private static void Append(IntPtr sample, List<float> samples)
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
                samples.AddRange(new ReadOnlySpan<float>(data, length / sizeof(float)));
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
