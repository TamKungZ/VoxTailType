namespace VoxTail.TypeB.Dsp;

internal sealed class ToneFilter
{
    private const float MinimumFilterState = 1e-20f;
    private readonly float _sampleRate;
    private readonly float _smoothingCoefficient;
    private float _coefficient;
    private float _targetCoefficient;
    private float _low;

    public ToneFilter(float sampleRate)
    {
        _sampleRate = sampleRate;
        _smoothingCoefficient = Coefficient(sampleRate, 0.01f);
        _coefficient = CalculateCoefficient(0.5f);
        _targetCoefficient = _coefficient;
    }

    public void SetTarget(float tone) => _targetCoefficient = CalculateCoefficient(tone);

    public float ProcessSample(float sample, float tone)
    {
        _coefficient += (_targetCoefficient - _coefficient) * _smoothingCoefficient;
        _low += _coefficient * (sample - _low);
        if (MathF.Abs(_low) < MinimumFilterState)
        {
            _low = 0f;
        }

        var highGain = 0.4f + (1.2f * tone);
        return _low + ((sample - _low) * highGain);
    }

    private float CalculateCoefficient(float tone)
    {
        var cutoff = 700f * MathF.Pow(18f, tone);
        return 1f - MathF.Exp(-2f * MathF.PI * cutoff / _sampleRate);
    }

    private static float Coefficient(float sampleRate, float seconds) =>
        1f - MathF.Exp(-1f / (sampleRate * seconds));
}
