namespace OpenPGL.NET;

internal static partial class OpenPGL {
    public enum PGL_DEVICE_TYPE {
        PGL_DEVICE_TYPE_CPU_4,
        PGL_DEVICE_TYPE_CPU_8,
        PGL_DEVICE_TYPE_CPU_16,
    };

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglNewDevice(PGL_DEVICE_TYPE deviceType);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleaseDevice(nint device);
}

internal class Device : IDisposable {
    public Device() {
        Ptr = OpenPGL.pglNewDevice(OpenPGL.PGL_DEVICE_TYPE.PGL_DEVICE_TYPE_CPU_8);
    }

    public nint Ptr;

    protected virtual void Dispose(bool disposing) {
        if (Ptr != nint.Zero) {
            // OpenPGL.pglReleaseDevice(Ptr); TODO FIXME
            Ptr = nint.Zero;
        }
    }

    ~Device() => Dispose(disposing: false);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}