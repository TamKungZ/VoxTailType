namespace VoxTail.TypeB.Ui;

public readonly record struct EditorMeterFrame(
    float InputRms,
    float OutputRms,
    float BoostDb,
    bool VoiceActive);
