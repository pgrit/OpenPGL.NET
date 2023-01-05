namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglFieldNewSurfaceSamplingDistribution(nint fieldHandle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseSurfaceSamplingDistribution(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Vector3 pglSurfaceSamplingDistributionSample(nint handle, Vector2 sample2D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionPDF(nint handle, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionClear(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool pglFieldInitSurfaceSamplingDistribution(nint fieldHandle, nint handle,
        Vector3 samplePosition, ref float sample1D);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionApplyCosineProduct(nint handle, Vector3 normal);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglSurfaceSamplingGetRegion(nint handle);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionLobePDF(nint surfaceSamplingDistribution,
        nuint i, Vector3 direction);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nuint pglSurfaceSamplingDistributionNumLobes(nint surfaceSamplingDistribution);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern float pglSurfaceSamplingDistributionGetLobeWeight(nint surfaceSamplingDistribution,
        nuint i);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglSurfaceSamplingDistributionSetLobeWeight(nint surfaceSamplingDistribution,
        nuint i, float weight);
}

public class SurfaceSamplingDistribution : IDisposable {
    nint handle;
    Field field;

    public SurfaceSamplingDistribution(Field field) {
        this.field = field;
        handle = OpenPGL.pglFieldNewSurfaceSamplingDistribution(field.Handle);
    }

    public void Dispose() {
        if (handle != nint.Zero) {
            OpenPGL.pglReleaseSurfaceSamplingDistribution(handle);
            handle = nint.Zero;
        }
    }

    public Vector3 Sample(Vector2 sample2D)
    => OpenPGL.pglSurfaceSamplingDistributionSample(handle, sample2D);

    public float PDF(Vector3 direction) {
        Common.AssertNormalized(direction);
        return OpenPGL.pglSurfaceSamplingDistributionPDF(handle, direction);
    }

    public void Clear() => OpenPGL.pglSurfaceSamplingDistributionClear(handle);

    public bool Init(Vector3 pos, ref float sample1D)
    => OpenPGL.pglFieldInitSurfaceSamplingDistribution(field.Handle, handle, pos, ref sample1D);

    public void ApplyCosineProduct(Vector3 normal)
    => OpenPGL.pglSurfaceSamplingDistributionApplyCosineProduct(handle, normal);

    public nint Region => OpenPGL.pglSurfaceSamplingGetRegion(handle);

    ~SurfaceSamplingDistribution() => Dispose();

    public int NumLobes => (int) OpenPGL.pglSurfaceSamplingDistributionNumLobes(handle);

    public float ComputeLobePdf(int lobeIdx, in Vector3 direction)
    => OpenPGL.pglSurfaceSamplingDistributionLobePDF(handle, new((uint)lobeIdx), direction);

    public float GetLobeWeight(int lobeIdx)
    => OpenPGL.pglSurfaceSamplingDistributionGetLobeWeight(handle, (nuint)lobeIdx);

    public float[] LobeWeights {
        get {
            float[] weights = new float[NumLobes];
            for (int i = 0; i < weights.Length; ++i)
                weights[i] = OpenPGL.pglSurfaceSamplingDistributionGetLobeWeight(handle, (nuint)i);
            return weights;
        }
        set {
            Debug.Assert(value.Length == NumLobes, "must set all lobe weights");
            for (int i = 0; i < value.Length; ++i)
                OpenPGL.pglSurfaceSamplingDistributionSetLobeWeight(handle, (nuint)i, value[i]);
        }
    }
}
