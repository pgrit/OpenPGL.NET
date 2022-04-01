namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglNewSampleStorage();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseSampleStorage(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSample(IntPtr handle, in SampleData sample);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSamples(IntPtr handle, [In] SampleData[] samples, UIntPtr num);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSamples(IntPtr handle, IntPtr samples, UIntPtr num);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageReserve(IntPtr handle, UIntPtr sizeSurface, UIntPtr sizeVolume);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageClear(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglSampleStorageGetSizeSurface(IntPtr sampleStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglSampleStorageGetSizeVolume(IntPtr sampleStorage);
}

public class SampleStorage : IDisposable {
    internal IntPtr Handle;

    public SampleStorage() {
        Handle = OpenPGL.pglNewSampleStorage();
        Debug.Assert(Handle != IntPtr.Zero);
    }

    public void Dispose() {
        if (Handle != IntPtr.Zero) {
            OpenPGL.pglReleaseSampleStorage(Handle);
            Handle = IntPtr.Zero;
        }
    }

    ~SampleStorage() => Dispose();

    public void AddSample(SampleData sample) => OpenPGL.pglSampleStorageAddSample(Handle, in sample);

    public void AddSamples(SampleData[] samples)
    => OpenPGL.pglSampleStorageAddSamples(Handle, samples, new((uint)samples.Length));

    public void AddSamples(IntPtr samples, uint num)
    => OpenPGL.pglSampleStorageAddSamples(Handle, samples, new(num));

    public void Reserve(uint sizeSurface, uint sizeVolume)
    => OpenPGL.pglSampleStorageReserve(Handle, new(sizeSurface), new(sizeVolume));

    public void Clear() => OpenPGL.pglSampleStorageClear(Handle);

    public uint SizeSurface => OpenPGL.pglSampleStorageGetSizeSurface(Handle);
    public uint SizeVolume => OpenPGL.pglSampleStorageGetSizeVolume(Handle);
}