using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class ProcessorTests
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
        processor.SetParameters(VoxTailParameters.Default with
        {
            Drive = 1f,
            Mix = 1f,
            OutputGainDb = 12f,
            DynamicBoost = 1f
        });
        float[] input = [float.NaN, float.PositiveInfinity, float.NegativeInfinity, 10f, -10f, float.MaxValue, -float.MaxValue];
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.All(output, sample => Assert.True(float.IsFinite(sample) && MathF.Abs(sample) <= 1f));
    }

    [Fact]
    public void Process_SanitizesNonFiniteParameters()
    {
        var processor = new VoxTailProcessor(48000);
        processor.SetParameters(VoxTailParameters.Default with
        {
            InputGainDb = float.NaN,
            Intensity = float.PositiveInfinity,
            Drive = float.NegativeInfinity,
            Tone = float.NaN,
            Mix = float.PositiveInfinity,
            TailAmount = float.NaN,
            TailDrop = float.PositiveInfinity,
            TailTimeMilliseconds = float.NaN,
            OutputGainDb = float.PositiveInfinity,
            DynamicBoost = float.NaN,
            VoiceThresholdDb = float.PositiveInfinity,
            TargetLevelDb = float.NegativeInfinity,
            MaximumBoostDb = float.NaN
        });
        float[] input = [0f, 0.25f, -0.5f, 0.75f];
        var output = new float[input.Length];

        processor.Process(input, output);

        Assert.All(output, sample => Assert.True(float.IsFinite(sample) && MathF.Abs(sample) <= 1f));
    }

    [Fact]
    public void Process_TailDropAttenuatesOnlyTheFallingTail()
    {
        const int sampleRate = 48000;
        var input = CreateVocalTail(sampleRate);
        var dryTail = ProcessTailDrop(input, sampleRate, 0f);
        var droppedTail = ProcessTailDrop(input, sampleRate, 1f);

        var earlyDifference = MeanAbsoluteDifference(
            dryTail.AsSpan((int)(sampleRate * 0.1f), (int)(sampleRate * 0.1f)),
            droppedTail.AsSpan((int)(sampleRate * 0.1f), (int)(sampleRate * 0.1f)));
        var tailDifference = MeanAbsoluteDifference(
            dryTail.AsSpan((int)(sampleRate * 0.6f), (int)(sampleRate * 0.2f)),
            droppedTail.AsSpan((int)(sampleRate * 0.6f), (int)(sampleRate * 0.2f)));

        Assert.True(earlyDifference < 0.01f, $"Early difference: {earlyDifference}");
        Assert.True(tailDifference > 0.01f, $"Tail difference: {tailDifference}");
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

    [Fact]
    public void Process_ProducesConsistentOutputAcrossVariableBlockSizes()
    {
        const int sampleRate = 48000;
        var input = CreateVocalTail(sampleRate);
        var wholeBlockProcessor = new VoxTailProcessor(sampleRate);
        var variableBlockProcessor = new VoxTailProcessor(sampleRate);
        var parameters = VoxTailParameters.Default with
        {
            Drive = 0.6f,
            Mix = 0.8f,
            TailAmount = 0.4f,
            TailDrop = 0.3f
        };
        wholeBlockProcessor.SetParameters(parameters);
        variableBlockProcessor.SetParameters(parameters);
        var wholeBlockOutput = new float[input.Length];
        var variableBlockOutput = new float[input.Length];

        wholeBlockProcessor.Process(input, wholeBlockOutput);
        var blockSizes = new[] { 17, 256, 63, 1024, 5, 511 };
        var offset = 0;
        var blockIndex = 0;
        while (offset < input.Length)
        {
            var count = Math.Min(blockSizes[blockIndex++ % blockSizes.Length], input.Length - offset);
            variableBlockProcessor.Process(input.AsSpan(offset, count), variableBlockOutput.AsSpan(offset, count));
            offset += count;
        }

        Assert.Equal(wholeBlockOutput, variableBlockOutput);
    }

    [Fact]
    public void Process_AutomatedToneAndTailTimeAreIndependentOfBlockSize()
    {
        const int sampleRate = 48000;
        var input = CreateVocalTail(sampleRate);
        var parameters = VoxTailParameters.Default with
        {
            Tone = 0.9f,
            TailTimeMilliseconds = 1200f,
            Drive = 0.6f,
            Mix = 0.8f,
            TailAmount = 0.4f,
            TailDrop = 0.3f
        };
        var wholeBlockProcessor = new VoxTailProcessor(sampleRate);
        var variableBlockProcessor = new VoxTailProcessor(sampleRate);
        wholeBlockProcessor.SetParameters(parameters);
        variableBlockProcessor.SetParameters(parameters);
        var wholeBlockOutput = new float[input.Length];
        var variableBlockOutput = new float[input.Length];

        wholeBlockProcessor.Process(input, wholeBlockOutput);
        var blockSizes = new[] { 17, 256, 63, 1024, 5, 511 };
        var offset = 0;
        var blockIndex = 0;
        while (offset < input.Length)
        {
            var count = Math.Min(blockSizes[blockIndex++ % blockSizes.Length], input.Length - offset);
            variableBlockProcessor.Process(input.AsSpan(offset, count), variableBlockOutput.AsSpan(offset, count));
            offset += count;
        }

        Assert.Equal(wholeBlockOutput, variableBlockOutput);
    }

    private static float[] ProcessTailDrop(float[] input, int sampleRate, float tailDrop)
    {
        var processor = new VoxTailProcessor(sampleRate);
        processor.SetParameters(VoxTailParameters.Default with { Mix = 1f, TailDrop = tailDrop });
        var output = new float[input.Length];
        processor.Process(input, output);
        return output;
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
