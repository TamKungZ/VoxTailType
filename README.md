# VoxTail Type-B

VoxTail Type-B is a Windows x64 VST3 vocal-effect project. Its first milestone is a native, zero-latency passthrough effect; the DSP core is kept separate from the NPlug host lifecycle so later processing can be developed and tested independently.

## Build and test

Requires the .NET 10 SDK and the Visual C++ build tools needed by NativeAOT.

```powershell
dotnet test VoxTail-Type-B.sln
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

The native plug-in bundle is written to `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3`. Copy that bundle directory to a VST3 scan directory or add the output directory to the DAW's plug-in paths, then rescan for **VoxTail Type-B**. NPlug's validator is included as a dependency; its execution status is documented in `docs/development.md`.

See [development setup](docs/development.md), [architecture](docs/architecture.md), and [DSP roadmap](docs/dsp-roadmap.md) for environment notes, stable plug-in IDs, validation steps, and planned work.
