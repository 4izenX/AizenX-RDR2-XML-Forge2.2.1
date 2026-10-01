AizenX Forge v2.2.1

- Restored and merged the proven v2.2.1 native metadata compiler path.
- Fixed hashed metadata/class handling, override-map arrays and flags used by Scenario YMT.
- Direct XML-text .ymt input is accepted without requiring a .ymt.xml rename.
- Blackwater exact RSC8 round trip: 96,371 / 96,371 elements.
- Blackwater regression retains 375 entity overrides and 455 scenario points.
- PSO/MetaPed exact round trip: 10,992 / 10,992 elements on player_zero regression.
- RSC8 scenario regressions passed for Valentine, Aberdeen and Adler.
- YFT, YDD and YTD rebuild regression checks passed.
- Universal archive search uses verified RPF identities and raw-hash lookup.
- Unsupported semantic XML families use safe native fallback when possible.
- Recoverable native fallback no longer logs a misleading FAIL before success.
- Added child-process timeout protection for stuck exporter/verifier processes.
- Premium AIZENX FORGE redesign: grouped navigation, command palette, verified-formats panel, path validity, improved logs and original AX branding.
- Corrected executable/product metadata to 2.2.1 and replaced the old app icon.
- UI polish: deterministic Export startup, reliable Archive/Loose segmented mode state, wider owner-drawn Verified Formats rows, no chip/button overlap or repaint placeholders.