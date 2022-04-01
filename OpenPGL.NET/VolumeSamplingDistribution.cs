namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglNewVolumeSamplingDistribution();

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
    public static extern void pglVolumeSamplingDistributionInit(IntPtr handle, IntPtr regionHandle,
        Vector3 samplePosition, [MarshalAs(UnmanagedType.I1)] bool useParallaxComp);
}

public class VolumeSamplingDistribution : IDisposable {
    IntPtr handle;

    public VolumeSamplingDistribution() {
        handle = OpenPGL.pglNewVolumeSamplingDistribution();
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

    public void Init(Region region, Vector3 pos, bool useParallaxCompensation = true)
    => OpenPGL.pglVolumeSamplingDistributionInit(handle, region.Handle, pos, useParallaxCompensation);

    ~VolumeSamplingDistribution() => Dispose();
}
