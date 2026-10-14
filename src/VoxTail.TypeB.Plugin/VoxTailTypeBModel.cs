using NPlug;

namespace VoxTail.TypeB.Plugin;

public sealed class VoxTailTypeBModel : AudioProcessorModel
{
    public VoxTailTypeBModel() : base("VoxTail.TypeB", new VoxTailPresetBuilder())
    {
        AddByPassParameter("Bypass");
        AddParameter(new AudioRangeParameter("Input Gain", "dB", VoxTailParameterIds.InputGain, -12, 12, 0));
        AddParameter(new AudioRangeParameter("Intensity", "%", VoxTailParameterIds.Intensity, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Drive", "%", VoxTailParameterIds.Drive, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tone", "%", VoxTailParameterIds.Tone, 0, 100, 50));
        AddParameter(new AudioRangeParameter("Clean / Wet Mix", "%", VoxTailParameterIds.Mix, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tail Amount", "%", VoxTailParameterIds.TailAmount, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tail Time", "ms", VoxTailParameterIds.TailTime, 50, 1500, 250));
        AddParameter(new AudioStringListParameter("Tail Character", ["Crackle", "Breaks", "Glitch"], id: VoxTailParameterIds.TailCharacter));
        AddParameter(new AudioRangeParameter("Output Gain", "dB", VoxTailParameterIds.OutputGain, -12, 12, 0));
    }
}

internal sealed class VoxTailPresetBuilder : AudioProgramListBuilder<VoxTailTypeBModel>
{
    public VoxTailPresetBuilder() : base("VoxTail Type-B Presets", 1)
    {
        Add(model => Create(model, "Clean", 0.5, 0.0, 0.0, 0.5, 0.0, 0.0, 0.137931, 0.0, 0.5));
        Add(model => Create(model, "Bite", 0.5, 0.25, 0.38, 0.62, 0.55, 0.1, 0.20, 0.0, 0.5));
        Add(model => Create(model, "Shout", 0.5, 0.7, 0.65, 0.68, 0.8, 0.15, 0.24, 0.0, 0.48));
        Add(model => Create(model, "Cracked Tail", 0.5, 0.45, 0.5, 0.58, 0.75, 0.8, 0.31, 0.0, 0.5));
        Add(model => Create(model, "Broken Tail", 0.5, 0.5, 0.55, 0.58, 0.8, 0.85, 0.38, 0.5, 0.5));
        Add(model => Create(model, "Glitch Tail", 0.5, 0.55, 0.6, 0.65, 0.85, 1.0, 0.31, 1.0, 0.48));
    }

    private static AudioProgram Create(
        VoxTailTypeBModel model,
        string name,
        double inputGain,
        double intensity,
        double drive,
        double tone,
        double mix,
        double tailAmount,
        double tailTime,
        double tailCharacter,
        double outputGain)
    {
        SetValues(model, inputGain, intensity, drive, tone, mix, tailAmount, tailTime, tailCharacter, outputGain);
        var program = new AudioProgram(name);
        program.SetProgramDataFromUnit(model);
        SetValues(model, 0.5, 0.0, 0.0, 0.5, 0.0, 0.0, 0.137931, 0.0, 0.5);
        return program;
    }

    private static void SetValues(
        VoxTailTypeBModel model,
        double inputGain,
        double intensity,
        double drive,
        double tone,
        double mix,
        double tailAmount,
        double tailTime,
        double tailCharacter,
        double outputGain)
    {
        model.GetParameterById(VoxTailParameterIds.InputGain).NormalizedValue = inputGain;
        model.GetParameterById(VoxTailParameterIds.Intensity).NormalizedValue = intensity;
        model.GetParameterById(VoxTailParameterIds.Drive).NormalizedValue = drive;
        model.GetParameterById(VoxTailParameterIds.Tone).NormalizedValue = tone;
        model.GetParameterById(VoxTailParameterIds.Mix).NormalizedValue = mix;
        model.GetParameterById(VoxTailParameterIds.TailAmount).NormalizedValue = tailAmount;
        model.GetParameterById(VoxTailParameterIds.TailTime).NormalizedValue = tailTime;
        model.GetParameterById(VoxTailParameterIds.TailCharacter).NormalizedValue = tailCharacter;
        model.GetParameterById(VoxTailParameterIds.OutputGain).NormalizedValue = outputGain;
    }
}

internal static class VoxTailParameterIds
{
    public const int InputGain = 100;
    public const int Intensity = 101;
    public const int Drive = 102;
    public const int Tone = 103;
    public const int Mix = 104;
    public const int TailAmount = 105;
    public const int TailTime = 106;
    public const int TailCharacter = 107;
    public const int OutputGain = 108;
}
