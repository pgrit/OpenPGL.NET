namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglFieldNewSurfaceSamplingDistribution(IntPtr fieldHandle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseSurfaceSamplingDistribution(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector3 pglSurfaceSamplingDistributionSample(IntPtr handle, Vector2 sample2D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionPDF(IntPtr handle, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionClear(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool pglFieldInitSurfaceSamplingDistribution(IntPtr fieldHandle, IntPtr handle,
        Vector3 samplePosition, ref float sample1D, [MarshalAs(UnmanagedType.I1)] bool useParallaxComp);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionApplyCosineProduct(IntPtr handle, Vector3 normal);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglSurfaceSamplingGetRegion(IntPtr handle);
}

public class SurfaceSamplingDistribution : IDisposable {
    IntPtr handle;
    Field field;

    public SurfaceSamplingDistribution(Field field) {
        this.field = field;
        handle = OpenPGL.pglFieldNewSurfaceSamplingDistribution(field.Handle);
    }

    public void Dispose() {
        if (handle != IntPtr.Zero) {
            OpenPGL.pglReleaseSurfaceSamplingDistribution(handle);
            handle = IntPtr.Zero;
        }
    }

    public Vector3 Sample(Vector2 sample2D)
    => OpenPGL.pglSurfaceSamplingDistributionSample(handle, sample2D);

    public float PDF(Vector3 direction) {
        Common.AssertNormalized(direction);
        return OpenPGL.pglSurfaceSamplingDistributionPDF(handle, direction);
    }

    public void Clear() => OpenPGL.pglSurfaceSamplingDistributionClear(handle);

    public void Init(Vector3 pos, ref float sample1D, bool useParallaxCompensation = true)
    => OpenPGL.pglFieldInitSurfaceSamplingDistribution(field.Handle, handle, pos, ref sample1D,
        useParallaxCompensation);

    public void ApplyCosineProduct(Vector3 normal)
    => OpenPGL.pglSurfaceSamplingDistributionApplyCosineProduct(handle, normal);

    public Region Region => new(OpenPGL.pglSurfaceSamplingGetRegion(handle));

    ~SurfaceSamplingDistribution() => Dispose();
}
