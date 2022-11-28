using System.Numerics;
using Xunit;

namespace OpenPGL.NET.Tests;

public class VMMLobe {
    [Fact]
    public void LobeMixture_ShouldMatchGuidingPdf() {
        using Field field = new();

        // Generate a batch of dummy samples on the xy plane in [0,1]x[0,1]
        uint num = 100000;
        OpenPGL.NET.SampleStorage samples = new();
        samples.Reserve(num, 0);
        System.Random rng = new(1337);
        for (int i = 0; i < num; ++i)
            samples.AddSample(new()
            {
                Position = new((float)rng.NextDouble(), (float)rng.NextDouble(), 0),
                Direction = Vector3.UnitZ,
                Distance = 10,
                Pdf = 1,
                Weight = 42
            });

        // Train the model
        field.Update(samples, 1);

        SurfaceSamplingDistribution distribution = new(field);
        var u = 0.5f;
        distribution.Init(new(0.5f, 0.5f, 0), ref u);

        float mixturePdf = 0.0f;
        for (int i = 0; i < distribution.NumLobes; ++i) {
            mixturePdf += distribution.LobeWeights[i] * distribution.ComputeLobePdf(i, Vector3.UnitZ);
        }

        float guidePdf = distribution.PDF(Vector3.UnitZ);
        Assert.Equal(guidePdf, mixturePdf, 3);
    }
}