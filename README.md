# AizenX Forge v2.2.1

AizenX Forge is a Windows desktop utility for Red Dead Redemption 2 modding workflows.

It can search verified RPF archive assets, export supported resources to XML, rebuild supported XML/native resources with validation, compile Scenario YMT metadata, and prepare output for LML workflows.

## Source review

This repository contains the AizenX Forge v2.2.1 application source corresponding to the current release.

Compiled release binaries, PDB files, Rockstar game files, Oodle files, CodeX binaries, and RDR2 Manifest Tool binaries are intentionally not stored in this repository.

## Main features

- RPF archive-backed asset search and raw hash lookup
- Export supported RDR2 resources to semantic XML where available
- Safe native fallback for unsupported semantic XML formats
- Build supported XML resources back to native files
- Native RSC8 Scenario YMT compilation and round-trip verification
- PSO / MetaPed metadata compilation and verification
- Direct XML-text `.ymt` input support
- YFT, YDD, and YTD rebuild verification
- LML-ready output workflow
- File inspection, XML comparison, logs, Safe Mode, and verification UI

## Verified v2.2.1 regressions

- Blackwater Scenario YMT: 96,371 / 96,371 XML elements retained
- Blackwater test data: 375 entity overrides and 455 scenario points retained
- PSO / MetaPed: 10,992 / 10,992 exact semantic round trip
- Valentine, Aberdeen, and Adler RSC8 scenario tests passed
- YFT, YDD, and YTD rebuild tests passed
- Native fallback regression passed

These are structural/build verification results. Individual mod compatibility can still depend on the game, load order, and other installed mods.

## Build

See [BUILDING.md](BUILDING.md).

## Third-party components

AizenX does not redistribute CodeX, Oodle, Rockstar game files, or RDR2 Manifest Tool components in this repository.

See [THIRD_PARTY_NOTICE.md](THIRD_PARTY_NOTICE.md).

## License

AizenX source is released under the MIT License. See [LICENSE](LICENSE).

## Repository

Official source repository:
https://github.com/4izenX/AizenX-RDR2-XML-Forge2.2.1