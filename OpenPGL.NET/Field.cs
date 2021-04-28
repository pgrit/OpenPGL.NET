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
        public static extern void pglFieldUpdate(IntPtr field, BoundingBox bounds, IntPtr sampleStorage,
            uint numPerPixelSamples);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglFieldGetSurfaceRegion(IntPtr field, Vector3 position, ref Sampler sampler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglFieldGetVolumeRegion(IntPtr field, Vector3 position, ref Sampler sampler);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoundingBox {
        public Vector3 Lower, Upper;
    }

    public class Field : IDisposable {
        IntPtr handle;

        public Field() {
            // TODO expose arguments (via class hierarchy equivalent of the enum+void* approach)
            OpenPGL.PGLFieldArguments arguments;
            OpenPGL.pglFieldArgumentsSetDefaults(out arguments);

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

        public void Update(BoundingBox bounds, SampleStorage storage, uint numPerPixelSamples)
        => OpenPGL.pglFieldUpdate(handle, bounds, storage.Handle, numPerPixelSamples);
    }
}