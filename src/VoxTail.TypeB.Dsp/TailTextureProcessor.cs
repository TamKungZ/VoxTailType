namespace VoxTail.TypeB.Dsp;

internal sealed class TailTextureProcessor
{
    private const float MinimumEnvelope = 0.0001f;
    private readonly float _sampleRate;
    private readonly float _attackCoefficient;
    private readonly float _smoothingCoefficient;
    private readonly uint _randomSeed;
    private float _envelopeReleaseCoefficient;
    private float _targetEnvelopeReleaseCoefficient;
    private float _peakReleaseCoefficient;
    private float _targetPeakReleaseCoefficient;
    private float _envelope;
    private float _peak;
    private float _tailEventCountdown;
    private float _tailEventDuration;
    private float _heldSample;
    private float _lastWetSample;
    private uint _randomState;

    public TailTextureProcessor(float sampleRate, uint randomSeed)
    {
        _sampleRate = sampleRate;
        _attackCoefficient = Coefficient(sampleRate, 0.003f);
        _smoothingCoefficient = Coefficient(sampleRate, 0.01f);
        _randomSeed = randomSeed == 0 ? 1u : randomSeed;
        _randomState = _randomSeed;
        _targetEnvelopeReleaseCoefficient = EnvelopeReleaseCoefficient(250f);
        _targetPeakReleaseCoefficient = PeakReleaseCoefficient(250f);
        _envelopeReleaseCoefficient = _targetEnvelopeReleaseCoefficient;
        _peakReleaseCoefficient = _targetPeakReleaseCoefficient;
    }

    public void SetTarget(float tailTimeMilliseconds)
    {
        _targetEnvelopeReleaseCoefficient = EnvelopeReleaseCoefficient(tailTimeMilliseconds);
        _targetPeakReleaseCoefficient = PeakReleaseCoefficient(tailTimeMilliseconds);
    }

    public float UpdateEnvelope(float magnitude)
    {
        _envelopeReleaseCoefficient = Smooth(_envelopeReleaseCoefficient, _targetEnvelopeReleaseCoefficient);
        _peakReleaseCoefficient = Smooth(_peakReleaseCoefficient, _targetPeakReleaseCoefficient);
        _envelope += (magnitude - _envelope) *
            (magnitude > _envelope ? _attackCoefficient : _envelopeReleaseCoefficient);
        _peak += (magnitude - _peak) *
            (magnitude > _peak ? _attackCoefficient : _peakReleaseCoefficient);

        var tailActivity = _peak > MinimumEnvelope
            ? Math.Clamp((0.75f - (_envelope / _peak)) / 0.55f, 0f, 1f)
            : 0f;
        return tailActivity;
    }

    public float ProcessSample(float sample, float tailActivity, float tailAmount, float tailDrop, float tailTimeMilliseconds, TailCharacter character)
    {
        var textureWeight = tailActivity * tailAmount;
        var processed = ApplyCharacter(sample, textureWeight, tailTimeMilliseconds, character);
        return processed * (1f - (tailActivity * tailDrop));
    }

    private float ApplyCharacter(float sample, float tailWeight, float tailTimeMilliseconds, TailCharacter character)
    {
        if (tailWeight <= 0.001f)
        {
            _tailEventCountdown = 0f;
            _tailEventDuration = 0f;
            _lastWetSample = sample;
            return sample;
        }

        if (character == TailCharacter.Crackle)
        {
            if (_tailEventCountdown <= 0f)
            {
                _tailEventCountdown = _sampleRate * (0.002f + (NextRandomUnit() * 0.03f));
                if (NextRandomUnit() < tailWeight * 0.75f)
                {
                    sample += (NextRandomUnit() - 0.5f) * 0.36f * tailWeight;
                }
            }
            else
            {
                _tailEventCountdown--;
            }

            _lastWetSample = sample;
            return sample;
        }

        if (_tailEventCountdown > 0f)
        {
            _tailEventCountdown--;
        }

        if (_tailEventDuration > 0f)
        {
            _tailEventDuration--;
            if (character == TailCharacter.Breaks)
            {
                _lastWetSample = sample;
                return sample * (0.08f + (0.92f * Math.Clamp(_tailEventDuration / (_sampleRate * 0.08f), 0f, 1f)));
            }

            return _heldSample;
        }

        if (_tailEventCountdown <= 0f)
        {
            var timeScale = Math.Clamp(tailTimeMilliseconds / 250f, 0.2f, 4f);
            if (character == TailCharacter.Breaks)
            {
                _tailEventCountdown = _sampleRate * timeScale * (0.08f + (NextRandomUnit() * 0.24f));
                _tailEventDuration = tailWeight > 0.25f && NextRandomUnit() < tailWeight * 0.7f
                    ? _sampleRate * (0.004f + (NextRandomUnit() * 0.06f)) * tailWeight
                    : 0f;
            }
            else
            {
                _tailEventCountdown = _sampleRate * timeScale * (0.035f + (NextRandomUnit() * 0.09f));
                _tailEventDuration = _sampleRate * (0.008f + (NextRandomUnit() * 0.045f)) * tailWeight;
                _heldSample = _lastWetSample;
            }
        }

        _lastWetSample = sample;
        return sample;
    }

    private float EnvelopeReleaseCoefficient(float tailTimeMilliseconds)
    {
        var seconds = Math.Clamp(tailTimeMilliseconds / 1000f * 0.2f, 0.01f, 0.15f);
        return Coefficient(_sampleRate, seconds);
    }

    private float PeakReleaseCoefficient(float tailTimeMilliseconds)
    {
        var seconds = Math.Clamp(tailTimeMilliseconds / 1000f, 0.05f, 1.5f);
        return Coefficient(_sampleRate, seconds);
    }

    private float NextRandomUnit()
    {
        var value = _randomState;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _randomState = value == 0 ? _randomSeed : value;
        return (_randomState >> 8) * (1f / 16777216f);
    }

    private float Smooth(float current, float target) => current + ((target - current) * _smoothingCoefficient);

    private static float Coefficient(float sampleRate, float seconds) =>
        1f - MathF.Exp(-1f / (sampleRate * seconds));
}
