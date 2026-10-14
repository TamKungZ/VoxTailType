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
    public static VoxTailParameters Default => new(0f, 0f, 0f, 0.5f, 0f, 0f, 0f, 250f, TailCharacter.Crackle, 0f, 0f, -54f, -24f, 9f);
}

public sealed class VoxTailProcessor
{
    private const float MinimumEnvelope = 0.0001f;
    private const float MinimumFilterState = 1e-20f;
    private const float MaximumInternalSample = 1_000_000f;
    private readonly float _sampleRate;
    private readonly float _smoothingCoefficient;
    private readonly float _attackCoefficient;
    private readonly float _activityAttackCoefficient;
    private readonly float _activityReleaseCoefficient;
    private readonly float _rmsAttackCoefficient;
    private readonly float _rmsReleaseCoefficient;
    private readonly float _boostAttackCoefficient;
    private readonly float _boostReleaseCoefficient;
    private readonly uint _randomSeed;
    private VoxTailParameters _target = VoxTailParameters.Default;
    private float _inputGain = 1f;
    private float _intensity;
    private float _drive;
    private float _tone = 0.5f;
    private float _mix;
    private float _tailAmount;
    private float _tailDrop;
    private float _tailTimeMilliseconds = 250f;
    private float _outputGain = 1f;
    private float _dynamicBoost;
    private float _toneCoefficient;
    private float _envelopeReleaseCoefficient;
    private float _peakReleaseCoefficient;
    private float _voiceThreshold;
    private float _targetLevel;
    private float _maximumBoost;
    private float _toneLow;
    private float _envelope;
    private float _peak;
    private float _activityEnvelope;
    private float _rmsPower;
    private float _dynamicGain = 1f;
    private float _belowThresholdSamples;
    private bool _voiceActive;
    private float _tailEventCountdown;
    private float _tailEventDuration;
    private float _heldSample;
    private float _lastWetSample;
    private uint _randomState;

