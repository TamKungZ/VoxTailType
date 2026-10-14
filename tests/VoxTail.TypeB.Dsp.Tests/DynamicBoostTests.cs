using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class DynamicBoostTests
{
    [Fact]
    public void Process_RaisesQuietActiveAudioTowardTarget()
    {
        const int sampleRate = 48000;
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with
        {
            DynamicBoost = 1f,
            VoiceThresholdDb = -54f,
            TargetLevelDb = -18f,
            MaximumBoostDb = 12f
        });
        var input = CreateSine(sampleRate, 0.02f, 440f);
        var output = new float[input.Length];

        processor.Process(input, output);

        var inputRms = Rms(input.AsSpan(sampleRate / 2));
        var outputRms = Rms(output.AsSpan(sampleRate / 2));
        Assert.True(outputRms > inputRms * 2f, $"Expected quiet voice to be boosted. Input RMS: {inputRms}; output RMS: {outputRms}");
        Assert.All(output, sample => Assert.True(float.IsFinite(sample) && MathF.Abs(sample) <= 1f));
    }

    [Fact]
    public void Process_DoesNotRaiseAlreadyLoudAudio()
    {
        const int sampleRate = 48000;
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with
        {
            DynamicBoost = 1f,
            VoiceThresholdDb = -54f,
            TargetLevelDb = -18f,
            MaximumBoostDb = 12f
        });
        var input = CreateSine(sampleRate, 0.5f, 440f);
        var output = new float[input.Length];

        processor.Process(input, output);

        var inputRms = Rms(input.AsSpan(sampleRate / 2));
        var outputRms = Rms(output.AsSpan(sampleRate / 2));
        Assert.True(MathF.Abs(outputRms - inputRms) < 0.001f, $"Already-loud audio should remain at unity. Input RMS: {inputRms}; output RMS: {outputRms}");
    }

    [Fact]
    public void Process_ReducesGainQuicklyWhenInputBecomesLoud()
    {
        const int sampleRate = 48000;
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with
        {
            DynamicBoost = 1f,
            VoiceThresholdDb = -54f,
            TargetLevelDb = -18f,
            MaximumBoostDb = 12f
        });
        var input = new float[sampleRate];
        for (var index = 0; index < input.Length; index++)
        {
            var amplitude = index < sampleRate / 3 ? 0.02f : 0.5f;
            input[index] = amplitude * MathF.Sin(2f * MathF.PI * 440f * index / sampleRate);
        }

        var output = new float[input.Length];
        processor.Process(input, output);

        var inputLoudRms = Rms(input.AsSpan(sampleRate * 2 / 3));
        var outputLoudRms = Rms(output.AsSpan(sampleRate * 2 / 3));
        Assert.InRange(outputLoudRms, inputLoudRms * 0.9f, inputLoudRms * 1.1f);
    }

    [Fact]
    public void Process_LeavesSilenceAndBelowThresholdNoiseAlone()
    {
        const int sampleRate = 48000;
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with
        {
            DynamicBoost = 1f,
            VoiceThresholdDb = -54f,
            TargetLevelDb = -18f,
            MaximumBoostDb = 18f
        });
        var input = CreateSine(sampleRate, 0.0005f, 440f);
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.Equal(input, output);
    }

    [Fact]
    public void Process_DoesNotAllocatePerBlock()
    {
        var processor = new VoxTailProcessor(48000);
        processor.SetParameters(VoxTailParameters.Default with { DynamicBoost = 1f });
        var input = CreateSine(48000, 0.02f, 440f).AsSpan(0, 512).ToArray();
        var output = new float[input.Length];
        processor.Process(input, output);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var block = 0; block < 64; block++)
        {
            processor.Process(input, output);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    private static float[] CreateSine(int sampleRate, float amplitude, float frequency)
    {
        var samples = new float[sampleRate];
        for (var index = 0; index < samples.Length; index++)
        {
            samples[index] = amplitude * MathF.Sin(2f * MathF.PI * frequency * index / sampleRate);
        }

        return samples;
    }

    private static float Rms(ReadOnlySpan<float> samples)
    {
        double sum = 0d;
        foreach (var sample in samples)
        {
            sum += sample * sample;
        }

        return (float)Math.Sqrt(sum / samples.Length);
    }
}
