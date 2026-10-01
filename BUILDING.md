# Building AizenX Forge v2.2.1

## Requirements

- Windows 10/11 x64
- .NET 10 SDK
- RDR2 Manifest Tool installed separately
- Local build references:
  - `CodeX.Core.dll`
  - `CodeX.Games.RDR2.dll`

## Prepare local build references

AizenX does not redistribute the CodeX DLLs.

Locate the RDR2 Manifest Tool exporter runtime on your own machine and copy these two files into the repository's `BuildRefs` folder:

- `CodeX.Core.dll`
- `CodeX.Games.RDR2.dll`

The project expects:

```text
AizenX.csproj
BuildRefs/
  CodeX.Core.dll
  CodeX.Games.RDR2.dll
```

These references are marked with `<Private>false</Private>` and are not intended to be bundled as AizenX-owned components.

## Build command

From the repository root:

```powershell
dotnet build AizenX.csproj -c Release
```

Output:

```text
bin\Release\net10.0-windows7.0\
```

## Notes for reviewers

- Target framework: `net10.0-windows7.0`
- Platform: x64
- UI: Windows Forms
- `GenerateAssemblyInfo` is disabled; version metadata is in `Properties\AssemblyInfo.cs`
- AizenX can run as a GUI and also exposes CLI operations through the same executable
- Conversion work may launch the configured RDR2 exporter/verifier as a child process
- Timeout handling can terminate only a child exporter/verifier process started by AizenX
- No downloader/self-updater, registry startup persistence, or process-injection implementation is present in the AizenX source