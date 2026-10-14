# DSP roadmap

1. Add unit-tested parameter smoothing, envelope tracking, filtering, and saturation primitives.
2. Expose only working Drive, Tone, Mix, and Output Gain controls; verify neutral gain, increasing harmonics, stable automation, and bounded output.
3. Evaluate manual Crack Tail controls before considering envelope-driven tail detection.
4. Validate mono/stereo layouts, sample rates, variable block sizes, non-finite input handling, and real-time allocation behavior as DSP features are introduced.
5. Add character modes or a custom UI only after the underlying controls and host lifecycle are stable.

The effect is a creative vocal processor; DSP can add harmonics and coloration but cannot guarantee that a quiet source will sound identical to a human shout.
