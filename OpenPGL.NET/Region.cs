namespace OpenPGL.NET;

internal static partial class OpenPGL {
}

public struct Region {
    internal IntPtr Handle;

    internal Region(IntPtr handle) {
        Debug.Assert(handle != IntPtr.Zero);
        Handle = handle;
    }

    public bool IsValid => Handle != IntPtr.Zero;
}
