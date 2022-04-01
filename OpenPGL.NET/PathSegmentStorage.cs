namespace OpenPGL.NET;

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
    public static extern IntPtr pglPathSegmentStorageNextSegment(IntPtr pathSegmentStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr pglPathSegmentStoragePrepareSamples(IntPtr pathSegmentStorage,
        [MarshalAs(UnmanagedType.I1), In] in bool spaltSamples, [In, Out] ref Sampler sampler,
        [MarshalAs(UnmanagedType.I1)] bool useNEEMiWeights,
        [MarshalAs(UnmanagedType.I1)] bool guideDirectLight,
        [MarshalAs(UnmanagedType.I1)] bool rrEffectsDirectContribution);
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

    public int Count { get; private set; }
    public bool IsPrepared = false;

    public void Reserve(uint size) => OpenPGL.pglPathSegmentStorageReserve(storage, new(size));

    public void Clear() {
        Count = 0;
        IsPrepared = false;
        OpenPGL.pglPathSegmentStorageClear(storage);
    }

    public uint PrepareSamples(bool splatSamples, SamplerWrapper sampler, bool useNEEMiWeights,
                               bool guideDirectLight, bool rrAffectsDirectContribution) {
        Debug.Assert(!IsPrepared);
        IsPrepared = true;

        // Only allocate the sampler if it is actually needed.
        OpenPGL.Sampler samplerData;
        if (splatSamples)
            samplerData = sampler.ToUnmanaged();
        else
            samplerData = new() { };

        return (uint)OpenPGL.pglPathSegmentStoragePrepareSamples(storage, in splatSamples,
            ref samplerData, useNEEMiWeights, guideDirectLight, rrAffectsDirectContribution);
    }

    public unsafe Span<SampleData> Samples {
        get {
            IntPtr ptr = OpenPGL.pglPathSegmentStorageGetSamples(storage, out UIntPtr num);
            return new(ptr.ToPointer(), (int)num);
        }
    }

    public IntPtr SamplesRawPointer => OpenPGL.pglPathSegmentStorageGetSamples(storage, out _);

    public void AddSample(SampleData sample) => OpenPGL.pglPathSegmentStorageAddSample(storage, sample);

    public PathSegment NextSegment() {
        Count += 1;
        LastSegment = new PathSegment(OpenPGL.pglPathSegmentStorageNextSegment(storage));
        LastSegment.SetDefaults();
        return LastSegment;
    }

    public PathSegment LastSegment { get; private set; }
}
