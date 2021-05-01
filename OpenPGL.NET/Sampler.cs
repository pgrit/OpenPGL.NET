using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [StructLayout(LayoutKind.Sequential)]
        public struct Sampler {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate float PGLSamplerNext1DFunction(IntPtr sampler);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate Vector2 PGLSamplerNext2DFunction(IntPtr sampler);

            IntPtr Unused; // Delegates track their containing class for us already
            [MarshalAs(UnmanagedType.FunctionPtr)] public PGLSamplerNext1DFunction Next1D;
            [MarshalAs(UnmanagedType.FunctionPtr)] public PGLSamplerNext2DFunction Next2D;
        }
    }

    /// <summary>
    /// Provides access to a random number generator via callbacks for the unmanaged API.
    /// This is VERY SLOW due to multiple indirections and high overhead. Avoid if possible.
    /// </summary>
    public struct SamplerWrapper {
        public delegate float Next1DFunc();
        public delegate Vector2 Next2DFunc();

        public Next1DFunc Next1D { get; init; }
        public Next2DFunc Next2D { get; init; }

        public SamplerWrapper(Next1DFunc next1d, Next2DFunc next2d) {
            Next1D = next1d;
            Next2D = next2d;
        }

        internal float Next1DDummy(IntPtr _) => 0;
        internal Vector2 Next2DDummy(IntPtr _) => new(0, 0);

        internal OpenPGL.Sampler ToUnmanaged() => new() {
            Next1D = Next1DDummy,
            Next2D = Next2DDummy
        };
    }
}