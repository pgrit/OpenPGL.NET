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
    }
}
