# VoxTail Type-B

> **Note:** This is an experimental project. It is not a finished product and may change without notice.

VoxTail Type-B is a Windows x64 VST3 vocal effect for saturation, tone shaping, and stylized cracked vocal tails.

## Features

- **Core controls:** Input Gain, Intensity, Drive, Tone, Clean / Wet Mix, Output Gain, Bypass
- **Tail:** Tail Amount, Tail Drop, Tail Time, and Tail Character (**Crackle**, **Breaks**, **Glitch**)
- **Dynamic Boost:** a real-time leveler that lifts quiet, active audio toward Target Level (limited by Maximum Boost). Voice Threshold prevents boosting silence and noise. Off by default.
- **Factory programs:** Clean, Bite, Shout, Cracked Tail, Broken Tail, Glitch Tail
- **Custom editor:** live input/output RMS graph with Threshold and Target lines (an approximate level display, not a waveform or voice detector)

Dynamic Boost adds no lookahead latency and does not detect exact phrase boundaries. This is a creative effect, not voice conversion, so it cannot make a quiet recording sound identical to a real shout.

## Build and test

Requires the .NET 10 SDK, plus the Visual C++ build tools and Windows SDK for NativeAOT (needed only to publish, not to use the VST3).

```powershell
dotnet test VoxTail-Type-B.sln
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

If publishing fails with **Platform linker not found**, verify the Visual C++ x64 tools and Windows SDK are installed, then retry from **x64 Native Tools Command Prompt for Visual Studio** with `-p:IlcUseEnvironmentalTools=true`.

The output is `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3`. Copy it to a VST3 scan directory (or add its folder to your DAW's plug-in paths) and rescan.

See [development setup](docs/development.md), [architecture](docs/architecture.md), and [DSP roadmap](docs/dsp-roadmap.md) for more details.

## License

Open source under [GNU GPL version 3](LICENSE). Third-party components remain under their own licenses; see [third-party notices](THIRD-PARTY-NOTICES.md).