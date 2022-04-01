namespace OpenPGL.NET;

public struct SampleData {
    [Flags]
    public enum SampleFlags : UInt32 {
        ESplatted = 1 << 0, // point does not represent any real scene intersection point
        EInsideVolume = 1 << 1 // point does not represent any real scene intersection point
    };

    public Vector3 Position;
    public Vector3 Direction;
    public float Weight;
    public float Pdf;
    public float Distance;
    public SampleFlags Flags;
}
