using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class VoxTailProcessorTests
{
    [Fact]
    public void Process_DefaultSettings_PreservesAudio()
    {
        var processor = new VoxTailProcessor(48000);
        var parameters = VoxTailParameters.Default;
        processor.SetParameters(parameters);
        float[] input = [0f, 0.25f, -0.5f, 0.75f, -0.9f];
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.Equal(input, output);
    }

    [Theory]
    [InlineData(44100)]
    [InlineData(48000)]
    public void Process_DriveAddsHarmonicsAtCommonSampleRates(int sampleRate)
    {
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with { Drive = 0.8f, Mix = 1f });
        var input = Enumerable.Range(0, sampleRate / 10)
            .Select(sample => 0.5f * MathF.Sin(2f * MathF.PI * 440f * sample / sampleRate))
            .ToArray();
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.True(MeanAbsoluteDifference(input, output) > 0.02f);
        Assert.All(output, sample => Assert.True(float.IsFinite(sample)));
    }

    [Fact]
    public void Process_HandlesNonFiniteInputAndBoundsProcessedOutput()
    {
        var processor = new VoxTailProcessor(48000);
        processor.SetParameters(VoxTailParameters.Default with { Drive = 1f, Mix = 1f, OutputGainDb = 12f });
        float[] input = [float.NaN, float.PositiveInfinity, float.NegativeInfinity, 10f, -10f];
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.All(output, sample => Assert.True(float.IsFinite(sample) && MathF.Abs(sample) <= 1f));
    }

    [Fact]
    public void Process_TailCharactersProduceDistinctTextures()
    {
        const int sampleRate = 48000;
        var input = CreateVocalTail(sampleRate);
        var outputs = new float[3][];

        for (var character = 0; character < outputs.Length; character++)
        {
            var processor = new VoxTailProcessor(sampleRate, 0xA341316Cu);
            processor.SetParameters(VoxTailParameters.Default with
            {
                Drive = 0.55f,
                Mix = 1f,
                TailAmount = 1f,
                TailTimeMilliseconds = 500f,
                TailCharacter = (TailCharacter)character
            });
            outputs[character] = new float[input.Length];
            processor.Process(input, outputs[character]);
        }

        Assert.True(MeanAbsoluteDifference(outputs[0], outputs[1]) > 0.0001f);
        Assert.True(MeanAbsoluteDifference(outputs[1], outputs[2]) > 0.0001f);
    }

    [Fact]
    public void Process_DoesNotAllocateForPreparedBuffers()
    {
        var processor = new VoxTailProcessor(48000);
        processor.SetParameters(VoxTailParameters.Default with { Drive = 0.4f, Mix = 0.7f });
        var input = new float[512];
        var output = new float[input.Length];
        processor.Process(input, output);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var block = 0; block < 64; block++)
        {
            processor.Process(input, output);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static float[] CreateVocalTail(int sampleRate)
    {
        var samples = new float[sampleRate];
        for (var index = 0; index < samples.Length; index++)
        {
            var time = (float)index / sampleRate;
            var amplitude = time < 0.35f ? 0.7f : 0.7f * MathF.Exp(-(time - 0.35f) * 8f);
            samples[index] = amplitude * MathF.Sin(2f * MathF.PI * 220f * time);
        }

        return samples;
    }

    private static float MeanAbsoluteDifference(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        double total = 0d;
        for (var index = 0; index < left.Length; index++)
        {
            total += Math.Abs(left[index] - right[index]);
        }

        return (float)(total / left.Length);
    }
}
