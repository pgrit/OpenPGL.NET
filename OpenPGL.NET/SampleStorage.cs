namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglNewSampleStorage();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseSampleStorage(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSample(nint handle, in SampleData sample);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSamples(nint handle, [In] SampleData[] samples, nuint num);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageAddSamples(nint handle, nint samples, nuint num);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageReserve(nint handle, nuint sizeSurface, nuint sizeVolume);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSampleStorageClear(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglSampleStorageGetSizeSurface(nint sampleStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglSampleStorageGetSizeVolume(nint sampleStorage);
}

public class SampleStorage : IDisposable {
    internal nint Handle;

    public SampleStorage() {
        Handle = OpenPGL.pglNewSampleStorage();
        Debug.Assert(Handle != nint.Zero);
    }

    public void Dispose() {
        if (Handle != nint.Zero) {
            OpenPGL.pglReleaseSampleStorage(Handle);
            Handle = nint.Zero;
        }
    }

    ~SampleStorage() => Dispose();

    public void AddSample(SampleData sample) => OpenPGL.pglSampleStorageAddSample(Handle, in sample);

    public void AddSamples(SampleData[] samples)
    => OpenPGL.pglSampleStorageAddSamples(Handle, samples, new((uint)samples.Length));

    public void AddSamples(nint samples, uint num)
    => OpenPGL.pglSampleStorageAddSamples(Handle, samples, new(num));

    public void Reserve(uint sizeSurface, uint sizeVolume)
    => OpenPGL.pglSampleStorageReserve(Handle, new(sizeSurface), new(sizeVolume));

    public void Clear() => OpenPGL.pglSampleStorageClear(Handle);

    public uint SizeSurface => OpenPGL.pglSampleStorageGetSizeSurface(Handle);
    public uint SizeVolume => OpenPGL.pglSampleStorageGetSizeVolume(Handle);
}