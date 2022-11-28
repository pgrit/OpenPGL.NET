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
        Vector3 samplePosition, ref float sample1D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionApplyCosineProduct(IntPtr handle, Vector3 normal);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglSurfaceSamplingGetRegion(IntPtr handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionLobePDF(IntPtr surfaceSamplingDistribution,
        UIntPtr i, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UIntPtr pglSurfaceSamplingDistributionNumLobes(IntPtr surfaceSamplingDistribution);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionGetLobeWeight(IntPtr surfaceSamplingDistribution,
        UIntPtr i);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionSetLobeWeight(IntPtr surfaceSamplingDistribution,
        UIntPtr i, float weight);
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

    public void Init(Vector3 pos, ref float sample1D)
    => OpenPGL.pglFieldInitSurfaceSamplingDistribution(field.Handle, handle, pos, ref sample1D);

    public void ApplyCosineProduct(Vector3 normal)
    => OpenPGL.pglSurfaceSamplingDistributionApplyCosineProduct(handle, normal);

    public Region Region => new(OpenPGL.pglSurfaceSamplingGetRegion(handle));

    ~SurfaceSamplingDistribution() => Dispose();

    public int NumLobes => (int) OpenPGL.pglSurfaceSamplingDistributionNumLobes(handle);

    public float ComputeLobePdf(int lobeIdx, in Vector3 direction)
    => OpenPGL.pglSurfaceSamplingDistributionLobePDF(handle, new((uint)lobeIdx), direction);

    public float[] LobeWeights {
        get {
            float[] weights = new float[NumLobes];
            for (int i = 0; i < weights.Length; ++i)
                weights[i] = OpenPGL.pglSurfaceSamplingDistributionGetLobeWeight(handle, (UIntPtr)i);
            return weights;
        }
        set {
            Debug.Assert(value.Length == NumLobes, "must set all lobe weights");
            for (int i = 0; i < value.Length; ++i)
                OpenPGL.pglSurfaceSamplingDistributionSetLobeWeight(handle, (UIntPtr)i, value[i]);
        }
    }
}
