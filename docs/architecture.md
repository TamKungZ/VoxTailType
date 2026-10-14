# Architecture

The solution has two production layers and one test project:

- `VoxTail.TypeB.Dsp` contains host-independent processing code, parameter smoothing, saturation, tone shaping, envelope tracking, and the three tail characters; it has no VST3 dependency.
- `VoxTail.TypeB.Plugin` owns NPlug registration, stable class IDs, host-automatable parameters, factory programs, audio buses, and the real-time callback. It calls one pre-created DSP state per channel with the host's existing spans, so processing does not allocate audio buffers per block.
- `VoxTail.TypeB.Dsp.Tests` uses synthetic signals to test transparency, drive, tails, non-finite input, output bounds, common sample rates, and allocation behavior without requiring a DAW.

The processor is a zero-latency float32 stereo effect. Its clean defaults preserve finite input samples exactly; the bypass parameter uses NPlug's host bypass behavior. Drive and intensity add normalized tanh saturation, Tone applies a one-pole high-frequency tilt on the processed path, and Mix blends the processed path with the original. Tail detection compares a fast envelope follower with a slower peak follower. Tail Amount controls texture intensity, Tail Drop attenuates the processed signal as the detected tail fades, and Tail Time controls the envelope release; Tail Character selects sparse crackle transients, brief dropouts, or short sample repeats. Continuous parameter changes are smoothed over 10 ms, and filter/envelope coefficients are updated once per audio block rather than recalculated for every sample.

Mono bus layouts are not enabled yet. Non-finite input samples are replaced with silence in the active DSP path; clean defaults and host bypass preserve finite audio without alteration. Processed output uses a soft ceiling, while the default dry path remains transparent.

Factory programs provide Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail starting points. Their parameters remain editable, and the DAW can save the edited state as a user preset.

## Stable identifiers

| Class | ID |
|---|---|
| Processor | `1b3718ed-03a5-4a00-8879-cc3245cc8938` |
| Controller | `7f573153-82aa-4292-a701-357499ce2ad8` |

These GUIDs identify the Type-B processor and controller; keep them stable.
