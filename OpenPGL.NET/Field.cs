using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        public enum PGL_SPATIAL_STRUCTURE_TYPE {
            PGL_SPATIAL_STRUCTURE_KDTREE = 0
        };

        public enum PGL_DIRECTIONAL_DISTRIBUTION_TYPE {
            PGL_DIRECTIONAL_DISTRIBUTION_VMM = 0,
            PGL_DIRECTIONAL_DISTRIBUTION_PARALLAX_AWARE_VMM,
            PGL_DIRECTIONAL_DISTRIBUTION_QUADTREE
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct PGLFieldArguments {
            public PGL_SPATIAL_STRUCTURE_TYPE spatialStructureType;
            public IntPtr spatialSturctureArguments;
            public PGL_DIRECTIONAL_DISTRIBUTION_TYPE directionalDistributionType;
            public IntPtr directionalDistributionArguments;
            // for debugging
            [MarshalAs(UnmanagedType.I1)] public bool useParallaxCompensation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PGLKDTreeArguments
        {
            public bool knnLookup;
            public UIntPtr minSamples;
            public UIntPtr maxSamples;
            public UIntPtr maxDepth;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct PGLVMMFactoryArguments
        {
            // weighted EM arguments
            UIntPtr initK;

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
            bool useSplitAndMerge;

            float splittingThreshold;
            float mergingThreshold;

            bool partialReFit;
            int maxSplitItr;

            int minSamplesForSplitting;
            int minSamplesForPartialRefitting;
            int minSamplesForMerging;
        };

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglFieldArgumentsSetDefaults(out PGLFieldArguments arguments);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglNewField(PGLFieldArguments arguments);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglReleaseField(IntPtr field);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint pglFieldGetIteration(IntPtr field);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint pglFieldGetTotalSPP(IntPtr field);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglFieldUpdate(IntPtr field, IntPtr sampleStorage, UIntPtr numPerPixelSamples);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglFieldGetSurfaceRegion(IntPtr field, Vector3 position, [In] ref Sampler sampler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglFieldGetVolumeRegion(IntPtr field, Vector3 position, [In] ref Sampler sampler);

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
            OpenPGL.pglFieldArgumentsSetDefaults(out arguments);

            SpatialSettings?.SetArguments(ref arguments);
            DirectionalSettings?.SetArguments(ref arguments);

            return arguments;
        }
    }

    public class Field : IDisposable {
        IntPtr handle;

        public Field(FieldSettings settings = new()) {
            var arguments = settings.MakeArguments();
            handle = OpenPGL.pglNewField(arguments);
            Debug.Assert(handle != IntPtr.Zero);
        }

        public void Dispose() {
            if (handle != IntPtr.Zero) {
                OpenPGL.pglReleaseField(handle);
                handle = IntPtr.Zero;
            }
        }

        ~Field() => Dispose();

        public uint Iteration => OpenPGL.pglFieldGetIteration(handle);
        public uint TotalSPP => OpenPGL.pglFieldGetTotalSPP(handle);

        public Region GetSurfaceRegion(Vector3 position, SamplerWrapper sampler) {
            var s = sampler.ToUnmanaged();
            return new(OpenPGL.pglFieldGetSurfaceRegion(handle, position, ref s));
        }

        public Region GetVolumeRegion(Vector3 position, SamplerWrapper sampler) {
            var s = sampler.ToUnmanaged();
            return new(OpenPGL.pglFieldGetVolumeRegion(handle, position, ref s));
        }

        public void Update(SampleStorage storage, uint numPerPixelSamples)
        => OpenPGL.pglFieldUpdate(handle, storage.Handle, new(numPerPixelSamples));

        public BoundingBox SceneBounds { set => OpenPGL.pglFieldSetSceneBounds(handle, value); }
    }
}