using NPlug;
using NPlug.Validator;
using VoxTail.TypeB.Plugin;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class PluginValidationTests
{
    [Fact]
    public void Model_ExposesNamedTailCharactersAndFactoryPrograms()
    {
        var model = new Model();
        model.Initialize();

        var tailCharacter = Assert.Single(Enumerable.Range(0, model.ParameterCount)
            .Select(model.GetParameterByIndex)
            .Where(parameter => parameter.Title == "Tail Character")
            .OfType<AudioStringListParameter>());
        Assert.Equal(["Crackle", "Breaks", "Glitch"], tailCharacter.Items);
        var tailDrop = Assert.Single(
            Enumerable.Range(0, model.ParameterCount).Select(model.GetParameterByIndex),
            parameter => parameter.Title == "Tail Drop");
        Assert.Equal(0d, tailDrop.NormalizedValue);
        Assert.Contains(Enumerable.Range(0, model.ParameterCount)
            .Select(model.GetParameterByIndex)
            .Select(parameter => parameter.Title),
            title => title == "Dynamic Boost");
        Assert.Contains(Enumerable.Range(0, model.ParameterCount)
            .Select(model.GetParameterByIndex)
            .Select(parameter => parameter.Title),
            title => title == "Voice Threshold");
        Assert.Contains(Enumerable.Range(0, model.ParameterCount)
            .Select(model.GetParameterByIndex)
            .Select(parameter => parameter.Title),
            title => title == "Target Level");
        Assert.Contains(Enumerable.Range(0, model.ParameterCount)
            .Select(model.GetParameterByIndex)
            .Select(parameter => parameter.Title),
            title => title == "Maximum Boost");

        Assert.Equal(1, model.ProgramListCount);
        var programs = model.GetProgramListByIndex(0);
        Assert.Equal(
            ["Clean", "Bite", "Shout", "Cracked Tail", "Broken Tail", "Glitch Tail"],
            programs.Select(program => program.Name));
    }

    [Fact]
    public void PluginFactoryAndPublishedBinary_PassVst3ValidatorWhenBinaryExists()
    {
        var factoryResult = AudioPluginValidator.Validate(
            PluginEntry.GetFactory().Export,
            Console.Out,
            Console.Error);

        Assert.True(factoryResult);

        var pluginPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "artifacts", "VoxTail-Type-B", "win-x64", "VoxTail Type-B.vst3"));

        if (File.Exists(pluginPath))
        {
            using var output = new StringWriter();
            using var errors = new StringWriter();
            var nativeResult = AudioPluginValidator.Validate(pluginPath, output, errors);
            Assert.True(nativeResult, $"{output}{Environment.NewLine}{errors}");
        }
        else
        {
            Console.WriteLine($"Skipping native binary validation because the published plugin was not found: {pluginPath}");
        }
    }
}
