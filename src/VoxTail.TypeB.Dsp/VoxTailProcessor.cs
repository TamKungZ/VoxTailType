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
    float TailTimeMilliseconds,
    TailCharacter TailCharacter,
    float OutputGainDb)
{
    public static VoxTailParameters Default => new(0f, 0f, 0f, 0.5f, 0f, 0f, 250f, TailCharacter.Crackle, 0f);
}

public sealed class VoxTailProcessor
{
    private const float MinimumEnvelope = 0.0001f;
    private readonly float _sampleRate;
    private readonly float _smoothingCoefficient;
    private readonly uint _randomSeed;
    private VoxTailParameters _target = VoxTailParameters.Default;
    private float _inputGain = 1f;
    private float _intensity;
    private float _drive;
    private float _tone = 0.5f;
    private float _mix;
    private float _tailAmount;
    private float _tailTimeMilliseconds = 250f;
    private float _outputGain = 1f;
    private float _toneLow;
    private float _envelope;
    private float _peak;
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
        _randomSeed = randomSeed == 0 ? 1u : randomSeed;
        _randomState = _randomSeed;
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
            TailTimeMilliseconds = ClampFinite(parameters.TailTimeMilliseconds, 50f, 1500f),
            TailCharacter = Enum.IsDefined(parameters.TailCharacter) ? parameters.TailCharacter : TailCharacter.Crackle,
            OutputGainDb = ClampFinite(parameters.OutputGainDb, -12f, 12f)
        };
    }

    public void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != output.Length)
        {
            throw new ArgumentException("Input and output buffers must have the same length.", nameof(output));
        }

        for (var index = 0; index < input.Length; index++)
        {
            _inputGain = Smooth(_inputGain, DecibelsToGain(_target.InputGainDb));
            _intensity = Smooth(_intensity, _target.Intensity);
            _drive = Smooth(_drive, _target.Drive);
            _tone = Smooth(_tone, _target.Tone);
            _mix = Smooth(_mix, _target.Mix);
            _tailAmount = Smooth(_tailAmount, _target.TailAmount);
            _tailTimeMilliseconds = Smooth(_tailTimeMilliseconds, _target.TailTimeMilliseconds);
            _outputGain = Smooth(_outputGain, DecibelsToGain(_target.OutputGainDb));

            var dry = float.IsFinite(input[index]) ? input[index] : 0f;
            var gained = dry * _inputGain;
            var magnitude = MathF.Abs(gained);
            UpdateEnvelope(magnitude);

            var tailWeight = _peak > MinimumEnvelope
                ? Math.Clamp((_peak - _envelope) / (_peak * 0.75f), 0f, 1f) * _tailAmount
                : 0f;
            var drive = 1f + (_drive * 18f) + (_intensity * 7f) + (tailWeight * 10f);
            var normalizedSaturation = MathF.Tanh(drive);
            var saturated = drive > 1f
                ? MathF.Tanh(gained * drive) / normalizedSaturation
                : gained;

            var toneCoefficient = ToneCoefficient(_tone);
            _toneLow += toneCoefficient * (saturated - _toneLow);
            var highGain = 0.4f + (1.2f * _tone);
            var wet = _toneLow + ((saturated - _toneLow) * highGain);
            wet = ApplyTailCharacter(wet, tailWeight);

            var mixed = (dry * (1f - _mix)) + (wet * _mix);
            var processed = mixed * _outputGain;
            output[index] = _mix <= 0f && _outputGain == 1f
                ? dry
                : SoftLimit(processed);
        }
    }

    private float Smooth(float current, float target) =>
        current + ((target - current) * _smoothingCoefficient);

    private void UpdateEnvelope(float magnitude)
    {
        const float attackSeconds = 0.003f;
        var attackCoefficient = 1f - MathF.Exp(-1f / (_sampleRate * attackSeconds));
        var envelopeSeconds = Math.Clamp(_tailTimeMilliseconds / 1000f * 0.2f, 0.01f, 0.15f);
        var envelopeCoefficient = 1f - MathF.Exp(-1f / (_sampleRate * envelopeSeconds));
        var peakSeconds = Math.Clamp(_tailTimeMilliseconds / 1000f, 0.05f, 1.5f);
        var peakCoefficient = 1f - MathF.Exp(-1f / (_sampleRate * peakSeconds));

        _envelope += (magnitude - _envelope) * (magnitude > _envelope ? attackCoefficient : envelopeCoefficient);
        _peak += (magnitude - _peak) * (magnitude > _peak ? attackCoefficient : peakCoefficient);
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
