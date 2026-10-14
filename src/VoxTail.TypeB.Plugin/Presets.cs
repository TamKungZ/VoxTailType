using NPlug;

namespace VoxTail.TypeB.Plugin;

internal sealed class Presets : AudioProgramListBuilder<Model>
{
    public Presets() : base("VoxTail Type-B Presets", 1)
    {
        Add(model => Create(model, "Clean", 0.5, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.137931, 0.0, 0.5));
        Add(model => Create(model, "Bite", 0.5, 0.25, 0.38, 0.62, 0.55, 0.1, 0.1, 0.20, 0.0, 0.5));
        Add(model => Create(model, "Shout", 0.5, 0.7, 0.65, 0.68, 0.8, 0.15, 0.15, 0.24, 0.0, 0.48));
        Add(model => Create(model, "Cracked Tail", 0.5, 0.45, 0.5, 0.58, 0.75, 0.8, 0.5, 0.31, 0.0, 0.5));
        Add(model => Create(model, "Broken Tail", 0.5, 0.5, 0.55, 0.58, 0.8, 0.85, 0.65, 0.38, 0.5, 0.5));
        Add(model => Create(model, "Glitch Tail", 0.5, 0.55, 0.6, 0.65, 0.85, 1.0, 0.4, 0.31, 1.0, 0.48));
    }

    private static AudioProgram Create(
        Model model,
        string name,
        double inputGain,
        double intensity,
        double drive,
        double tone,
        double mix,
        double tailAmount,
        double tailDrop,
        double tailTime,
        double tailCharacter,
        double outputGain)
    {
        SetValues(model, inputGain, intensity, drive, tone, mix, tailAmount, tailDrop, tailTime, tailCharacter, outputGain);
        var program = new AudioProgram(name);
        program.SetProgramDataFromUnit(model);
        SetValues(model, 0.5, 0.0, 0.0, 0.5, 0.0, 0.0, 0.0, 0.137931, 0.0, 0.5);
        return program;
    }

    private static void SetValues(
        Model model,
        double inputGain,
        double intensity,
        double drive,
        double tone,
        double mix,
        double tailAmount,
        double tailDrop,
        double tailTime,
        double tailCharacter,
        double outputGain)
    {
        model.GetParameterById(ParameterIds.InputGain).NormalizedValue = inputGain;
        model.GetParameterById(ParameterIds.Intensity).NormalizedValue = intensity;
        model.GetParameterById(ParameterIds.Drive).NormalizedValue = drive;
        model.GetParameterById(ParameterIds.Tone).NormalizedValue = tone;
        model.GetParameterById(ParameterIds.Mix).NormalizedValue = mix;
        model.GetParameterById(ParameterIds.TailAmount).NormalizedValue = tailAmount;
        model.GetParameterById(ParameterIds.TailDrop).NormalizedValue = tailDrop;
        model.GetParameterById(ParameterIds.TailTime).NormalizedValue = tailTime;
        model.GetParameterById(ParameterIds.TailCharacter).NormalizedValue = tailCharacter;
        model.GetParameterById(ParameterIds.OutputGain).NormalizedValue = outputGain;
        model.GetParameterById(ParameterIds.DynamicBoost).NormalizedValue = 0.0;
        model.GetParameterById(ParameterIds.VoiceThreshold).NormalizedValue = 0.375;
        model.GetParameterById(ParameterIds.TargetLevel).NormalizedValue = 0.5;
        model.GetParameterById(ParameterIds.MaximumBoost).NormalizedValue = 0.5;
    }
}
