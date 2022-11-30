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

            // Surface hit point
            PathSegment segment = storage.NextSegment();
            segment.Position = new(0, 0, 3);
            segment.Normal = Vector3.UnitZ;
            segment.DirectionIn = Vector3.UnitZ;
            segment.DirectionOut = Vector3.UnitZ;
            segment.ScatteredContribution = new(0, 0, 0);
            segment.ScatteringWeight = new(1, 1, 1);
            segment.PDFDirectionIn = 1;
            segment.Roughness = 1;
            segment.RussianRouletteProbability = 1;
            segment.Eta = 4;

            // Light vertex
            segment = storage.NextSegment();
            segment.Position = new(0, 0, 5);
            segment.DirectContribution = new(10, 10, 10);

            System.Random rng = new(1337);
            uint num = storage.PrepareSamples(false, true, true);

            Assert.Equal(1u, num);
            Assert.Equal(1, storage.Samples.Length);

            var sample = storage.Samples[0];
            Assert.Equal(2, sample.Distance);
            Assert.Equal(10, sample.Weight);
            Assert.Equal(new Vector3(0, 0, 3), sample.Position);
            Assert.Equal(new Vector3(0, 0, 1), sample.Direction);
            Assert.Equal(1, sample.Pdf);
        }

        [Fact]
        public void PathSegments_ShouldBeCleared() {
            PathSegmentStorage storage = new();
            storage.Reserve(10);

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

            // Clear and add the same two segments again
            storage.Clear();

            segment = storage.NextSegment();
            segment.Position = new(0, 0, 3);
            segment.Normal = Vector3.UnitZ;
            segment.DirectionIn = Vector3.UnitZ;
            segment.DirectionOut = Vector3.UnitZ;
            segment.ScatteredContribution = new(0, 0, 0);
            segment.ScatteringWeight = new(1, 1, 1);
            segment.TransmittanceWeight = Vector3.One;

            // Light vertex
            segment = storage.NextSegment();
            segment.Position = new(0, 0, 5);
            segment.Normal = new(0, 0, -1);
            segment.DirectContribution = new(10, 10, 10);

            uint num = storage.PrepareSamples(false, true, true);

            Assert.Equal(1u, num);
            Assert.Equal(1, storage.Samples.Length);

            var sample = storage.Samples[0];
            Assert.Equal(2, sample.Distance);
            Assert.Equal(10, sample.Weight);
            Assert.Equal(new Vector3(0, 0, 3), sample.Position);
            Assert.Equal(new Vector3(0, 0, 1), sample.Direction);
            Assert.Equal(1, sample.Pdf);
        }

        [Fact]
        public void SampleStorage_ShouldContainTheSample() {
            OpenPGL.NET.SampleStorage storage = new();
            storage.Reserve(10, 10);
            storage.AddSample(new() {
                Direction = new(1, 2, 3),
                Distance = 42,
                Pdf = 123,
                Position = new(4, 5, 6),
                Weight = 133.7f,
                Flags = 0
            });

            storage.AddSamples(new SampleData[] {
                new() {
                    Direction = new(1, 2, 3),
                    Distance = 42,
                    Pdf = 123,
                    Position = new(4,5,6),
                    Weight = 10,
                    Flags = SampleData.SampleFlags.EInsideVolume
                },
                new() {
                    Direction = new(1, 2, 3),
                    Distance = 42,
                    Pdf = 123,
                    Position = new(4,5,6),
                    Weight = 20,
                    Flags = 0
                }
            });

            Assert.Equal(2u, storage.SizeSurface);
            Assert.Equal(1u, storage.SizeVolume);

            storage.Clear();

            Assert.Equal(0u, storage.SizeSurface);
            Assert.Equal(0u, storage.SizeVolume);
        }

        [Fact]
        public void PathSegment_RandomAccess() {
            PathSegmentStorage storage = new();
            storage.Reserve(10);

            PathSegment segment1 = storage.NextSegment();
            segment1.Position = new(0, 0, 0);

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

            PathSegment segmentGet = storage.LastSegment;
            segmentGet.Position = new(0, 1, 0);

            PathSegment segment3 = storage.NextSegment();
            segment3.Position = new(0, 0, 0);

            System.Random rng = new(1337);
            uint num = storage.PrepareSamples(false, true, true);

            Assert.Equal(1u, num);
            Assert.Equal(1, storage.Samples.Length);
            Assert.Equal(1, storage.Samples[0].Distance);
        }
    }
}
