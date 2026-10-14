using System.Runtime.CompilerServices;
using NPlug;

namespace VoxTail.TypeB.Plugin;

public static class PluginEntry
{
    public static AudioPluginFactory GetFactory()
    {
        var factory = new AudioPluginFactory(
            new("TamKungZ_", "https://github.com/TamKungZ/VoxTailType", "dev@tamkungz.me"));
        factory.RegisterPlugin<Processor>(
            new(Processor.ClassId, "VoxTail Type-B", AudioProcessorCategory.Effect));
        factory.RegisterPlugin<Controller>(
            new(Controller.ClassId, "VoxTail Type-B Controller"));
        return factory;
    }

    [ModuleInitializer]
    internal static void ExportThisPlugin()
    {
        AudioPluginFactoryExporter.Instance = GetFactory();
    }
}
