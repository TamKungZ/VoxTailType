using NPlug;
using VoxTail.TypeB.Ui;

namespace VoxTail.TypeB.Plugin;

public sealed class Controller : AudioController<Model>, IEditorBridge
{
    public static readonly Guid ClassId = new("7f573153-82aa-4292-a701-357499ce2ad8");

    private EditorView? _editorView;
    private AudioParameter? _editingParameter;

    protected override IAudioPluginView? CreateView()
    {
        _editorView = new EditorView(this);
        return _editorView;
    }

    public double GetEditorParameterValue(EditorParameter parameter) =>
        Model.GetParameterById(GetParameterId(parameter)).NormalizedValue;

    public AudioParameterId GetHostParameterId(EditorParameter parameter) =>
        GetParameterId(parameter);

    public void BeginEditorParameterEdit(EditorParameter parameter)
    {
        EndEditorParameterEdit();
        _editingParameter = Model.GetParameterById(GetParameterId(parameter));
        BeginEditParameter(_editingParameter);
    }

    public void SetEditorParameterValue(EditorParameter parameterId, double normalizedValue)
    {
        var parameter = Model.GetParameterById(GetParameterId(parameterId));
        parameter.NormalizedValue = Math.Clamp(normalizedValue, 0d, 1d);
    }

    public void EndEditorParameterEdit()
    {
        if (_editingParameter is null)
        {
            return;
        }

        EndEditParameter();
        _editingParameter = null;
    }

    public bool RequestMeterFrame()
    {
        if (Host is null ||
            ConnectionPoint is null ||
            !Host.TryCreateMessage(MeterMessages.Request, out var request))
        {
            return false;
        }

        using (request)
        {
            ConnectionPoint.Notify(request);
        }

        return true;
    }

    protected override void OnMessage(AudioMessage message)
    {
        if (message.Id != MeterMessages.Response)
        {
            return;
        }

        var attributes = message.Attributes;
        if (!attributes.TryGetFloat32(MeterMessages.InputRms, out var inputRms) ||
            !attributes.TryGetFloat32(MeterMessages.OutputRms, out var outputRms) ||
            !attributes.TryGetFloat32(MeterMessages.BoostDb, out var boostDb) ||
            !attributes.TryGetBool(MeterMessages.VoiceActive, out var voiceActive))
        {
            return;
        }

        _editorView?.OnMeterFrame(new EditorMeterFrame(inputRms, outputRms, boostDb, voiceActive));
    }

    private static int GetParameterId(EditorParameter parameter) => parameter switch
    {
        EditorParameter.InputGain => ParameterIds.InputGain,
        EditorParameter.Intensity => ParameterIds.Intensity,
        EditorParameter.Drive => ParameterIds.Drive,
        EditorParameter.Tone => ParameterIds.Tone,
        EditorParameter.Mix => ParameterIds.Mix,
        EditorParameter.TailAmount => ParameterIds.TailAmount,
        EditorParameter.TailDrop => ParameterIds.TailDrop,
        EditorParameter.TailTime => ParameterIds.TailTime,
        EditorParameter.TailCharacter => ParameterIds.TailCharacter,
        EditorParameter.OutputGain => ParameterIds.OutputGain,
        EditorParameter.DynamicBoost => ParameterIds.DynamicBoost,
        EditorParameter.VoiceThreshold => ParameterIds.VoiceThreshold,
        EditorParameter.TargetLevel => ParameterIds.TargetLevel,
        EditorParameter.MaximumBoost => ParameterIds.MaximumBoost,
        _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter, "Unknown editor parameter.")
    };
}
