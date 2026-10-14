# DSP roadmap

## Implemented

- Host-automatable Input Gain, Intensity, Drive, Tone, Clean / Wet Mix, Tail Amount, Tail Time, Tail Character, Output Gain, and Bypass.
- Smoothed continuous controls, a saturation/tone path, envelope-based tail triggering, and Crackle, Breaks, and Glitch tail treatments.
- Factory programs: Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail. The DAW can save modified settings as user presets.
- Synthetic tests for neutral transparency, drive coloration, all tail modes, non-finite values, output bounds, common sample rates, and allocations.

## Remaining validation

- Test automation and preset recall in target DAWs; the test suite does not replace listening tests in a host.
- Add and validate mono bus layouts.
- Refine tail detection and sound using controlled listening tests; tail triggering is currently an envelope heuristic, not vocal-semantic detection.
- Validate parameter ramps for clicks and test variable block sizes and long-term state continuity with the actual plugin host.
- Design a custom UI only after the host lifecycle and parameter mapping are stable.

The effect is a creative vocal processor; DSP can add harmonics and coloration but cannot guarantee that a quiet source will sound identical to a human shout.
