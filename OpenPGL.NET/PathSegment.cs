using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetPosition(IntPtr data, [In] Vector3 position);
    }

    public class PathSegment {
        internal PathSegment(IntPtr data) {
            this.data = data;
        }

        public Vector3 Position { set { OpenPGL.pglPathSegmentSetPosition(data, value); } }

        // TODO setters for all other values

        IntPtr data;
    }
}
