using NPlug;
using VoxTail.TypeB.Dsp;

namespace VoxTail.TypeB.Plugin;

public sealed class VoxTailTypeBProcessor : AudioProcessor<VoxTailTypeBModel>
{
    public static readonly Guid ClassId = new("1b3718ed-03a5-4a00-8879-cc3245cc8938");

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

    protected override void ProcessMain(in AudioProcessData data)
    {
        var input = data.Input[0];
        var output = data.Output[0];
        var sharedChannelCount = Math.Min(input.ChannelCount, output.ChannelCount);

        for (var channel = 0; channel < sharedChannelCount; channel++)
        {
            var inputChannel = input.GetChannelSpanAsFloat32(ProcessSetupData, data, channel);
            var outputChannel = output.GetChannelSpanAsFloat32(ProcessSetupData, data, channel);
            PassThroughProcessor.Process(inputChannel, outputChannel);
        }

        for (var channel = sharedChannelCount; channel < output.ChannelCount; channel++)
        {
            output.GetChannelSpanAsFloat32(ProcessSetupData, data, channel).Clear();
        }
    }
}
