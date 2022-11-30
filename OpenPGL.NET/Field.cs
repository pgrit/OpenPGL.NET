namespace OpenPGL.NET;

internal static partial class OpenPGL {
    public enum PGL_SPATIAL_STRUCTURE_TYPE {
        PGL_SPATIAL_STRUCTURE_KDTREE = 0
    };

    public enum PGL_DIRECTIONAL_DISTRIBUTION_TYPE {
        PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM = 0,
        PGL_DIRECTIONAL_DISTRIBUTION_QUADTREE,
        PGL_DIRECTIONAL_DISTRIBUTION_VMM
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLFieldArguments {
        public PGL_SPATIAL_STRUCTURE_TYPE spatialStructureType;
        public IntPtr spatialSturctureArguments;
        public PGL_DIRECTIONAL_DISTRIBUTION_TYPE directionalDistributionType;
        public IntPtr directionalDistributionArguments;
        // for debugging
        [MarshalAs(UnmanagedType.I1)] public bool deterministic;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLKDTreeArguments {
        [MarshalAs(UnmanagedType.I1)] public bool knnLookup;
        public UIntPtr minSamples;
        public UIntPtr maxSamples;
        public UIntPtr maxDepth;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLVMMFactoryArguments {
        // weighted EM arguments
        UIntPtr initK;
        float initKappa;

        UIntPtr maxK;
        UIntPtr maxEMIterrations;

        float maxKappa;
        //float maxMeanCosine { KappaToMeanCosine<float>(OPENPGL_MAX_KAPPA)};
        float convergenceThreshold;

        // MAP prior parameters
        // weight prior
        float weightPrior;

        // concentration/meanCosine prior
        float meanCosinePriorStrength;
        float meanCosinePrior;

        // adaptive split and merge arguments
        [MarshalAs(UnmanagedType.I1)] bool useSplitAndMerge;

        float splittingThreshold;
        float mergingThreshold;

        [MarshalAs(UnmanagedType.I1)] bool partialReFit;
        int maxSplitItr;

        int minSamplesForSplitting;
        int minSamplesForPartialRefitting;
        int minSamplesForMerging;
        [MarshalAs(UnmanagedType.I1)] bool parallaxCompensation;
    };

    enum PGLDQTLeafEstimator {
        REJECTION_SAMPLING = 0,
        PER_LEAF
    };

    enum PGLDQTSplitMetric {
        MEAN = 0,
        SECOND_MOMENT
    };

    [StructLayout(LayoutKind.Sequential)]
    struct PGLDQTFactoryArguments {
        PGLDQTLeafEstimator leafEstimator;
        PGLDQTSplitMetric splitMetric;
        float splitThreshold;
        float footprintFactor;
        UInt32 maxLevels;
    };


    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldArgumentsSetDefaults(out PGLFieldArguments arguments,
        PGL_SPATIAL_STRUCTURE_TYPE spatialType, PGL_DIRECTIONAL_DISTRIBUTION_TYPE directionalType);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglDeviceNewField(IntPtr device, PGLFieldArguments arguments);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseField(IntPtr device, IntPtr field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglFieldGetIteration(IntPtr field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglFieldGetTotalSPP(IntPtr field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldUpdate(IntPtr field, IntPtr sampleStorage, UIntPtr numPerPixelSamples);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldSetSceneBounds(IntPtr field, BoundingBox bounds);
}

[StructLayout(LayoutKind.Sequential)]
public struct BoundingBox {
    public Vector3 Lower, Upper;
}

public abstract class SpatialSettings {
    internal abstract void SetArguments(ref OpenPGL.PGLFieldArguments target);
}

public abstract class DirectionalSettings {
    internal abstract void SetArguments(ref OpenPGL.PGLFieldArguments target);
}

public class KdTreeSettings : SpatialSettings {
    public bool KnnLookup = true;
    public uint MinSamples = 100;
    public uint MaxSamples = 32000;
    public uint MaxDepth = 32;

    internal override void SetArguments(ref OpenPGL.PGLFieldArguments target) {
        target.spatialStructureType = OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE.PGL_SPATIAL_STRUCTURE_KDTREE;
        OpenPGL.PGLKDTreeArguments args = new() {
            knnLookup = KnnLookup,
            minSamples = new(MinSamples),
            maxSamples = new(MaxSamples),
            maxDepth = new(MaxDepth)
        };
        Marshal.StructureToPtr<OpenPGL.PGLKDTreeArguments>(args, target.spatialSturctureArguments, true);
    }
}

public struct FieldSettings {
    public SpatialSettings SpatialSettings { get; init; }
    public DirectionalSettings DirectionalSettings { get; init; }

    internal OpenPGL.PGLFieldArguments MakeArguments() {
        OpenPGL.PGLFieldArguments arguments;
        OpenPGL.pglFieldArgumentsSetDefaults(out arguments,
            OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE.PGL_SPATIAL_STRUCTURE_KDTREE,
            OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM);

        SpatialSettings?.SetArguments(ref arguments);
        DirectionalSettings?.SetArguments(ref arguments);

        return arguments;
    }
}

public class Field : IDisposable {
    internal IntPtr Handle;
    Device device;

    public Field(FieldSettings settings = new()) {
        device = new();
        var arguments = settings.MakeArguments();
        Handle = OpenPGL.pglDeviceNewField(device.Ptr, arguments);
        Debug.Assert(Handle != IntPtr.Zero);
    }

    public void Dispose() {
        if (Handle != IntPtr.Zero) {
            OpenPGL.pglReleaseField(device.Ptr, Handle);
            Handle = IntPtr.Zero;
            device.Dispose();
        }
    }

    ~Field() => Dispose();

    public uint Iteration => OpenPGL.pglFieldGetIteration(Handle);
    public uint TotalSPP => OpenPGL.pglFieldGetTotalSPP(Handle);

    public void Update(SampleStorage storage, uint numPerPixelSamples)
    => OpenPGL.pglFieldUpdate(Handle, storage.Handle, new(numPerPixelSamples));

    public BoundingBox SceneBounds { set => OpenPGL.pglFieldSetSceneBounds(Handle, value); }
}