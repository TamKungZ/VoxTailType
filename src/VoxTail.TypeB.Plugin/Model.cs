using NPlug;

namespace VoxTail.TypeB.Plugin;

public sealed class Model : AudioProcessorModel
{
    public Model() : base("VoxTail.TypeB", new Presets())
    {
        AddByPassParameter("Bypass");
        AddParameter(new AudioRangeParameter("Input Gain", "dB", ParameterIds.InputGain, -12, 12, 0));
        AddParameter(new AudioRangeParameter("Intensity", "%", ParameterIds.Intensity, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Drive", "%", ParameterIds.Drive, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tone", "%", ParameterIds.Tone, 0, 100, 50));
        AddParameter(new AudioRangeParameter("Clean / Wet Mix", "%", ParameterIds.Mix, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tail Amount", "%", ParameterIds.TailAmount, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tail Drop", "%", ParameterIds.TailDrop, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Tail Time", "ms", ParameterIds.TailTime, 50, 1500, 250));
        AddParameter(new AudioStringListParameter("Tail Character", ["Crackle", "Breaks", "Glitch"], id: ParameterIds.TailCharacter));
        AddParameter(new AudioRangeParameter("Output Gain", "dB", ParameterIds.OutputGain, -12, 12, 0));
        AddParameter(new AudioRangeParameter("Dynamic Boost", "%", ParameterIds.DynamicBoost, 0, 100, 0));
        AddParameter(new AudioRangeParameter("Voice Threshold", "dB", ParameterIds.VoiceThreshold, -72, -24, -54));
        AddParameter(new AudioRangeParameter("Target Level", "dBFS", ParameterIds.TargetLevel, -36, -12, -24));
        AddParameter(new AudioRangeParameter("Maximum Boost", "dB", ParameterIds.MaximumBoost, 0, 18, 9));
    }
}
