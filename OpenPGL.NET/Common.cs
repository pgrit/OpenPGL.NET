namespace OpenPGL.NET;

public static class Common {
    public static void AssertNormalized(Vector3 direction) {
        Debug.Assert(MathF.Abs(direction.LengthSquared() - 1.0f) < 0.001f);
    }
}