using System;
using System.Diagnostics;
using System.Numerics;
using OpenPGL.NET;
using SeeSharp.Shading;
using SeeSharp.Shading.Background;
using SimpleImageIO;

void BenchPrepareSamples()
{
    PathSegmentStorage storage = new();
    storage.Reserve(10);

    System.Random rng = new(1337);

    int numrepeats = 10_000_000;
    var timer = Stopwatch.StartNew();

    for (int i = 0; i < numrepeats; ++i)
    {
        // Surface hit point
        PathSegment segment = storage.NextSegment();
        segment.Position = new(0, 0, 3);
        segment.Normal = Vector3.UnitZ;
        segment.DirectionIn = Vector3.UnitZ;
        segment.DirectionOut = Vector3.UnitZ;
        segment.ScatteredContribution = new(0, 0, 0);
        segment.ScatteringWeight = new(1, 1, 1);

        // Light vertex
        segment = storage.NextSegment();
        segment.Position = new(0, 0, 5);
        segment.DirectContribution = new(10, 10, 10);

        uint num = storage.PrepareSamples(false, true, true);

        storage.Clear();
    }

    Console.WriteLine($"PathSegmentStorage.PrepareSamples() per DI path: {timer.ElapsedMilliseconds / (double)numrepeats}ms");
}

OpenPGL.NET.Field GenerateData(uint numTrainSamples = 10000) {
    RgbImage hdr = new(1024, 512);
    for (int row = 0; row < 256; ++row)
    {
        for (int col = 0; col < 1024; ++col)
        {
            hdr.SetPixel(col, row, new RgbColor(0.1f, 0.2f, 0.8f));

            if ((new Vector2(col, row) - new Vector2(550, 130)).Length() < 8)
                hdr.SetPixel(col, row, new RgbColor(0.9f, 0.8f, 0.3f) * 100);
        }
    }
    for (int row = 256; row < 512; ++row)
    {
        for (int col = 0; col < 1024; ++col)
        {
            hdr.SetPixel(col, row, new RgbColor(0.2f, 0.8f, 0));
        }
    }

    var surfaceNormal = Vector3.Normalize(new Vector3(0.0f, 1.0f, 0.3f));
    var envmap = new EnvironmentMap(hdr);

    var field = new OpenPGL.NET.Field();

    OpenPGL.NET.SampleStorage samples = new();
    samples.Reserve((uint)numTrainSamples, 0);
    System.Random rng = new(1337);
    for (int i = 0; i < numTrainSamples; ++i) {
        var dir = envmap.SampleDirection(new(rng.NextSingle(), rng.NextSingle()));
        var primDir = ShadingSpace.WorldToShading(surfaceNormal, dir.Direction);
        float cos = Math.Max(primDir.Z, 0);

        samples.AddSample(new()
        {
            Position = new((float)rng.NextDouble(), (float)rng.NextDouble(), 0),
            Direction = dir.Direction,
            Distance = 10000,
            Pdf = dir.Pdf,
            Weight = (dir.Weight * cos).Average
        });
    }

    // Train the model
    field.Update(samples, 1);
    return field;
}

void BenchInitDistribution(uint numRepeats = 100000)
{
    var field = GenerateData();

    var distribution = new OpenPGL.NET.SurfaceSamplingDistribution(field);

    var timer = System.Diagnostics.Stopwatch.StartNew();
    for (uint i = 0; i < numRepeats; ++i)
    {
        var u = 0.5f;
        distribution.Init(new(0.5f, 0.5f, 0), ref u);
        distribution.ApplyCosineProduct(Vector3.UnitY);
    }
    Console.WriteLine($"Init distribution: {timer.ElapsedMilliseconds / (double)numRepeats}ms");
}

void BenchSampleDistribution(uint numRepeats = 100000)
{
    var field = GenerateData();
    var distribution = new OpenPGL.NET.SurfaceSamplingDistribution(field);
    var u = 0.5f;
    distribution.Init(new(0.5f, 0.5f, 0), ref u);

    var timer = System.Diagnostics.Stopwatch.StartNew();
    for (uint i = 0; i < numRepeats; ++i)
    {
        distribution.Sample(new(0.5f, 0.8f));
    }
    Console.WriteLine($"Take 1 sample: {timer.ElapsedMilliseconds / (double)numRepeats}ms");
}

BenchPrepareSamples();
BenchInitDistribution();
BenchSampleDistribution();