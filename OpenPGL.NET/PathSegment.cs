namespace OpenPGL.NET;

internal static partial class OpenPGL {
    [StructLayout(LayoutKind.Sequential)]
    public struct PathSegmentData {
        public Vector3 Position;
        public Vector3 DirectionIn;
        public Vector3 DirectionOut;
        public Vector3 Normal;
        [MarshalAs(UnmanagedType.I1)] public bool VolumeScatter;
        public float PDFDirectionIn;
        [MarshalAs(UnmanagedType.I1)] public bool IsDelta;
        public Vector3 ScatteringWeight;
        public Vector3 TransmittanceWeight;
        public Vector3 DirectContribution;
        public float MiWeight;
        public Vector3 ScatteredContribution;
        public float RussianRouletteProbability;
        public float Eta;
        public float Roughness;
        public IntPtr Region;
    }
}

public unsafe struct PathSegment {
    private OpenPGL.PathSegmentData* ptr;

    internal PathSegment(IntPtr data) {
        ptr = (OpenPGL.PathSegmentData*)data.ToPointer();
    }

    public Vector3 Position {
        set {
            ptr->Position = value;
        }
    }
    public Vector3 Normal {
        set {
            ptr->Normal = value;
        }
    }
    public Vector3 DirectionIn {
        set {
            Common.AssertNormalized(value);
            ptr->DirectionIn = value;
        }
    }
    public float PDFDirectionIn { set { ptr->PDFDirectionIn = value; } }
    public Vector3 DirectionOut {
        set {
            Common.AssertNormalized(value);
            ptr->DirectionOut = value;
        }
    }
    public bool VolumeScatter { set { ptr->VolumeScatter = value; } }
    public Vector3 ScatteringWeight {
        set {
            ptr->ScatteringWeight = value;
        }
    }
    public Vector3 DirectContribution {
        set {
            ptr->DirectContribution = value;
        }
    }
    public Vector3 ScatteredContribution {
        get => ptr->ScatteredContribution;
        set => ptr->ScatteredContribution = value;
    }
    public float MiWeight { set { ptr->MiWeight = value; } }
    public float RussianRouletteProbability { set { ptr->RussianRouletteProbability = value; } }
    public float Eta { set { ptr->Eta = value; } }
    public bool IsDelta { set { ptr->IsDelta = value; } }
    public float Roughness { set { ptr->Roughness = value; } }
    public Vector3 TransmittanceWeight {
        set {
            ptr->TransmittanceWeight = value;
        }
    }
    public Region Region { set { ptr->Region = value.Handle; } }

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