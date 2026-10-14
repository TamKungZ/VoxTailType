using NPlug;
using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Plugin;

public sealed class VoxTailTypeBProcessor : AudioProcessor<VoxTailTypeBModel>
{
    public static readonly Guid ClassId = new("1b3718ed-03a5-4a00-8879-cc3245cc8938");
    private VoxTailProcessor[]? _channelProcessors;

    public VoxTailTypeBProcessor() : base(AudioSampleSizeSupport.Float32)
    {
    }

    public override Guid ControllerClassId => VoxTailTypeBController.ClassId;

    protected override bool Initialize(AudioHostApplication host)
    {
        AddAudioInput("Audio Input", SpeakerArrangement.SpeakerStereo);
        AddAudioOutput("Audio Output", SpeakerArrangement.SpeakerStereo);
        return true;
    }

    protected override void OnSetupProcessing(in AudioProcessSetupData setup)
    {
        _channelProcessors =
        [
            new VoxTailProcessor((float)setup.SampleRate, 0xA341316Cu),
            new VoxTailProcessor((float)setup.SampleRate, 0xC8013EA4u)
        ];
    }

    protected override void ProcessMain(in AudioProcessData data)
    {
        var input = data.Input[0];
        var output = data.Output[0];
        var processors = _channelProcessors ?? throw new InvalidOperationException("The audio processor has not been set up.");
        var parameters = GetParameters();
        var sharedChannelCount = Math.Min(Math.Min(input.ChannelCount, output.ChannelCount), processors.Length);

        for (var channel = 0; channel < sharedChannelCount; channel++)
        {
            var inputChannel = input.GetChannelSpanAsFloat32(ProcessSetupData, data, channel);
            var outputChannel = output.GetChannelSpanAsFloat32(ProcessSetupData, data, channel);
            processors[channel].SetParameters(parameters);
            processors[channel].Process(inputChannel, outputChannel);
        }

        for (var channel = sharedChannelCount; channel < output.ChannelCount; channel++)
        {
            output.GetChannelSpanAsFloat32(ProcessSetupData, data, channel).Clear();
        }
    }

    private VoxTailParameters GetParameters()
    {
        static float Read(AudioProcessorModel model, int id) => (float)model.GetNormalizedValueById(id);
        static float Decibels(float normalized) => (normalized * 24f) - 12f;

        var tailCharacter = (TailCharacter)Math.Clamp(
            (int)MathF.Round(Read(Model, VoxTailParameterIds.TailCharacter) * 2f),
            0,
            2);

        return new VoxTailParameters(
            Decibels(Read(Model, VoxTailParameterIds.InputGain)),
            Read(Model, VoxTailParameterIds.Intensity),
            Read(Model, VoxTailParameterIds.Drive),
            Read(Model, VoxTailParameterIds.Tone),
            Read(Model, VoxTailParameterIds.Mix),
            Read(Model, VoxTailParameterIds.TailAmount),
            Read(Model, VoxTailParameterIds.TailDrop),
            50f + (Read(Model, VoxTailParameterIds.TailTime) * 1450f),
            tailCharacter,
            Decibels(Read(Model, VoxTailParameterIds.OutputGain)),
            Read(Model, VoxTailParameterIds.DynamicBoost),
            -72f + (Read(Model, VoxTailParameterIds.VoiceThreshold) * 48f),
            -36f + (Read(Model, VoxTailParameterIds.TargetLevel) * 24f),
            Read(Model, VoxTailParameterIds.MaximumBoost) * 18f);
    }
}
