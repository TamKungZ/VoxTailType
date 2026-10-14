namespace VoxTail.TypeB.Dsp;

public enum TailCharacter
{
    Crackle,
    Breaks,
    Glitch
}

public readonly record struct VoxTailParameters(
    float InputGainDb,
    float Intensity,
    float Drive,
    float Tone,
    float Mix,
    float TailAmount,
    float TailDrop,
    float TailTimeMilliseconds,
    TailCharacter TailCharacter,
    float OutputGainDb,
    float DynamicBoost,
    float VoiceThresholdDb,
    float TargetLevelDb,
    float MaximumBoostDb)
{
    public static VoxTailParameters Default =>
        new(0f, 0f, 0f, 0.5f, 0f, 0f, 0f, 250f, TailCharacter.Crackle, 0f, 0f, -54f, -24f, 9f);
}
