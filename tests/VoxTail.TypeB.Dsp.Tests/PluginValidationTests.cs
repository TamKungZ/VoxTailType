using NPlug.Validator;
using VoxTail.TypeB.Plugin;

namespace VoxTail.TypeB.Dsp.Tests;

public sealed class PluginValidationTests
{
    [Fact]
    public void PluginFactoryAndPublishedBinary_PassVst3ValidatorWhenBinaryExists()
    {
        var factoryResult = AudioPluginValidator.Validate(
            VoxTailTypeBPlugin.GetFactory().Export,
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
