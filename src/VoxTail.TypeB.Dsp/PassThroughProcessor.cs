namespace VoxTail.TypeB.Dsp;

public static class PassThroughProcessor
{
    public static void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        if (input.Length != output.Length)
        {
            throw new ArgumentException("Input and output buffers must have the same length.", nameof(output));
        }

        input.CopyTo(output);
    }
}
