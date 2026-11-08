using System.Runtime.InteropServices;

namespace Gengfu.App.Speech;

/// <summary>
/// The Windows functions behind speech and audio files. COM and WinRT methods are called through
/// their slot in the object's method table, in the order Windows' documented headers give, so the
/// 24 MB C# projection of the whole Windows API doesn't ship (ADR-0005). Slots 0–2 are IUnknown's;
/// on WinRT objects 3–5 are IInspectable's.
/// </summary>
internal static unsafe partial class Interop
{
    private const uint CoinitMultithreaded = 0;

    public static IntPtr Slot(IntPtr instance, int slot) => (*(IntPtr**)instance)[slot];

    public static void Check(int result, string what)
    {
        if (result < 0)
        {
            throw new COMException(what, result);
        }
    }

    /// <summary>
    /// Joins the calling thread to COM's multithreaded apartment, which WinRT and Media Foundation
    /// need: in a WPF app no thread is in it by default. A thread already in an apartment stays put.
    /// </summary>
    public static void EnterMultithreadedApartment() => _ = CoInitializeEx(IntPtr.Zero, CoinitMultithreaded);

    public static IntPtr QueryInterface(IntPtr instance, Guid iid)
    {
        Check(Marshal.QueryInterface(instance, in iid, out var result), "QueryInterface " + iid);
        return result;
    }

    public static void Release(IntPtr instance)
    {
        if (instance != IntPtr.Zero)
        {
            Marshal.Release(instance);
        }
    }

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(IntPtr reserved, uint coInit);

    [LibraryImport("combase.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int WindowsCreateString(string source, int length, out IntPtr hstring);

    [LibraryImport("combase.dll")]
    public static partial int WindowsDeleteString(IntPtr hstring);

    [LibraryImport("combase.dll")]
    public static partial char* WindowsGetStringRawBuffer(IntPtr hstring, out uint length);

    [LibraryImport("combase.dll")]
    public static partial int RoGetActivationFactory(IntPtr activatableClassId, in Guid iid, out IntPtr factory);

    [LibraryImport("shcore.dll")]
    public static partial int CreateStreamOverRandomAccessStream(IntPtr randomAccessStream, in Guid iid, out IntPtr stream);

    [LibraryImport("mfplat.dll")]
    public static partial int MFStartup(uint version, uint flags);

    [LibraryImport("mfplat.dll")]
    public static partial int MFShutdown();

    [LibraryImport("mfplat.dll")]
    public static partial int MFCreateMediaType(out IntPtr mediaType);

    [LibraryImport("mfplat.dll")]
    public static partial int MFInitMediaTypeFromWaveFormatEx(IntPtr mediaType, in WaveFormatEx format, uint size);

    [LibraryImport("mfreadwrite.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MFCreateSourceReaderFromURL(string url, IntPtr attributes, out IntPtr reader);

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct WaveFormatEx
    {
        public ushort FormatTag;
        public ushort Channels;
        public uint SamplesPerSec;
        public uint AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public ushort ExtraSize;
    }
}
