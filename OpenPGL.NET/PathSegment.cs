namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [StructLayout(LayoutKind.Sequential)]
    public struct PathSegmentData {
        public Vector3 Position;
        public Vector3 DirectionIn;
        public Vector3 DirectionOut;
        public Vector3 Normal;
        public byte VolumeScatter;
        public float PDFDirectionIn;
        public byte IsDelta;
        public Vector3 ScatteringWeight;
        public Vector3 TransmittanceWeight;
        public Vector3 DirectContribution;
        public float MiWeight;
        public Vector3 ScatteredContribution;
        public float RussianRouletteProbability;
        public float Eta;
        public float Roughness;
        public nint Region;
    }
}

public unsafe struct PathSegment {
    private OpenPGL.PathSegmentData* ptr;

    internal PathSegment(nint data) {
        ptr = (OpenPGL.PathSegmentData*)data.ToPointer();
    }

    public ref Vector3 Position => ref ptr->Position;
    public ref Vector3 Normal => ref ptr->Normal;
    public ref Vector3 DirectionIn => ref ptr->DirectionIn;
    public ref float PDFDirectionIn => ref ptr->PDFDirectionIn;
    public ref Vector3 DirectionOut => ref ptr->DirectionOut;
    public bool VolumeScatter {
        get => ptr->VolumeScatter != 0;
        set => ptr->VolumeScatter = value ? (byte)1 : (byte)0;
    }
    public ref Vector3 ScatteringWeight => ref ptr->ScatteringWeight;
    public ref Vector3 DirectContribution => ref ptr->DirectContribution;
    public ref Vector3 ScatteredContribution => ref ptr->ScatteredContribution;
    public ref float MiWeight => ref ptr->MiWeight;
    public ref float RussianRouletteProbability => ref ptr->RussianRouletteProbability;
    public ref float Eta => ref ptr->Eta;
    public bool IsDelta {
        get => ptr->IsDelta != 0;
        set => ptr->IsDelta = value ? (byte)1 : (byte)0;
    }
    public ref float Roughness => ref ptr->Roughness;
    public ref Vector3 TransmittanceWeight => ref ptr->TransmittanceWeight;

    public void SetDefaults() {
        Position = Vector3.Zero;
        Normal = Vector3.UnitZ;
        DirectionIn = Vector3.UnitZ;
        DirectionOut = Vector3.UnitZ;
        PDFDirectionIn = 1;
        VolumeScatter = false;
        ScatteringWeight = Vector3.Zero;
        DirectContribution = Vector3.Zero;
        ScatteredContribution = Vector3.Zero;
        MiWeight = 1;
        RussianRouletteProbability = 1;
        Eta = 1;
        IsDelta = false;
        Roughness = 1;
        TransmittanceWeight = Vector3.One;
    }
}