namespace OpenPGL.NET;

public struct SampleData
{
    [Flags]
    public enum SampleFlags : UInt32
    {
        EInsideVolume = 1 << 0, // point does not represent any real scene intersection point
        EDirectLight = 1 << 1, // if the samples represents direct light from a light source
    };

    public Vector3 Position;
    public Vector3 Direction;
    public float Weight;
    public float Pdf;
    public float Distance;
    public SampleFlags Flags;
}
