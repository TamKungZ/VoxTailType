# VoxTail Type-B project record

## Product identity

- Display name: **VoxTail Type-B**
- Repository: https://github.com/TamKungZ/VoxTailType
- Maintainer: TamKungZ_ (dev@tamkungz.me)
- .NET namespaces: `VoxTail.TypeB.*`
- Processor class ID: `1b3718ed-03a5-4a00-8879-cc3245cc8938`
- Controller class ID: `7f573153-82aa-4292-a701-357499ce2ad8`

The class IDs are persistent identifiers. Do not regenerate them for subsequent builds or Type variants.

## Current milestone

The plugin now connects an allocation-free managed DSP chain to the NPlug VST3 processor. Its host-automatable controls implement input/output gain, saturation and intensity, tone, wet/dry mix, and three manually adjustable tail characters. It includes factory programs and synthetic tests for transparency, added drive, tail texture, non-finite input, output bounds, and callback allocations.

The effect uses envelope-based tail triggering as a creative heuristic; it does not guarantee a human-like shout, and mono bus layouts and target-DAW auditioning remain to be validated.

## License

VoxTail Type-B is licensed under GNU GPL version 3. See [LICENSE](LICENSE). NPlug and its related dependencies retain their own BSD-2-Clause licenses. The GPLv3 license choice follows the open-source licensing option described in the NPlug and Steinberg VST3 licensing documentation; see [third-party notices](THIRD-PARTY-NOTICES.md).
