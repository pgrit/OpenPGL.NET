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

    internal override void SetArguments(ref OpenPGL.PGLFieldArguments target) {
        if (UseParallaxCompensation)
            target.directionalDistributionType = OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM;
        else
            target.directionalDistributionType = OpenPGL.PGL_DIRECTIONAL_DISTRIBUTION_TYPE.PGL_DIRECTIONAL_DISTRIBUTION_VMM;

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
            // parallaxCompensation = ParallaxCompensation,
        };
        Marshal.StructureToPtr<OpenPGL.PGLVMMFactoryArguments>(args, target.directionalDistributionArguments, true);
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
    internal nint Handle;
    Device device;

    public Field(FieldSettings settings = new()) {
        device = new();
        var arguments = settings.MakeArguments();
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