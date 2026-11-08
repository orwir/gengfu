using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Gengfu.App.Speech;

/// <summary>
/// Windows' speech API, Windows.Media.SpeechSynthesis, called through its documented interfaces. Its
/// voices are the ones Windows' speech settings install, on Windows 10 and later. Call it off the UI
/// thread.
/// </summary>
internal static unsafe class WindowsSpeech
{
    private const string Synthesizer = "Windows.Media.SpeechSynthesis.SpeechSynthesizer";
    private const int Started = 0;
    private const int Completed = 1;

    // IActivationFactory: 6 ActivateInstance.
    private static readonly Guid ActivationFactory = new("00000035-0000-0000-C000-000000000046");

    // ISpeechSynthesizer: 6 SynthesizeTextToStreamAsync, 8 put_Voice.
    private static readonly Guid SpeechSynthesizer = new("CE9F7C76-97F4-4CED-AD68-D51C458E45C6");

    // IInstalledVoicesStatic: 6 AllVoices, 7 DefaultVoice. The list, IVectorView: 6 GetAt, 7 Size.
    // IVoiceInformation: 6 DisplayName, 7 Id, 8 Language.
    private static readonly Guid InstalledVoicesStatic = new("7D526ECC-7533-4C3F-85BE-888C2BAEEBDC");

    // IAsyncInfo: 7 Status, 8 ErrorCode, 9 Cancel, 10 Close. IAsyncOperation: 8 GetResults.
    private static readonly Guid AsyncInfo = new("00000036-0000-0000-C000-000000000046");

    // IStream, read through ISequentialStream: 3 Read.
    private static readonly Guid Stream = new("0000000C-0000-0000-C000-000000000046");

    /// <summary>How long Windows gets to speak a line before the attempt counts as failed.</summary>
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    /// <summary>The installed voices, Windows' default first. Throws when Windows has no speech.</summary>
    public static IReadOnlyList<Voice> Voices()
    {
        var statics = Factory(InstalledVoicesStatic);
        try
        {
            var defaultId = DefaultVoiceId(statics);
            var voices = new List<Voice>();
            ForEachVoice(statics, voice =>
            {
                voices.Add(new Voice(Text(voice, 7), Text(voice, 6), Text(voice, 8)));
                return false;
            });
            return DefaultFirst(voices, defaultId);
        }
        finally
        {
            Interop.Release(statics);
        }
    }

    /// <summary>The voices with Windows' default moved first; the rest keep Windows' order.</summary>
    internal static IReadOnlyList<Voice> DefaultFirst(IReadOnlyList<Voice> voices, string? defaultId) =>
        [.. voices.OrderBy(voice => voice.Id == defaultId ? 0 : 1)];

