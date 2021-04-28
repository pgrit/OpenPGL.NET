using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglNewSurfaceSamplingDistribution();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglReleaseSurfaceSamplingDistribution(IntPtr handle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern Vector3 pglSurfaceSamplingDistributionSample(IntPtr handle, Vector2 sample2D);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern float pglSurfaceSamplingDistributionPDF(IntPtr handle, Vector3 direction);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool pglSurfaceSamplingDistributionIsValid(IntPtr handle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglSurfaceSamplingDistributionClear(IntPtr handle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglSurfaceSamplingDistributionInit(IntPtr handle, IntPtr regionHandle,
            Vector3 samplePosition, Vector3 normal, [MarshalAs(UnmanagedType.I1)] bool useParallaxComp,
            [MarshalAs(UnmanagedType.I1)] bool useCosine);
    }

    public class SurfaceSamplingDistribution : IDisposable {
        IntPtr handle;

        public SurfaceSamplingDistribution() {
            handle = OpenPGL.pglNewSurfaceSamplingDistribution();
        }

        public void Dispose() {
            if (handle != IntPtr.Zero) {
                OpenPGL.pglReleaseSurfaceSamplingDistribution(handle);
                handle = IntPtr.Zero;
            }
        }

        public Vector3 Sample(Vector2 sample2D)
        => OpenPGL.pglSurfaceSamplingDistributionSample(handle, sample2D);

        public float PDF(Vector3 direction)
        => OpenPGL.pglSurfaceSamplingDistributionPDF(handle, direction);

        public bool IsValid => OpenPGL.pglSurfaceSamplingDistributionIsValid(handle);

        public void Clear() => OpenPGL.pglSurfaceSamplingDistributionClear(handle);

        public void Init(Region region, Vector3 pos, Vector3 normal, bool useParallaxCompensation = true,
            bool useCosineProduct = true)
        => OpenPGL.pglSurfaceSamplingDistributionInit(handle, region.Handle, pos, normal,
            useParallaxCompensation, useCosineProduct);

        ~SurfaceSamplingDistribution() => Dispose();
    }
}
