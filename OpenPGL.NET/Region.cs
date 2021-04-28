using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool pglRegionGetValid(IntPtr regionHandle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglRegionGetDistribution(IntPtr region, Vector3 samplePosition,
            [MarshalAs(UnmanagedType.I1)] in bool useParallaxComp);
    }

    public class Distribution {
        IntPtr handle;
        internal Distribution(IntPtr handle) {
            this.handle = handle;
        }
    }

    public class Region {
        internal IntPtr Handle;

        internal Region(IntPtr handle) {
            Handle = handle;
        }

        public bool IsValid => OpenPGL.pglRegionGetValid(Handle);

        public Distribution GetDistribution(Vector3 samplePosition, bool useParallaxComp)
        => new(OpenPGL.pglRegionGetDistribution(Handle, samplePosition, useParallaxComp));
    }
}