    /// <summary>Speaks the text in the voice with this id, into a WAV of 16-bit PCM at the voice's own rate.</summary>
    public static byte[] Speak(string text, string voiceId)
    {
        var voice = FindVoice(voiceId);
        var synthesizer = IntPtr.Zero;
        try
        {
            synthesizer = CreateSynthesizer();
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, int>)Interop.Slot(synthesizer, 8))(synthesizer, voice), "set the voice");
            var line = HString(text);
            IntPtr operation;
            int started;
            try
            {
                started = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr*, int>)Interop.Slot(synthesizer, 6))(synthesizer, line, &operation);
            }
            finally
            {
                Interop.WindowsDeleteString(line);
            }
            Interop.Check(started, "start speaking");
            var stream = Await(operation);
            try
            {
                return ReadAll(stream);
            }
            finally
            {
                Interop.Release(stream);
            }
        }
        finally
        {
            Interop.Release(synthesizer);
            Interop.Release(voice);
        }
    }

    private static IntPtr FindVoice(string voiceId)
    {
        var statics = Factory(InstalledVoicesStatic);
        try
        {
            var found = IntPtr.Zero;
            ForEachVoice(statics, voice =>
            {
                if (Text(voice, 7) != voiceId)
                {
                    return false;
                }
                Marshal.AddRef(voice);
                found = voice;
                return true;
            });
            return found != IntPtr.Zero ? found : throw new ArgumentException($"No installed voice has the id {voiceId}.", nameof(voiceId));
        }
        finally
        {
            Interop.Release(statics);
        }
    }

    // Visits each voice until the visit returns true; a voice outlives the visit only if it adds a reference.
    private static void ForEachVoice(IntPtr statics, Func<IntPtr, bool> visit)
    {
        IntPtr list;
        Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(statics, 6))(statics, &list), "list the voices");
        try
        {
            uint count;
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint*, int>)Interop.Slot(list, 7))(list, &count), "count the voices");
            for (uint index = 0; index < count; index++)
            {
                IntPtr voice;
                Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)Interop.Slot(list, 6))(list, index, &voice), "read a voice");
                try
                {
                    if (visit(voice))
                    {
                        return;
                    }
                }
                finally
                {
                    Interop.Release(voice);
                }
            }
        }
        finally
        {
            Interop.Release(list);
        }
    }

    // Windows may have no default voice to give, such as when no voice is installed.
    private static string? DefaultVoiceId(IntPtr statics)
    {
        IntPtr voice;
        if (((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(statics, 7))(statics, &voice) < 0 || voice == IntPtr.Zero)
        {
            return null;
        }
        try
        {
            return Text(voice, 7);
        }
        finally
        {
            Interop.Release(voice);
        }
    }

    private static IntPtr CreateSynthesizer()
    {
        var factory = Factory(ActivationFactory);
        var instance = IntPtr.Zero;
        try
        {
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(factory, 6))(factory, &instance), "create a synthesizer");
            return Interop.QueryInterface(instance, SpeechSynthesizer);
        }
        finally
        {
            Interop.Release(instance);
            Interop.Release(factory);
        }
    }

    // The async call completes on Windows' own threads; polling it keeps the wait simple and bounded.
    private static IntPtr Await(IntPtr operation)
    {
        var info = IntPtr.Zero;
        try
        {
            info = Interop.QueryInterface(operation, AsyncInfo);
            var waited = Stopwatch.StartNew();
            while (true)
            {
                int status;
                Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Interop.Slot(info, 7))(info, &status), "check on the speech");
                if (status == Completed)
                {
                    break;
                }
                if (status != Started)
                {
                    int error;
                    _ = ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)Interop.Slot(info, 8))(info, &error);
                    throw new COMException($"Couldn't speak the line: {Interop.Describe(error)}", error);
                }
                if (waited.Elapsed > Patience)
                {
                    _ = ((delegate* unmanaged[Stdcall]<IntPtr, int>)Interop.Slot(info, 9))(info);
                    throw new TimeoutException("Windows took too long to speak the line.");
                }
                Thread.Sleep(2);
            }
            IntPtr result;
            Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(operation, 8))(operation, &result), "take the speech");
            return result;
        }
        finally
        {
            if (info != IntPtr.Zero)
            {
                _ = ((delegate* unmanaged[Stdcall]<IntPtr, int>)Interop.Slot(info, 10))(info);
            }
            Interop.Release(info);
            Interop.Release(operation);
        }
    }

    private static byte[] ReadAll(IntPtr randomAccessStream)
    {
        Interop.Check(Interop.CreateStreamOverRandomAccessStream(randomAccessStream, Stream, out var stream), "open the speech");
        try
        {
            using var output = new MemoryStream();
            var buffer = new byte[64 * 1024];
            while (true)
            {
                uint read;
                fixed (byte* start = buffer)
                {
                    Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, byte*, uint, uint*, int>)Interop.Slot(stream, 3))(stream, start, (uint)buffer.Length, &read), "read the speech");
                }
                if (read == 0)
                {
                    return output.ToArray();
                }
                output.Write(buffer, 0, (int)read);
            }
        }
        finally
        {
            Interop.Release(stream);
        }
    }

    private static IntPtr Factory(Guid iid)
    {
        var name = HString(Synthesizer);
        try
        {
            Interop.Check(Interop.RoGetActivationFactory(name, iid, out var factory), "reach Windows speech");
            return factory;
        }
        finally
        {
            Interop.WindowsDeleteString(name);
        }
    }

    private static IntPtr HString(string text)
    {
        Interop.Check(Interop.WindowsCreateString(text, text.Length, out var hstring), "make a string");
        return hstring;
    }

    private static string Text(IntPtr instance, int slot)
    {
        IntPtr hstring;
        Interop.Check(((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)Interop.Slot(instance, slot))(instance, &hstring), "read a string");
        try
        {
            var chars = Interop.WindowsGetStringRawBuffer(hstring, out var length);
            return new string(chars, 0, (int)length);
        }
        finally
        {
            Interop.WindowsDeleteString(hstring);
        }
    }
}
