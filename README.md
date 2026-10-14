# VoxTail Type-B

VoxTail Type-B is a Windows x64 VST3 vocal effect for adding controlled saturation, tone shaping, and stylized cracked vocal tails. The host-independent DSP core is kept separate from the NPlug lifecycle and processes existing audio buffers without per-block allocations.

The plugin exposes Input Gain, Intensity, Drive, Tone, Clean / Wet Mix, Tail Amount, Tail Time, Tail Character, Output Gain, and Bypass. Tail Character selects **Crackle**, **Breaks**, or **Glitch**. Factory programs include Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail; programs can be adjusted after loading, and DAW preset/state features can save custom settings.

This is a creative effect, not voice conversion: DSP can add harmonics and stylized texture, but cannot guarantee that a quiet recording will sound identical to a real human shout.

## Build and test

Requires the .NET 10 SDK and the Visual C++ build tools needed by NativeAOT.

```powershell
dotnet test VoxTail-Type-B.sln
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

If NativeAOT cannot discover the installed Visual Studio linker, run the publish command from a Visual Studio Developer Command Prompt and add `-p:IlcUseEnvironmentalTools=true`.

The native plug-in bundle is written to `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3`. Copy that bundle directory to a VST3 scan directory or add the output directory to the DAW's plug-in paths, then rescan for **VoxTail Type-B**. NPlug's validator is included as a dependency; its execution status is documented in `docs/development.md`.

See [development setup](docs/development.md), [architecture](docs/architecture.md), and [DSP roadmap](docs/dsp-roadmap.md) for environment notes, stable plug-in IDs, validation steps, and planned work.

## License

VoxTail Type-B is open source under [GNU GPL version 3](LICENSE). Third-party components remain under their respective licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).
