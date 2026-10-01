# Nexus Mods Source Review Notes

Project: AizenX Forge  
Version: 2.2.1  
Target: .NET 10 / Windows Forms / x64

This repository provides the source corresponding to the submitted AizenX Forge v2.2.1 release for moderation and security review.

## Build

Follow `BUILDING.md`.

Third-party CodeX build-reference DLLs are intentionally excluded from the repository and must be obtained from the reviewer's own RDR2 Manifest Tool installation.

## Runtime behavior relevant to review

- No PowerShell or `cmd.exe` launcher is implemented by AizenX.
- No network downloader or self-updater is implemented.
- No registry startup persistence is implemented.
- No process-injection or remote-thread implementation is present.
- Conversion operations may start the configured RDR2 exporter/verifier executable as a child process.
- A timeout may terminate only a child conversion process launched by AizenX.
- Native Windows console APIs are used so the same executable can support both GUI and CLI operation.

## Release verification

The v2.2.1 regression set includes:

- RSC8 Scenario YMT
- PSO / MetaPed
- YFT
- YDD
- YTD
- Archive-backed asset search
- Native fallback

See `README.md` and `CHANGELOG.md` for the release details.

No off-site download link is required for source review.

## Repository submitted for review

https://github.com/4izenX/AizenX-RDR2-XML-Forge2.2.1