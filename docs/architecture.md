# Architecture

The solution has two production layers and one test project:

- `VoxTail.TypeB.Dsp` contains host-independent DSP. `Processor.cs` coordinates the signal chain; `DynamicLeveler.cs` owns Dynamic Boost detection and gain state; `ToneFilter.cs` handles tone shaping; `TailTextureProcessor.cs` owns tail tracking and tail characters; `Parameters.cs` defines the shared parameter model.
- `VoxTail.TypeB.Plugin` owns NPlug integration. `Processor.cs` handles bus setup and the audio callback; `Model.cs` defines automatable parameters; `Presets.cs` defines factory programs; `ParameterIds.cs` centralizes stable parameter IDs; `Controller.cs` and `PluginEntry.cs` handle controller and factory registration. Each audio channel gets pre-created DSP state and uses the host's spans, so audio buffers are not allocated per block.
- `VoxTail.TypeB.Dsp.Tests` keeps feature-focused checks in `DynamicBoostTests.cs` and general DSP behavior in `ProcessorTests.cs`, using synthetic signals without requiring a DAW.

The processor is a zero-latency float32 stereo effect. Its clean defaults preserve finite input samples exactly; the bypass parameter uses NPlug's host bypass behavior. Drive and intensity add normalized tanh saturation, Tone applies a one-pole high-frequency tilt on the processed path, and Mix blends the processed path with the original. Tail detection compares a fast envelope follower with a slower peak follower. Tail Amount controls texture intensity, Tail Drop attenuates the processed signal as the detected tail fades, and Tail Time controls the envelope release; Tail Character selects sparse crackle transients, brief dropouts, or short sample repeats. Dynamic Boost adds a continuous per-channel leveler that estimates activity and RMS loudness, lifts only active material toward a target, and caps gain at Maximum Boost. Voice Threshold keeps silence and quiet background noise from opening the leveler. Gain reduction uses a 50 ms release to avoid carrying excess boost into louder material. It adds no lookahead latency; it estimates activity rather than identifying exact phrase boundaries. The internal loudness envelope can drive this control, but no visual graph is currently rendered. Parameter and coefficient changes are smoothed sample by sample; target coefficients are calculated once per block so processing remains independent of block boundaries.

Mono bus layouts are not enabled yet. Non-finite input samples are replaced with silence in the active DSP path; clean defaults and host bypass preserve finite audio without alteration. Processed output uses a soft ceiling, while the default dry path remains transparent.

Factory programs provide Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail starting points. Their parameters remain editable, and the DAW can save the edited state as a user preset.

## Stable identifiers

| Class | ID |
|---|---|
| Processor | `1b3718ed-03a5-4a00-8879-cc3245cc8938` |
| Controller | `7f573153-82aa-4292-a701-357499ce2ad8` |

These GUIDs identify the Type-B processor and controller; keep them stable.
