namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [StructLayout(LayoutKind.Sequential)]
    public struct Sampler {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate float PGLSamplerNext1DFunction(IntPtr sampler);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate Vector2 PGLSamplerNext2DFunction(IntPtr sampler);

        public IntPtr UserData;
        public IntPtr Next1D;
        public IntPtr Next2D;
    }
}

/// <summary>
/// Provides access to a random number generator via callbacks for the unmanaged API.
/// This is VERY SLOW due to multiple indirections and high overhead. Avoid if possible.
/// </summary>
public struct SamplerWrapper {
    public delegate float Next1DFunc();
    public delegate Vector2 Next2DFunc();

    OpenPGL.Sampler.PGLSamplerNext1DFunction next1dDelegate;
    OpenPGL.Sampler.PGLSamplerNext2DFunction next2dDelegate;
    OpenPGL.Sampler sampler;

    public SamplerWrapper(Next1DFunc next1d, Next2DFunc next2d) {
        next1dDelegate = _ => next1d();
        next2dDelegate = _ => next2d();
        sampler = new() {
            Next1D = next1d != null ? Marshal.GetFunctionPointerForDelegate(next1d) : IntPtr.Zero,
            Next2D = next2d != null ? Marshal.GetFunctionPointerForDelegate(next2d) : IntPtr.Zero
        };
    }

    internal OpenPGL.Sampler ToUnmanaged() => sampler;
}