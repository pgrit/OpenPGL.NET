namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseVolumeSamplingDistribution(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector3 pglVolumeSamplingDistributionSample(nint handle, Vector2 sample2D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglVolumeSamplingDistributionPDF(nint handle, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool pglVolumeSamplingDistributionIsValid(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglVolumeSamplingDistributionClear(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglFieldNewVolumeSamplingDistribution(nint field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool pglFieldInitVolumeSamplingDistribution(nint field, nint volumeSamplingDistribution,
        Vector3 position, ref float sample1D);
}

public class VolumeSamplingDistribution : IDisposable {
    nint handle;
    Field field;

    public VolumeSamplingDistribution(Field field) {
        this.field = field;
        handle = OpenPGL.pglFieldNewVolumeSamplingDistribution(field.Handle);
    }

    public void Dispose() {
        if (handle != nint.Zero) {
            OpenPGL.pglReleaseVolumeSamplingDistribution(handle);
            handle = nint.Zero;
        }
    }

    public Vector3 Sample(Vector2 sample2D)
    => OpenPGL.pglVolumeSamplingDistributionSample(handle, sample2D);

    public float PDF(Vector3 direction) {
        Common.AssertNormalized(direction);
        return OpenPGL.pglVolumeSamplingDistributionPDF(handle, direction);
    }

    public bool IsValid => OpenPGL.pglVolumeSamplingDistributionIsValid(handle);

    public void Clear() => OpenPGL.pglVolumeSamplingDistributionClear(handle);

    public void Init(Vector3 pos, ref float sample1D)
    => OpenPGL.pglFieldInitVolumeSamplingDistribution(field.Handle, handle, pos, ref sample1D);

    ~VolumeSamplingDistribution() => Dispose();
}
