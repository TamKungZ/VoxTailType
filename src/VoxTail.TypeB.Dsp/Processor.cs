namespace VoxTail.TypeB.Dsp;

public sealed class VoxTailProcessor
{
    private const float MaximumInternalSample = 1_000_000f;
    private readonly float _smoothingCoefficient;
    private readonly DynamicLeveler _leveler;
    private readonly ToneFilter _toneFilter;
    private readonly TailTextureProcessor _tailTexture;
    private VoxTailParameters _target = VoxTailParameters.Default;
    private float _targetInputGain = 1f;
    private float _targetOutputGain = 1f;
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

    public float MeterInputRms { get; private set; }
    public float MeterOutputRms { get; private set; }
    public float MeterBoostDb { get; private set; }
    public bool MeterVoiceActive { get; private set; }

    public VoxTailProcessor(float sampleRate, uint randomSeed = 0xA341316Cu)
    {
        if (!float.IsFinite(sampleRate) || sampleRate <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        _smoothingCoefficient = 1f - MathF.Exp(-1f / (sampleRate * 0.01f));
        _leveler = new DynamicLeveler(sampleRate);
        _toneFilter = new ToneFilter(sampleRate);
        _tailTexture = new TailTextureProcessor(sampleRate, randomSeed);
        _toneFilter.SetTarget(_tone);
        _tailTexture.SetTarget(_tailTimeMilliseconds);
        _leveler.SetTargets(_target.VoiceThresholdDb, _target.TargetLevelDb, _target.MaximumBoostDb);
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
        _targetInputGain = DecibelsToGain(_target.InputGainDb);
        _targetOutputGain = DecibelsToGain(_target.OutputGainDb);
    }

    public void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != output.Length)
        {
            throw new ArgumentException("Input and output buffers must have the same length.", nameof(output));
        }

        _toneFilter.SetTarget(_target.Tone);
        _tailTexture.SetTarget(_target.TailTimeMilliseconds);
        _leveler.SetTargets(_target.VoiceThresholdDb, _target.TargetLevelDb, _target.MaximumBoostDb);
        var inputPower = 0f;
        var outputPower = 0f;

        for (var index = 0; index < input.Length; index++)
        {
            _inputGain = Smooth(_inputGain, _targetInputGain);
            _intensity = Smooth(_intensity, _target.Intensity);
            _drive = Smooth(_drive, _target.Drive);
            _tone = Smooth(_tone, _target.Tone);
            _mix = Smooth(_mix, _target.Mix);
            _tailAmount = Smooth(_tailAmount, _target.TailAmount);
            _tailDrop = Smooth(_tailDrop, _target.TailDrop);
            _tailTimeMilliseconds = Smooth(_tailTimeMilliseconds, _target.TailTimeMilliseconds);
            _outputGain = Smooth(_outputGain, _targetOutputGain);
            _dynamicBoost = Smooth(_dynamicBoost, _target.DynamicBoost);

            var dry = float.IsFinite(input[index]) ? input[index] : 0f;
            var detectorInput = Math.Clamp(dry, -MaximumInternalSample, MaximumInternalSample);
            var levelled = Math.Clamp(dry * _leveler.ProcessSample(detectorInput, _dynamicBoost), -MaximumInternalSample, MaximumInternalSample);
            var gained = Math.Clamp(levelled * _inputGain, -MaximumInternalSample, MaximumInternalSample);
            var magnitude = MathF.Abs(gained);
            var tailActivity = _tailTexture.UpdateEnvelope(magnitude);
            var textureWeight = tailActivity * _tailAmount;
            var drive = 1f + (_drive * 18f) + (_intensity * 7f) + (textureWeight * 10f);
            var saturated = drive > 1f
                ? MathF.Tanh(gained * drive) / MathF.Tanh(drive)
                : gained;
            var toned = _toneFilter.ProcessSample(saturated, _tone);
            var wet = _tailTexture.ProcessSample(
                toned,
                tailActivity,
                _tailAmount,
                _tailDrop,
                _tailTimeMilliseconds,
                _target.TailCharacter);
            var mixed = (levelled * (1f - _mix)) + (wet * _mix);
            var processed = mixed * _outputGain;
            output[index] = _mix <= 0f && _outputGain == 1f && _dynamicBoost <= 0f
                ? dry
                : SoftLimit(processed);
            var meterInput = Math.Clamp(detectorInput, -1f, 1f);
            var meterOutput = Math.Clamp(output[index], -1f, 1f);
            inputPower += meterInput * meterInput;
            outputPower += meterOutput * meterOutput;
        }

        MeterInputRms = input.Length == 0 ? 0f : MathF.Sqrt(inputPower / input.Length);
        MeterOutputRms = input.Length == 0 ? 0f : MathF.Sqrt(outputPower / input.Length);
        MeterBoostDb = 20f * MathF.Log10(MathF.Max(_leveler.CurrentGain, 1e-6f));
        MeterVoiceActive = _leveler.IsVoiceActive;
    }

    private float Smooth(float current, float target) =>
        current + ((target - current) * _smoothingCoefficient);

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
