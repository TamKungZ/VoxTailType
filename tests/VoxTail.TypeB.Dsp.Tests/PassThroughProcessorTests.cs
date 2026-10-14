using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class PassThroughProcessorTests
{
    [Fact]
    public void Process_CopiesInputExactly()
    {
        float[] input = [0f, 1f, -1f, 0.25f, -0.75f];
        var output = new float[input.Length];

        PassThroughProcessor.Process(input, output);

        Assert.Equal(input, output);
    }

    [Fact]
    public void Process_PreservesNonFiniteSamples()
    {
        float[] input = [float.NaN, float.PositiveInfinity, float.NegativeInfinity];
        var output = new float[input.Length];

        PassThroughProcessor.Process(input, output);

        Assert.True(float.IsNaN(output[0]));
        Assert.Equal(float.PositiveInfinity, output[1]);
        Assert.Equal(float.NegativeInfinity, output[2]);
    }

    [Fact]
    public void Process_CopiesSilence()
    {
        var input = new float[128];
        var output = Enumerable.Repeat(1f, input.Length).ToArray();

        PassThroughProcessor.Process(input, output);

        Assert.All(output, sample => Assert.Equal(0f, sample));
    }

    [Fact]
    public void Process_SupportsContinuousBlocks()
    {
        float[] input = [0.1f, -0.2f, 0.3f, -0.4f, 0.5f, -0.6f];
        var output = new float[input.Length];

        PassThroughProcessor.Process(input.AsSpan(0, 2), output.AsSpan(0, 2));
        PassThroughProcessor.Process(input.AsSpan(2, 4), output.AsSpan(2, 4));

        Assert.Equal(input, output);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void Process_PreservesSineWaveAtCommonSampleRates(int sampleRate)
    {
        var input = Enumerable.Range(0, sampleRate)
            .Select(sample => MathF.Sin(2f * MathF.PI * 440f * sample / sampleRate))
            .ToArray();
        var output = new float[input.Length];

        PassThroughProcessor.Process(input, output);

        Assert.Equal(input, output);
    }

    [Fact]
    public void Process_DoesNotAllocateForPreparedBuffers()
    {
        var input = new float[512];
        var output = new float[input.Length];
        PassThroughProcessor.Process(input, output);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var block = 0; block < 64; block++)
        {
            PassThroughProcessor.Process(input, output);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void Process_AllowsOverlappingBuffers()
    {
        float[] buffer = [1f, 2f, 3f, 4f];

        PassThroughProcessor.Process(buffer.AsSpan(0, 3), buffer.AsSpan(1, 3));

        Assert.Equal([1f, 1f, 2f, 3f], buffer);
    }

    [Fact]
    public void Process_RejectsDifferentBufferLengths()
    {
        var input = new float[2];
        var output = new float[1];

        Assert.Throws<ArgumentException>(() => PassThroughProcessor.Process(input, output));
    }
}
