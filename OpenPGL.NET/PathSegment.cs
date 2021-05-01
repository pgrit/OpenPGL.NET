using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetPosition(IntPtr data, [In] Vector3 position);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetNormal(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetDirectionIn(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetPDFDirectionIn(IntPtr data, float value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetDirectionOut(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetVolumeScatter(IntPtr data, [MarshalAs(UnmanagedType.I1)] bool value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetScatteringWeight(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetDirectContribution(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetScatteredContribution(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetMiWeight(IntPtr data, float value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetRussianRouletteProbability(IntPtr data, float value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetEta(IntPtr data, float value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetIsDelta(IntPtr data, [MarshalAs(UnmanagedType.I1)] bool value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetRoughness(IntPtr data, float value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetTransmittanceWeight(IntPtr data, Vector3 value);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentSetRegion(IntPtr data, IntPtr value);
    }

    public struct PathSegment {
        internal PathSegment(IntPtr data) {
            this.data = data;
        }

        public Vector3 Position { set { OpenPGL.pglPathSegmentSetPosition(data, value); } }
        public Vector3 Normal { set { OpenPGL.pglPathSegmentSetNormal(data, value); } }
        public Vector3 DirectionIn { set { OpenPGL.pglPathSegmentSetDirectionIn(data, value); } }
        public float PDFDirectionIn { set { OpenPGL.pglPathSegmentSetPDFDirectionIn(data, value); } }
        public Vector3 DirectionOut { set { OpenPGL.pglPathSegmentSetDirectionOut(data, value); } }
        public bool VolumeScatter { set { OpenPGL.pglPathSegmentSetVolumeScatter(data, value); } }
        public Vector3 ScatteringWeight { set { OpenPGL.pglPathSegmentSetScatteringWeight(data, value); } }
        public Vector3 DirectContribution { set { OpenPGL.pglPathSegmentSetDirectContribution(data, value); } }
        public Vector3 ScatteredContribution { set { OpenPGL.pglPathSegmentSetScatteredContribution(data, value); } }
        public float MiWeight { set { OpenPGL.pglPathSegmentSetMiWeight(data, value); } }
        public float RussianRouletteProbability { set { OpenPGL.pglPathSegmentSetRussianRouletteProbability(data, value); } }
        public float Eta { set { OpenPGL.pglPathSegmentSetEta(data, value); } }
        public bool IsDelta { set { OpenPGL.pglPathSegmentSetIsDelta(data, value); } }
        public float Roughness { set { OpenPGL.pglPathSegmentSetRoughness(data, value); } }
        public Vector3 TransmittanceWeight { set { OpenPGL.pglPathSegmentSetTransmittanceWeight(data, value); } }
        public Region Region { set { OpenPGL.pglPathSegmentSetRegion(data, value.Handle); } }

        IntPtr data;
    }
}