    public VoxTailProcessor(float sampleRate, uint randomSeed = 0xA341316Cu)
    {
        if (!float.IsFinite(sampleRate) || sampleRate <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        _sampleRate = sampleRate;
        _smoothingCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.01f));
        _attackCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.003f));
        _activityAttackCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.002f));
        _activityReleaseCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.08f));
        _rmsAttackCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.015f));
        _rmsReleaseCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.12f));
        _boostAttackCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.04f));
        _boostReleaseCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.2f));
        _randomSeed = randomSeed == 0 ? 1u : randomSeed;
        _randomState = _randomSeed;
        UpdateProcessingCoefficients();
    }

    public void SetParameters(VoxTailParameters parameters)
    {
        _target = parameters with
        {
            InputGainDb = ClampFinite(parameters.InputGainDb, -12f, 12f),
            Intensity = ClampFinite(parameters.Intensity, 0f, 1f),
            Drive = ClampFinite(parameters.Drive, 0f, 1f),
            Tone = ClampFinite(parameters.Tone, 0f, 1f),
            Mix = ClampFinite(parameters.Mix, 0f, 1f),
            TailAmount = ClampFinite(parameters.TailAmount, 0f, 1f),
            TailDrop = ClampFinite(parameters.TailDrop, 0f, 1f),
            TailTimeMilliseconds = ClampFinite(parameters.TailTimeMilliseconds, 50f, 1500f),
            TailCharacter = Enum.IsDefined(parameters.TailCharacter) ? parameters.TailCharacter : TailCharacter.Crackle,
            OutputGainDb = ClampFinite(parameters.OutputGainDb, -12f, 12f),
            DynamicBoost = ClampFinite(parameters.DynamicBoost, 0f, 1f),
            VoiceThresholdDb = ClampFinite(parameters.VoiceThresholdDb, -72f, -24f),
            TargetLevelDb = ClampFinite(parameters.TargetLevelDb, -36f, -12f),
            MaximumBoostDb = ClampFinite(parameters.MaximumBoostDb, 0f, 18f)
        };
    }

    public void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != output.Length)
        {
            throw new ArgumentException("Input and output buffers must have the same length.", nameof(output));
        }

        UpdateProcessingCoefficients();
        UpdateDynamicLevelerTargets();

        for (var index = 0; index < input.Length; index++)
        {
            _inputGain = Smooth(_inputGain, DecibelsToGain(_target.InputGainDb));
            _intensity = Smooth(_intensity, _target.Intensity);
            _drive = Smooth(_drive, _target.Drive);
            _tone = Smooth(_tone, _target.Tone);
            _mix = Smooth(_mix, _target.Mix);
            _tailAmount = Smooth(_tailAmount, _target.TailAmount);
            _tailDrop = Smooth(_tailDrop, _target.TailDrop);
            _tailTimeMilliseconds = Smooth(_tailTimeMilliseconds, _target.TailTimeMilliseconds);
            _outputGain = Smooth(_outputGain, DecibelsToGain(_target.OutputGainDb));
            _dynamicBoost = Smooth(_dynamicBoost, _target.DynamicBoost);

            var dry = float.IsFinite(input[index]) ? input[index] : 0f;
            var detectorInput = Math.Clamp(dry, -MaximumInternalSample, MaximumInternalSample);
            var levelled = Math.Clamp(dry * UpdateDynamicGain(detectorInput), -MaximumInternalSample, MaximumInternalSample);
            var gained = Math.Clamp(levelled * _inputGain, -MaximumInternalSample, MaximumInternalSample);
            var magnitude = MathF.Abs(gained);
            UpdateEnvelope(magnitude);

            var rawTailActivity = _peak > MinimumEnvelope
                ? Math.Clamp((0.75f - (_envelope / _peak)) / 0.55f, 0f, 1f)
                : 0f;
            var textureWeight = rawTailActivity * _tailAmount;
            var drive = 1f + (_drive * 18f) + (_intensity * 7f) + (textureWeight * 10f);
            var normalizedSaturation = MathF.Tanh(drive);
            var saturated = drive > 1f
                ? MathF.Tanh(gained * drive) / normalizedSaturation
                : gained;

            _toneLow += _toneCoefficient * (saturated - _toneLow);
            if (MathF.Abs(_toneLow) < MinimumFilterState)
            {
                _toneLow = 0f;
            }

            var highGain = 0.4f + (1.2f * _tone);
            var wet = _toneLow + ((saturated - _toneLow) * highGain);
            wet = ApplyTailCharacter(wet, textureWeight);
            wet *= 1f - (rawTailActivity * _tailDrop);

            var mixed = (levelled * (1f - _mix)) + (wet * _mix);
            var processed = mixed * _outputGain;
            output[index] = _mix <= 0f && _outputGain == 1f && _dynamicBoost <= 0f
                ? dry
                : SoftLimit(processed);
        }
    }

    private float Smooth(float current, float target) =>
        current + ((target - current) * _smoothingCoefficient);

    private void UpdateEnvelope(float magnitude)
    {
        _envelope += (magnitude - _envelope) * (magnitude > _envelope ? _attackCoefficient : _envelopeReleaseCoefficient);
        _peak += (magnitude - _peak) * (magnitude > _peak ? _attackCoefficient : _peakReleaseCoefficient);
    }

    private float UpdateDynamicGain(float sample)
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

        var square = sample * sample;
        _rmsPower += (square - _rmsPower) *
            (square > _rmsPower ? _rmsAttackCoefficient : _rmsReleaseCoefficient);
        var targetGain = 1f;

        if (_voiceActive && _dynamicBoost > 0f)
        {
            var rms = MathF.Sqrt(MathF.Max(_rmsPower, 0f));
            var requestedGain = _targetLevel / MathF.Max(rms, _voiceThreshold);
            targetGain = 1f + ((Math.Clamp(requestedGain, 1f, _maximumBoost) - 1f) * _dynamicBoost);
        }

        _dynamicGain += (targetGain - _dynamicGain) *
            (targetGain > _dynamicGain ? _boostAttackCoefficient : _boostReleaseCoefficient);
        return _dynamicGain;
    }

    private float ApplyTailCharacter(float sample, float tailWeight)
    {
        if (tailWeight <= 0.001f)
        {
            _tailEventCountdown = 0f;
            _tailEventDuration = 0f;
            _lastWetSample = sample;
            return sample;
        }

        if (_target.TailCharacter == TailCharacter.Crackle)
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
            if (_target.TailCharacter == TailCharacter.Breaks)
            {
                _lastWetSample = sample;
                return sample * (0.08f + (0.92f * Math.Clamp(_tailEventDuration / (_sampleRate * 0.08f), 0f, 1f)));
            }

            return _heldSample;
        }

        if (_tailEventCountdown <= 0f)
        {
            var timeScale = Math.Clamp(_tailTimeMilliseconds / 250f, 0.2f, 4f);
            if (_target.TailCharacter == TailCharacter.Breaks)
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

    private float ToneCoefficient(float tone)
    {
        var cutoff = 700f * MathF.Pow(18f, tone);
        return 1f - MathF.Exp(-2f * MathF.PI * cutoff / _sampleRate);
    }

    private void UpdateProcessingCoefficients()
    {
        _toneCoefficient = ToneCoefficient(_tone);
        var envelopeSeconds = Math.Clamp(_tailTimeMilliseconds / 1000f * 0.2f, 0.01f, 0.15f);
        var peakSeconds = Math.Clamp(_tailTimeMilliseconds / 1000f, 0.05f, 1.5f);
        _envelopeReleaseCoefficient = 1f - MathF.Exp(-1f / (_sampleRate * envelopeSeconds));
        _peakReleaseCoefficient = 1f - MathF.Exp(-1f / (_sampleRate * peakSeconds));
    }

    private void UpdateDynamicLevelerTargets()
    {
        _voiceThreshold = DecibelsToGain(_target.VoiceThresholdDb);
        _targetLevel = DecibelsToGain(_target.TargetLevelDb);
        _maximumBoost = DecibelsToGain(_target.MaximumBoostDb);
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

    private static float DecibelsToGain(float decibels) => MathF.Pow(10f, decibels / 20f);

    private static float ClampFinite(float value, float minimum, float maximum) =>
        float.IsFinite(value) ? Math.Clamp(value, minimum, maximum) : minimum;

    private static float SoftLimit(float sample)
    {
        var magnitude = MathF.Abs(sample);
        if (magnitude <= 0.9f)
        {
            return sample;
        }

        return MathF.CopySign(0.9f + (0.1f * MathF.Tanh((magnitude - 0.9f) * 10f)), sample);
    }
}
