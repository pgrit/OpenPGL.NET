using System;
using System.Diagnostics;
using System.Numerics;
using OpenPGL.NET;

PathSegmentStorage storage = new();
storage.Reserve(10);

System.Random rng = new(1337);

int numrepeats = 10_000_000;
var stop = Stopwatch.StartNew();

for (int i = 0; i < numrepeats; ++i) {
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

    SamplerWrapper sampler = new(
        () => (float)rng.NextDouble(),
        () => new((float)rng.NextDouble(), (float)rng.NextDouble())
    );
    uint num = storage.PrepareSamples(false, sampler, true, true, true);

    storage.Clear();
}

Console.WriteLine($"{stop.ElapsedMilliseconds}");