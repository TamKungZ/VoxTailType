# Development and validation

## Requirements

- .NET 10 SDK
- Windows x64 C++ build tools and Windows SDK for NativeAOT
- A VST3-compatible DAW for manual host testing

## Commands

Run from the repository root:

```powershell
dotnet test VoxTail-Type-B.sln
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

NativeAOT publish output is configured under `artifacts\VoxTail-Type-B\win-x64`. The publish target packages the native module as a VST3 bundle at `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3\Contents\x86_64-win\VoxTail.TypeB.vst3`.

## Validation

The test project uses NPlug.Validator to check the plug-in factory. If the published bundle exists, the same test also runs the VST3 validator against that native bundle. Run tests after publishing to validate both:

```powershell
dotnet test VoxTail-Type-B.sln
```

The plugin exposes host-automatable processing and tail controls, plus factory programs named Clean, Bite, Shout, Cracked Tail, Broken Tail, and Glitch Tail. Load a program as a starting point, adjust its parameters, then use the DAW's preset/state save feature for custom settings.

## Install in a DAW

Copy the `VoxTail Type-B.vst3` bundle directory into a VST3 scan directory (commonly `%CommonProgramFiles%\VST3`) or add `artifacts\VoxTail-Type-B\win-x64` to the DAW's plug-in paths. Rescan and load **VoxTail Type-B** as an audio effect. If the scan fails, confirm the DAW is 64-bit, rescan the output path, and consult the DAW's plug-in scan log.

Manual loading in a DAW is separate from automated tests; test the published bundle in the target host before distributing it.
