using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenPGL.NET {
    internal static partial class OpenPGL {
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglNewPathSegmentStorage();

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglReleasePathSegmentStorage(IntPtr pathSegmentStorage);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentStorageReserve(IntPtr pathSegmentStorage, IntPtr size);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentStorageClear(IntPtr pathSegmentStorage);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentStorageGetSamples(IntPtr pathSegmentStorage, out uint nSamples);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentStorageAddSample(IntPtr pathSegmentStorage, [In] SampleData sample);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentNextSegment(IntPtr pathSegmentStorage);
    }

    public class PathSegmentStorage : IDisposable {
        public PathSegmentStorage() {
            storage = OpenPGL.pglNewPathSegmentStorage();
            Debug.Assert(storage != IntPtr.Zero);
        }

        public void Dispose() {
            if (storage != IntPtr.Zero) {
                OpenPGL.pglReleasePathSegmentStorage(storage);
                storage = IntPtr.Zero;
            }
        }

        ~PathSegmentStorage() => Dispose();

        public void Reserve(uint size) => OpenPGL.pglPathSegmentStorageReserve(storage, new IntPtr(size));

        public void Clear() => OpenPGL.pglPathSegmentStorageClear(storage);

        public uint PrepareSamples(bool splatSamples, Sampler sampler, bool useNEEMiWeights,
                                   bool guideDirectLight) {
            return 0;
        }

        public unsafe Span<SampleData> Samples {
            get {
                uint num;
                IntPtr ptr = OpenPGL.pglPathSegmentStorageGetSamples(storage, out num);
                return new(ptr.ToPointer(), (int)num);
            }
        }

        public void AddSample(SampleData sample) => OpenPGL.pglPathSegmentStorageAddSample(storage, sample);

        public PathSegment NextSegment() => new(OpenPGL.pglPathSegmentNextSegment(storage));

        IntPtr storage;
    }
}
