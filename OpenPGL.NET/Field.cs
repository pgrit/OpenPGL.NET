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
        public nint spatialSturctureArguments;
        public PGL_DIRECTIONAL_DISTRIBUTION_TYPE directionalDistributionType;
        public nint directionalDistributionArguments;
        // for debugging
        [MarshalAs(UnmanagedType.I1)] public bool deterministic;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLKDTreeArguments {
        [MarshalAs(UnmanagedType.I1)] public bool knnLookup;
        public nuint minSamples;
        public nuint maxSamples;
        public nuint maxDepth;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLVMMFactoryArguments {
        // weighted EM arguments
        public nuint initK;
        public float initKappa;

        public nuint maxK;
        public nuint maxEMIterations;

        public float maxKappa;
        //float maxMeanCosine { KappaToMeanCosine<float>(OPENPGL_MAX_KAPPA)};
        public float convergenceThreshold;

        // MAP prior parameters
        // weight prior
        public float weightPrior;

        // concentration/meanCosine prior
        public float meanCosinePriorStrength;
        public float meanCosinePrior;

        // adaptive split and merge arguments
        [MarshalAs(UnmanagedType.I1)] public bool useSplitAndMerge;

        public float splittingThreshold;
        public float mergingThreshold;

        [MarshalAs(UnmanagedType.I1)] public bool partialReFit;
        public int maxSplitItr;

        public int minSamplesForSplitting;
        public int minSamplesForPartialRefitting;
        public int minSamplesForMerging;
        // [MarshalAs(UnmanagedType.I1)] public bool parallaxCompensation;
    };

    public enum PGLDQTLeafEstimator {
        REJECTION_SAMPLING = 0,
        PER_LEAF
    };

    public enum PGLDQTSplitMetric {
        MEAN = 0,
        SECOND_MOMENT
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct PGLDQTFactoryArguments {
        public PGLDQTLeafEstimator leafEstimator;
        public PGLDQTSplitMetric splitMetric;
        public float splitThreshold;
        public float footprintFactor;
        public UInt32 maxLevels;
    };


    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldArgumentsSetDefaults(out PGLFieldArguments arguments,
        PGL_SPATIAL_STRUCTURE_TYPE spatialType, PGL_DIRECTIONAL_DISTRIBUTION_TYPE directionalType);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglDeviceNewField(nint device, PGLFieldArguments arguments);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseField(nint device, nint field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglFieldGetIteration(nint field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint pglFieldGetTotalSPP(nint field);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldUpdate(nint field, nint sampleStorage, nuint numPerPixelSamples);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglFieldSetSceneBounds(nint field, BoundingBox bounds);
}

[StructLayout(LayoutKind.Sequential)]
public struct BoundingBox {
    public Vector3 Lower, Upper;
}

public abstract class SpatialSettings {
    internal abstract OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE StructType { get; }
    internal abstract void SetArguments(ref OpenPGL.PGLFieldArguments target);
}

public abstract class DirectionalSettings {
    internal abstract OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE DistType { get; }
    internal abstract void SetArguments(ref OpenPGL.PGLFieldArguments target);
}

public class KdTreeSettings : SpatialSettings {
    public bool KnnLookup = true;
    public uint MinSamples = 100;
    public uint MaxSamples = 32000;
    public uint MaxDepth = 32;

    internal override OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE StructType
    => OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE.PGL_SPATIAL_STRUCTURE_KDTREE;

    internal override void SetArguments(ref OpenPGL.PGLFieldArguments target) {
        target.spatialStructureType = StructType;
        OpenPGL.PGLKDTreeArguments args = new() {
            knnLookup = KnnLookup,
            minSamples = new(MinSamples),
            maxSamples = new(MaxSamples),
            maxDepth = new(MaxDepth)
        };
        Marshal.StructureToPtr(args, target.spatialSturctureArguments, true);
    }
}

public class VMMDirectionalSettings : DirectionalSettings {
    // Defaults extracted from the 06.01.2024 OpenPGL version

    public int InitK = 16;
    public float InitKappa = 0.5f;

    public int MaxK = 32;
    public int MaxEMIterations = 100;

    public float MaxKappa = 320000;
    public float ConvergenceThreshold = 0.005f;

    public float WeightPrior = 0.01f;

    public float MeanCosinePriorStrength = 0.2f;
    public float MeanCosinePrior = 0;

    public bool UseSplitAndMerge = true;

    public float SplittingThreshold = 0.5f;
    public float MergingThreshold = 0.025f;

    public bool PartialReFit = true;
    public int MaxSplitItr = 1;

    public int MinSamplesForSplitting = 32000 / 8;
    public int MinSamplesForPartialRefitting = 32000 / 8;
    public int MinSamplesForMerging = 32000 / 4;

    public bool UseParallaxCompensation = true;

    internal override OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE DistType
    => UseParallaxCompensation
    ? OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM
    : OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_VMM;

    internal override void SetArguments(ref OpenPGL.PGLFieldArguments target) {
        target.directionalDistributionType = DistType;

        OpenPGL.PGLVMMFactoryArguments args = new() {
            initK = (nuint)int.Max(1, InitK),
            initKappa = InitKappa,
            maxK = (nuint)int.Max(1, MaxK),
            maxEMIterations = (nuint)int.Max(0, MaxEMIterations),
            maxKappa = MaxKappa,
            convergenceThreshold = ConvergenceThreshold,
            weightPrior = WeightPrior,
            meanCosinePriorStrength = MeanCosinePriorStrength,
            meanCosinePrior = MeanCosinePrior,
            useSplitAndMerge = UseSplitAndMerge,
            splittingThreshold = SplittingThreshold,
            mergingThreshold = MergingThreshold,
            partialReFit = PartialReFit,
            maxSplitItr = MaxSplitItr,
            minSamplesForSplitting = MinSamplesForSplitting,
            minSamplesForPartialRefitting = MinSamplesForPartialRefitting,
            minSamplesForMerging = MinSamplesForMerging,
        };
        Marshal.StructureToPtr(args, target.directionalDistributionArguments, true);
    }
}

public enum DQTLeafEstimator {
    RejectionSampling = 0,
    PerLeaf
};

public enum DQTSplitMetric {
    Mean = 0,
    SecondMoment
};

public class DQTDirectionalSettings : DirectionalSettings {
    public DQTLeafEstimator LeafEstimator = DQTLeafEstimator.RejectionSampling;
    public DQTSplitMetric SplitMetric = DQTSplitMetric.Mean;
    public float SplitThreshold = 0.01f;
    public float FootprintFactor = 1;
    public int MaxLevels = 12;

    internal override OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE DistType
    => OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_QUADTREE;

    internal override void SetArguments(ref OpenPGL.PGLFieldArguments target) {
        target.directionalDistributionType = DistType;

        OpenPGL.PGLDQTFactoryArguments args = new() {
            leafEstimator = (OpenPGL.PGLDQTLeafEstimator) LeafEstimator,
            splitMetric = (OpenPGL.PGLDQTSplitMetric) SplitMetric,
            splitThreshold = SplitThreshold,
            footprintFactor = FootprintFactor,
            maxLevels = (uint)int.Max(0, MaxLevels),
        };
        Marshal.StructureToPtr(args, target.directionalDistributionArguments, true);
    }
}

public struct FieldSettings {
    public SpatialSettings SpatialSettings { get; init; }
    public DirectionalSettings DirectionalSettings { get; init; }
}

public class Field : IDisposable {
    internal nint Handle;
    Device device;

    public Field(FieldSettings settings = new()) {
        device = new();

        OpenPGL.pglFieldArgumentsSetDefaults(out var arguments,
            settings.SpatialSettings?.StructType ?? OpenPGL.PGL_SPATIAL_STRUCTURE_TYPE.PGL_SPATIAL_STRUCTURE_KDTREE,
            settings.DirectionalSettings?.DistType ?? OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM);

        settings.SpatialSettings?.SetArguments(ref arguments);
        settings.DirectionalSettings?.SetArguments(ref arguments);

        Handle = OpenPGL.pglDeviceNewField(device.Ptr, arguments);
        Debug.Assert(Handle != nint.Zero);
    }

    public void Dispose() {
        if (Handle != nint.Zero) {
            OpenPGL.pglReleaseField(device.Ptr, Handle);
            Handle = nint.Zero;
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