namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglNewPathSegmentStorage();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglReleasePathSegmentStorage(nint pathSegmentStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglPathSegmentStorageReserve(nint pathSegmentStorage, nuint size);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglPathSegmentStorageClear(nint pathSegmentStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglPathSegmentStorageGetSamples(nint pathSegmentStorage, out nuint nSamples);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void pglPathSegmentStorageAddSample(nint pathSegmentStorage, SampleData sample);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglPathSegmentStorageNextSegment(nint pathSegmentStorage);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern nint pglPathSegmentStoragePrepareSamples(nint pathSegmentStorage,
        [MarshalAs(UnmanagedType.I1)] bool useNEEMiWeights,
        [MarshalAs(UnmanagedType.I1)] bool guideDirectLight,
        [MarshalAs(UnmanagedType.I1)] bool rrEffectsDirectContribution);
}

public class PathSegmentStorage : IDisposable {
    nint storage;

    public PathSegmentStorage() {
        storage = OpenPGL.pglNewPathSegmentStorage();
        Debug.Assert(storage != nint.Zero);
    }

    public void Dispose() {
        if (storage != nint.Zero) {
            OpenPGL.pglReleasePathSegmentStorage(storage);
            storage = nint.Zero;
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

    public uint PrepareSamples(bool useNEEMiWeights, bool guideDirectLight, bool rrAffectsDirectContribution) {
        Debug.Assert(!IsPrepared);
        IsPrepared = true;

        return (uint)OpenPGL.pglPathSegmentStoragePrepareSamples(storage, useNEEMiWeights, guideDirectLight,
            rrAffectsDirectContribution);
    }

    public unsafe Span<SampleData> Samples {
        get {
            nint ptr = OpenPGL.pglPathSegmentStorageGetSamples(storage, out nuint num);
            return new(ptr.ToPointer(), (int)num);
        }
    }

    public nint SamplesRawPointer => OpenPGL.pglPathSegmentStorageGetSamples(storage, out _);

    public void AddSample(SampleData sample) => OpenPGL.pglPathSegmentStorageAddSample(storage, sample);

    public PathSegment NextSegment() {
        Count += 1;
        LastSegment = new PathSegment(OpenPGL.pglPathSegmentStorageNextSegment(storage));
        LastSegment.SetDefaults();
        return LastSegment;
    }

    public PathSegment LastSegment { get; private set; }
}
