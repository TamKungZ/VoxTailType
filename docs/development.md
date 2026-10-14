# Development and validation

## Environment checked

Checked on Windows x64 on 2026-10-14:

- .NET SDKs installed: 8.0.425 and 10.0.401; .NET 10.0.401 is selected because it is the installed current LTS SDK.
- MSVC toolset: 14.51.36231 is installed through Visual Studio Community 18.
- Git: 2.56.0.windows.1.
- No standalone `vst3validator` command was found on PATH. The project references NPlug.Validator 1.4.0.380, which wraps the official VST3 validator.
- NPlug 0.5.0.380 supports .NET 7+ and NativeAOT. Its current sample targets .NET 10.

## Commands

Run from the repository root:

```powershell
dotnet test VoxTail-Type-B.sln
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true
```

NativeAOT publish output is configured under `artifacts\VoxTail-Type-B\win-x64`. The publish target packages the native module as a VST3 bundle at `artifacts\VoxTail-Type-B\win-x64\VoxTail Type-B.vst3\Contents\x86_64-win\VoxTail.TypeB.vst3`. Build and publish outcomes are recorded separately; a normal managed build is not a substitute for a NativeAOT publish.

## Results checked

- `dotnet test VoxTail-Type-B.sln --verbosity minimal` — passed, 10 tests, 0 failures. This includes validation of both the plugin factory and the published native VST3 bundle.
- `dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true -p:IlcUseEnvironmentalTools=true --verbosity minimal` — passed after process-local `PATH`, `LIB`, and `INCLUDE` were set to the installed MSVC and Windows SDK locations. The ordinary command without these environment settings failed because this Visual Studio installation's C++ tools were not discoverable through `vswhere`.
- The standalone `vst3validator` executable was not present on `PATH`; the VST3 Validator bundled with NPlug.Validator ran against the native bundle and passed.
- No DAW was available for a manual scan/load check.

## Validator and DAW host check

The NPlug.Validator package is referenced so the plugin factory can be validated with `AudioPluginValidator.Validate(VoxTailTypeBPlugin.GetFactory().Export, Console.Out, Console.Error)`. This call is not run from production audio code.

The validator test always checks the plugin factory and also validates the generated `.vst3` bundle directly when that published bundle exists. If it does not exist, the test output explicitly reports that the native-binary check was skipped.

After publishing, copy the generated `VoxTail Type-B.vst3` bundle directory into a VST3 scan directory (commonly `%CommonProgramFiles%\VST3`) or add `artifacts\VoxTail-Type-B\win-x64` to the DAW's plug-in paths. Rescan and load **VoxTail Type-B** as an audio effect. If the scan fails, confirm the DAW is 64-bit, rescan the exact output path, and consult the DAW's plug-in scan log.

## Notes

The local Visual Studio installation contains the MSVC linker and Windows SDK but its C++ component is not discoverable by `vswhere`; the process-local environment setup used for this build did not change machine-wide environment settings. On a standard Visual Studio C++ workload installation, the ordinary publish command above should discover the toolchain automatically.

The exact process-local setup used here was:

```powershell
$vc = 'G:\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231'
$sdk = 'C:\Program Files (x86)\Windows Kits\10'
$sdkVersion = '10.0.28000.0'
$env:PATH = "$vc\bin\Hostx64\x64;$sdk\bin\$sdkVersion\x64;$env:PATH"
$env:LIB = "$vc\lib\x64;$sdk\Lib\$sdkVersion\ucrt\x64;$sdk\Lib\$sdkVersion\um\x64"
$env:INCLUDE = "$vc\include;$sdk\Include\$sdkVersion\ucrt;$sdk\Include\$sdkVersion\shared;$sdk\Include\$sdkVersion\um;$sdk\Include\$sdkVersion\winrt;$sdk\Include\$sdkVersion\cppwinrt"
dotnet publish src\VoxTail.TypeB.Plugin\VoxTail.TypeB.Plugin.csproj -c Release -r win-x64 -p:PublishAot=true -p:IlcUseEnvironmentalTools=true
```
