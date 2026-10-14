using NPlug;

namespace VoxTail.TypeB.Plugin;

public sealed class VoxTailTypeBModel : AudioProcessorModel
{
    public VoxTailTypeBModel() : base("VoxTail.TypeB")
    {
        AddByPassParameter();
    }
}
