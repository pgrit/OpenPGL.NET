using System.Numerics;
using Xunit;

namespace OpenPGL.NET.Tests {
    public class SampleStorage {
        [Fact]
        public void WrittenSamples_ShouldBeReadBack() {
            PathSegmentStorage storage = new();
            storage.Reserve(10);
            storage.AddSample(new() {
                Direction = new(1, 2, 3),
                Distance = 42,
                Pdf = 123,
                Position = new(4,5,6),
                Weight = 133.7f,
                Flags = 0
            });

            Assert.Equal(1, storage.Samples.Length);

            var sample = storage.Samples[0];
            Assert.Equal(new(1, 2, 3), sample.Direction);
            Assert.Equal(42, sample.Distance);
            Assert.Equal(123, sample.Pdf);
            Assert.Equal(new(4,5,6), sample.Position);
            Assert.Equal(133.7f, sample.Weight);
            Assert.Equal(0u, (uint)sample.Flags);
        }

        [Fact]
        public void PathSegments_ShouldBeConverted() {
            PathSegmentStorage storage = new();
            storage.Reserve(10);

            // Dummy "camera" ray
            storage.NextSegment();

            PathSegment segment = storage.NextSegment();
            segment.Position = new(1, 2, 3);
            segment.MiWeight = 1;
            segment.Normal = Vector3.UnitZ;
            segment.PDFDirectionIn = 1;
            segment.DirectionIn = Vector3.UnitZ;
            segment.DirectionOut = Vector3.UnitZ;
            segment.Eta = 1.4f;
            segment.Roughness = 0.5f;
            segment.RussianRouletteProbability = 1.0f;
            segment.ScatteredContribution = new(10, 10, 10);
            segment.ScatteringWeight = new(1, 1, 1);

            // Dummy "light" ray
            storage.NextSegment();

            System.Random rng = new(1337);
            SamplerWrapper sampler = new(
                () => (float)rng.NextDouble(),
                () => new((float)rng.NextDouble(), (float)rng.NextDouble())
            );
            uint num = storage.PrepareSamples(false, sampler, true, true);

            Assert.Equal(1u, num);
            Assert.Equal(1, storage.Samples.Length);

            // TOOD check the actual values once conventions are clear
        }
    }
}
