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
        public static extern void pglPathSegmentStorageReserve(IntPtr pathSegmentStorage, UIntPtr size);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentStorageClear(IntPtr pathSegmentStorage);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentStorageGetSamples(IntPtr pathSegmentStorage, out UIntPtr nSamples);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void pglPathSegmentStorageAddSample(IntPtr pathSegmentStorage, SampleData sample);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentNextSegment(IntPtr pathSegmentStorage);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentGetSegment(IntPtr pathSegmentStorage, UIntPtr index);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr pglPathSegmentStoragePrepareSamples(IntPtr pathSegmentStorage,
            [MarshalAs(UnmanagedType.I1), In] in bool spaltSamples, [In, Out] ref Sampler sampler,
            [MarshalAs(UnmanagedType.I1)] bool useNEEMiWeights,
            [MarshalAs(UnmanagedType.I1)] bool guideDirectLight);
    }

    public class PathSegmentStorage : IDisposable {
        IntPtr storage;

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

        public void Reserve(uint size) => OpenPGL.pglPathSegmentStorageReserve(storage, new(size));

        public void Clear() => OpenPGL.pglPathSegmentStorageClear(storage);

        public uint PrepareSamples(bool splatSamples, SamplerWrapper sampler, bool useNEEMiWeights,
                                   bool guideDirectLight) {
            var samplerData = sampler.ToUnmanaged();
            return (uint) OpenPGL.pglPathSegmentStoragePrepareSamples(storage, in splatSamples,
                ref samplerData, useNEEMiWeights, guideDirectLight);
        }

        public unsafe Span<SampleData> Samples {
            get {
                UIntPtr num;
                IntPtr ptr = OpenPGL.pglPathSegmentStorageGetSamples(storage, out num);
                return new(ptr.ToPointer(), (int)num);
            }
        }

        public void AddSample(SampleData sample) => OpenPGL.pglPathSegmentStorageAddSample(storage, sample);

        public PathSegment NextSegment() => new(OpenPGL.pglPathSegmentNextSegment(storage));

        public PathSegment this[uint index] => new(OpenPGL.pglPathSegmentGetSegment(storage, new(index)));

        public PathSegment GetSegment(uint index) => this[index];
    }
}
