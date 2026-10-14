# VoxTail Type-B

VoxTail Type-B is a Windows x64 VST3 vocal effect for adding controlled saturation, tone shaping, and stylized cracked vocal tails. The host-independent DSP core is kept separate from the NPlug lifecycle and processes existing audio buffers without per-block allocations.

The plugin exposes Input Gain, Intensity, Drive, Tone, Clean / Wet Mix, Tail Amount, Tail Drop, Tail Time, Tail Character, Dynamic Boost, Voice Threshold, Target Level, Maximum Boost, Output Gain, and Bypass. Tail Amount controls the tail texture; Tail Drop attenuates the processed signal as the detected tail fades. Tail Character selects **Crackle**, **Breaks**, or **Glitch**. Dynamic Boost follows a real-time loudness envelope and lifts active, quiet audio toward Target Level, bounded by Maximum Boost; Voice Threshold prevents boosting silence and low-level noise. Defaults leave this feature off. Factory programs include Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail; programs can be adjusted after loading, and DAW preset/state features can save custom settings.

Dynamic Boost is a real-time leveler, not an offline phrase analyzer: it does not add lookahead latency or know exact phrase boundaries. Voice Threshold and envelope smoothing estimate active audio continuously. The custom VST3 editor is implemented in the separate `VoxTail.TypeB.Ui` project and includes a live input/output RMS envelope graph, Threshold and Target reference lines, and controls for all processing parameters. Its minimal white-and-pink interface uses soft card shadows; the graph is an approximate level display, not a waveform or voice detector. The UI assembly is included through the plugin project reference and is not a separate user-installed plugin.

This is a creative effect, not voice conversion: DSP can add harmonics and stylized texture, but cannot guarantee that a quiet recording will sound identical to a real human shout.

## Build and test

Requires the .NET 10 SDK and the Visual C++ build tools and Windows SDK required by NativeAOT. The build tools are required on machines that publish the plugin; they are not required on machines that only install and use the resulting VST3.

```powershell
dotnet test VoxTail-Type-B.sln
```

Publish from a regular terminal with:

```powershell
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

If this fails with **Platform linker not found**, first verify that the Visual C++ x64 tools and Windows SDK are installed. Then retry from **x64 Native Tools Command Prompt for Visual Studio** (or **Developer PowerShell for Visual Studio**) with `-p:IlcUseEnvironmentalTools=true`. This is a fallback for linker discovery, not a requirement for every machine.

The native plug-in bundle is written to `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3`. Copy that bundle directory to a VST3 scan directory or add the output directory to the DAW's plug-in paths, then rescan for **VoxTail Type-B**. NPlug's validator is included as a dependency; its execution status is documented in `docs/development.md`.

See [development setup](docs/development.md), [architecture](docs/architecture.md), and [DSP roadmap](docs/dsp-roadmap.md) for environment notes, stable plug-in IDs, validation steps, and planned work.

## License

VoxTail Type-B is open source under [GNU GPL version 3](LICENSE). Third-party components remain under their respective licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).
