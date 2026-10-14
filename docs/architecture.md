# Architecture

The solution has two production layers and one test project:

- `VoxTail.TypeB.Dsp` contains host-independent processing code and has no VST3 dependency.
- `VoxTail.TypeB.Plugin` owns NPlug registration, stable class IDs, audio buses, and the real-time callback. It calls the DSP core with the host's existing channel spans, so processing does not allocate audio buffers per block.
- `VoxTail.TypeB.Dsp.Tests` uses synthetic signals to test the DSP layer without requiring a DAW.

The initial processor is a float32 stereo effect that copies each input channel to the matching output channel without changing sample values or adding latency. Mono bus layouts are not enabled yet; NPlug's base processor only accepts the bus arrangements registered at initialization, so mono support needs explicit layout handling rather than assuming a stereo host buffer. The built-in host bypass parameter is registered. DSP controls are intentionally deferred until they have implemented behavior.

## Stable identifiers

| Class | ID |
|---|---|
| Processor | `1b3718ed-03a5-4a00-8879-cc3245cc8938` |
| Controller | `7f573153-82aa-4292-a701-357499ce2ad8` |

These GUIDs identify the Type-B processor and controller; keep them stable.
