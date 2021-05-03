using System.Numerics;
using Xunit;

namespace OpenPGL.NET.Tests {
    public class FieldTest {
        [Fact]
        public void ShouldNotSegfault() {
            Field field = new();

            SamplerWrapper sampler = new(
                () => 1,
                () => new(1, 1)
            );
            var region = field.GetSurfaceRegion(Vector3.Zero, sampler);

            Assert.NotNull(region);
        }

        [Fact]
        public void RegionShouldBeFound() {
            Field field = new();

            // Generate a batch of dummy samples on the xy plane in [0,1]x[0,1]
            uint num = 100000;
            OpenPGL.NET.SampleStorage samples = new();
            samples.Reserve(num, 0);
            System.Random rng = new(1337);
            for (int i = 0; i < num; ++i)
                samples.AddSample(new() {
                    Position = new((float)rng.NextDouble(), (float)rng.NextDouble(), 0),
                    Direction = Vector3.UnitZ,
                    Distance = 10,
                    Pdf = 1,
                    Weight = 42
                });

            // Train the model
            field.Update(samples, 1);

            // Check that a valid region is found in the center
            SamplerWrapper sampler = new(
                () => (float)rng.NextDouble(),
                () => new((float)rng.NextDouble(), (float)rng.NextDouble())
            );
            var region = field.GetSurfaceRegion(new(0.5f, 0.5f, 0), sampler);

            Assert.NotNull(region);
            Assert.True(region.IsValid);
        }

        [Fact]
        public void RegionShouldBeFound_NoKnn() {
            Field field = new(new(){
                SpatialSettings = new KdTreeSettings() { KnnLookup = false }
            });

            // Generate a batch of dummy samples on the xy plane in [0,1]x[0,1]
            uint num = 100000;
            OpenPGL.NET.SampleStorage samples = new();
            samples.Reserve(num, 0);
            System.Random rng = new(1337);
            for (int i = 0; i < num; ++i)
                samples.AddSample(new() {
                    Position = new((float)rng.NextDouble(), (float)rng.NextDouble(), 0),
                    Direction = Vector3.UnitZ,
                    Distance = 10,
                    Pdf = 1,
                    Weight = 42
                });

            // Train the model
            field.Update(samples, 1);

            // Check that a valid region is found in the center
            SamplerWrapper sampler = new(
                () => (float)rng.NextDouble(),
                () => new((float)rng.NextDouble(), (float)rng.NextDouble())
            );
            var region = field.GetSurfaceRegion(new(0.5f, 0.5f, 0), sampler);

            Assert.NotNull(region);
            Assert.True(region.IsValid);
        }
    }
}