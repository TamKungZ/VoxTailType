using NPlug;

namespace VoxTail.TypeB.Ui;

public interface IEditorBridge
{
    double GetEditorParameterValue(EditorParameter parameter);
    AudioParameterId GetHostParameterId(EditorParameter parameter);
    void BeginEditorParameterEdit(EditorParameter parameter);
    void SetEditorParameterValue(EditorParameter parameter, double normalizedValue);
    void EndEditorParameterEdit();
    bool RequestMeterFrame();
}
