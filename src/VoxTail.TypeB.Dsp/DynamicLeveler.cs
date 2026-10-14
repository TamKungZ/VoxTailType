namespace VoxTail.TypeB.Dsp;

internal sealed class DynamicLeveler
{
    private readonly float _sampleRate;
    private readonly float _activityAttackCoefficient;
    private readonly float _activityReleaseCoefficient;
    private readonly float _rmsAttackCoefficient;
    private readonly float _rmsReleaseCoefficient;
    private readonly float _boostAttackCoefficient;
    private readonly float _boostReleaseCoefficient;
    private float _voiceThreshold;
    private float _targetLevel;
    private float _maximumBoost;
    private float _activityEnvelope;
    private float _rmsPower;
    private float _gain = 1f;
    private float _belowThresholdSamples;
    private bool _voiceActive;

    public DynamicLeveler(float sampleRate)
    {
        _sampleRate = sampleRate;
        _activityAttackCoefficient = Coefficient(sampleRate, 0.002f);
        _activityReleaseCoefficient = Coefficient(sampleRate, 0.08f);
        _rmsAttackCoefficient = Coefficient(sampleRate, 0.015f);
        _rmsReleaseCoefficient = Coefficient(sampleRate, 0.12f);
        _boostAttackCoefficient = Coefficient(sampleRate, 0.04f);
        _boostReleaseCoefficient = Coefficient(sampleRate, 0.05f);
    }

    public void SetTargets(float voiceThresholdDb, float targetLevelDb, float maximumBoostDb)
    {
        _voiceThreshold = DecibelsToGain(voiceThresholdDb);
        _targetLevel = DecibelsToGain(targetLevelDb);
        _maximumBoost = DecibelsToGain(maximumBoostDb);
    }

    public float ProcessSample(float sample, float amount)
    {
        var magnitude = MathF.Abs(sample);
        _activityEnvelope += (magnitude - _activityEnvelope) *
            (magnitude > _activityEnvelope ? _activityAttackCoefficient : _activityReleaseCoefficient);

        if (_activityEnvelope >= _voiceThreshold)
        {
            _voiceActive = true;
            _belowThresholdSamples = 0f;
        }
        else if (_voiceActive && _activityEnvelope < _voiceThreshold * 0.5f)
        {
            _belowThresholdSamples++;
            if (_belowThresholdSamples >= _sampleRate * 0.08f)
            {
                _voiceActive = false;
                _belowThresholdSamples = 0f;
            }
        }
        else
        {
            _belowThresholdSamples = 0f;
        }

        var power = sample * sample;
        _rmsPower += (power - _rmsPower) *
            (power > _rmsPower ? _rmsAttackCoefficient : _rmsReleaseCoefficient);
        var targetGain = 1f;

        if (_voiceActive && amount > 0f)
        {
            var rms = MathF.Sqrt(MathF.Max(_rmsPower, 0f));
            var requestedGain = _targetLevel / MathF.Max(rms, _voiceThreshold);
            targetGain = 1f + ((Math.Clamp(requestedGain, 1f, _maximumBoost) - 1f) * amount);
        }

        _gain += (targetGain - _gain) *
            (targetGain > _gain ? _boostAttackCoefficient : _boostReleaseCoefficient);
        return _gain;
    }

    private static float Coefficient(float sampleRate, float seconds) =>
        1f - MathF.Exp(-1f / (sampleRate * seconds));

    private static float DecibelsToGain(float decibels) => MathF.Pow(10f, decibels / 20f);
}
