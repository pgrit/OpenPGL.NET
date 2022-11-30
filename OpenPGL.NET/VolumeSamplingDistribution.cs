namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseVolumeSamplingDistribution(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector3 pglVolumeSamplingDistributionSample(IntPtr handle, Vector2 sample2D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglVolumeSamplingDistributionPDF(IntPtr handle, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool pglVolumeSamplingDistributionIsValid(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglVolumeSamplingDistributionClear(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglFieldNewVolumeSamplingDistribution(IntPtr field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool pglFieldInitVolumeSamplingDistribution(IntPtr field, IntPtr volumeSamplingDistribution,
        Vector3 position, ref float sample1D);
}

public class VolumeSamplingDistribution : IDisposable {
    IntPtr handle;
    Field field;

    public VolumeSamplingDistribution(Field field) {
        this.field = field;
        handle = OpenPGL.pglFieldNewVolumeSamplingDistribution(field.Handle);
    }

    public void Dispose() {
        if (handle != IntPtr.Zero) {
            OpenPGL.pglReleaseVolumeSamplingDistribution(handle);
            handle = IntPtr.Zero;
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
