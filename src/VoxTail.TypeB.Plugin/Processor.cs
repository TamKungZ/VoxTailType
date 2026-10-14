using NPlug;
using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Plugin;

public sealed class Processor : AudioProcessor<Model>
{
    public static readonly Guid ClassId = new("1b3718ed-03a5-4a00-8879-cc3245cc8938");
    private VoxTailProcessor[]? _channelProcessors;
    private float _meterInputRms;
    private float _meterOutputRms;
    private float _meterBoostDb;
    private int _meterVoiceActive;

    public Processor() : base(AudioSampleSizeSupport.Float32)
    {
    }

    public override Guid ControllerClassId => Controller.ClassId;

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

        if (sharedChannelCount > 0)
        {
            var inputRms = 0f;
            var outputRms = 0f;
            var boostDb = 0f;
            var voiceActive = false;
            for (var channel = 0; channel < sharedChannelCount; channel++)
            {
                inputRms += processors[channel].MeterInputRms;
                outputRms += processors[channel].MeterOutputRms;
                boostDb += processors[channel].MeterBoostDb;
                voiceActive |= processors[channel].MeterVoiceActive;
            }

            var channelScale = 1f / sharedChannelCount;
            Volatile.Write(ref _meterInputRms, inputRms * channelScale);
            Volatile.Write(ref _meterOutputRms, outputRms * channelScale);
            Volatile.Write(ref _meterBoostDb, boostDb * channelScale);
            Volatile.Write(ref _meterVoiceActive, voiceActive ? 1 : 0);
        }
    }

    protected override void OnMessage(AudioMessage message)
    {
        if (message.Id != MeterMessages.Request ||
            Host is null ||
            !Host.TryCreateMessage(MeterMessages.Response, out var response))
        {
            return;
        }

        using (response)
        {
            var attributes = response.Attributes;
            if (!attributes.TrySetFloat32(MeterMessages.InputRms, Volatile.Read(ref _meterInputRms)) ||
                !attributes.TrySetFloat32(MeterMessages.OutputRms, Volatile.Read(ref _meterOutputRms)) ||
                !attributes.TrySetFloat32(MeterMessages.BoostDb, Volatile.Read(ref _meterBoostDb)) ||
                !attributes.TrySetBool(MeterMessages.VoiceActive, Volatile.Read(ref _meterVoiceActive) != 0))
            {
                return;
            }

            ConnectionPoint?.Notify(response);
        }
    }

    private VoxTailParameters GetParameters()
    {
        static float Read(AudioProcessorModel model, int id) => (float)model.GetNormalizedValueById(id);
        static float Decibels(float normalized) => (normalized * 24f) - 12f;

        var tailCharacter = (TailCharacter)Math.Clamp(
            (int)MathF.Round(Read(Model, ParameterIds.TailCharacter) * 2f),
            0,
            2);

        return new VoxTailParameters(
            Decibels(Read(Model, ParameterIds.InputGain)),
            Read(Model, ParameterIds.Intensity),
            Read(Model, ParameterIds.Drive),
            Read(Model, ParameterIds.Tone),
            Read(Model, ParameterIds.Mix),
            Read(Model, ParameterIds.TailAmount),
            Read(Model, ParameterIds.TailDrop),
            50f + (Read(Model, ParameterIds.TailTime) * 1450f),
            tailCharacter,
            Decibels(Read(Model, ParameterIds.OutputGain)),
            Read(Model, ParameterIds.DynamicBoost),
            -72f + (Read(Model, ParameterIds.VoiceThreshold) * 48f),
            -36f + (Read(Model, ParameterIds.TargetLevel) * 24f),
            Read(Model, ParameterIds.MaximumBoost) * 18f);
    }
}
