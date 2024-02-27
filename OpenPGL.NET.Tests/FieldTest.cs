using System.Numerics;
using Xunit;

namespace OpenPGL.NET.Tests;

public class FieldTest
{
    [Fact]
    public void FieldShouldBeValid()
    {
        using Field field = new(new FieldSettings() {
            SpatialSettings = new KdTreeSettings() { KnnLookup = false, MinSamples = 100000 },
            DirectionalSettings = new DQTDirectionalSettings() { }
        });

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
                Pdf = 1.0f / (2.0f * System.MathF.PI),
                Weight = 42,
            });

        field.Update(samples, 1);

        Assert.True(field.IsValid);
    }

    [Fact]
    public void RegionShouldBeFound()
    {
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

        // Check that a valid region is found in the center
        SurfaceSamplingDistribution distribution = new(field);
        var u = 0.5f;
        distribution.Init(new(0.5f, 0.5f, 0), ref u);
        var region = distribution.Region;

        Assert.True(region != nint.Zero);
    }

    [Fact]
    public void RegionShouldBeFound_NoKnn()
    {
        using Field field = new(new FieldSettings()
        {
            SpatialSettings = new KdTreeSettings() { KnnLookup = false }
        });

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

        // Check that a valid region is found in the center
        SurfaceSamplingDistribution distribution = new(field);
        var u = 0.5f;
        distribution.Init(new(0.5f, 0.5f, 0), ref u);
        var region = distribution.Region;

        Assert.True(region != nint.Zero);
    }
}