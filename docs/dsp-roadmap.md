# DSP roadmap

## Implemented

- Host-automatable Input Gain, Intensity, Drive, Tone, Clean / Wet Mix, Tail Amount, Tail Drop, Tail Time, Tail Character, Dynamic Boost, Voice Threshold, Target Level, Maximum Boost, Output Gain, and Bypass.
- Smoothed continuous controls, a saturation/tone path, envelope-based tail triggering, and Crackle, Breaks, and Glitch tail treatments.
- A zero-latency per-channel loudness envelope and gated Dynamic Boost with adjustable activity threshold, target level, and maximum boost.
- A Windows custom VST3 editor with host-automatable controls, a live input/output RMS history graph, and Threshold/Target guide lines in the requested white, blush-pink, and navy palette.
- Factory programs: Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail. The DAW can save modified settings as user presets.
- Synthetic tests for neutral transparency, drive coloration, all tail modes, tail-drop attenuation, dynamic boosting and low-level gating, non-finite input and parameters, output bounds, common sample rates, variable block sizes, and allocations.

## Remaining validation

- Test automation and preset recall in target DAWs; the test suite does not replace listening tests in a host.
- Add and validate mono bus layouts.
- Link stereo loudness detection to prevent gain differences from shifting the stereo image.
- Refine tail detection and sound using controlled listening tests; tail triggering is currently an envelope heuristic, not vocal-semantic detection.
- Validate parameter ramps for clicks and test variable block sizes and long-term state continuity with the actual plugin host.
- Validate custom editor attachment, parameter gestures/automation, meter polling, and multiple plugin instances in supported DAWs. The editor currently targets the Windows HWND view platform.

The effect is a creative vocal processor; DSP can add harmonics and coloration but cannot guarantee that a quiet source will sound identical to a human shout.
