# AizenX v2.2.1 Premium Redesign

## Design rationale
1. Keep the compiler/search backend untouched and make trust visible in the interface.
2. Use a dark-luxury forge direction instead of gamer-RGB styling.
3. Give verified formats a persistent, collapsible status surface on conversion screens.
4. Improve hierarchy with grouped navigation, compact header, command palette and task status.
5. Use crimson only for primary actions/active state so it keeps meaning.
6. Use brass for premium accents and emerald exclusively for verified/success states.
7. Keep paths/logs in mono typography and working UI in a neutral grotesk.
8. Replace mixed symbols with a consistent geometric/utility language.
9. Preserve performance by avoiding large blurred glass layers in WinForms.
10. Respect Reduce Motion; motion is limited to existing progress/status/hover behaviors.

## Name treatments
- **AIZENX FORGE** — *Verified RDR2 Asset Pipeline* — **recommended**
- **AizenX Studio** — *Build. Verify. Mod.*
- **AIZEN//X** — *Native Asset Engineering*
- **AizenX Atelier** — *Precision Tools for RDR2 Modding*
- **AizenX Works** — *Forge Native. Verify Everything.*

### Recommendation
**AIZENX FORGE** keeps the existing Aizen identity, sounds professional, and directly reflects the compile/rebuild purpose without borrowing anime or Rockstar branding.

## Logo
Original geometric AX mark:
- dark rounded plate
- crimson forged A/X strokes
- brass crossbar
- subtle metallic edge highlight
- designed to remain legible at 16px, 32px and 256px

Files:
- `AizenX_Forge_Logo.svg` — horizontal lockup
- `AizenX_Forge_Mark.svg` — icon-only vector
- `AizenX_Forge_Icon_256.png` — raster preview
- `AizenX_Forge.ico` — multi-size Windows icon

### ICO export guide
Use the SVG/256 PNG as the master. Export 16, 24, 32, 48, 64, 128 and 256px square PNGs, then bundle them into one 32-bit RGBA ICO. The project `app.ico` already contains those sizes.

## Typography
- UI: Inter if locally installed; fallback Segoe UI Variable
- Display: Space Grotesk if locally installed; fallback Segoe UI Variable Display
- Mono: JetBrains Mono if locally installed; fallback Cascadia Mono
- No external CDN/font dependency is required.

## Implemented shell
- grouped sidebar: Convert / Discover / System
- active crimson rail
- compact AIZENX FORGE header
- Ctrl+K command palette
- Engine Ready + Safe Mode shortcuts
- bottom live queue/task status
- Verified Formats column on Export / Build / Native YMT
- Verified Build shield in sidebar
- path-validity indicators in Settings
- colored log severity legend
- original procedural header treatment with optional local custom artwork

## Verified panel data
- RSC8 Scenario YMT — Verified
- PSO / MetaPed — Verified
- YFT — Verified
- YDD — Verified
- YTD — Verified
- Blackwater — 375 entity overrides, 455 scenario points retained
- Unproven/native-fallback families remain amber instead of being falsely labeled verified

## Component checklist
- Primary button: crimson gradient, top highlight, hover glow, pressed offset, disabled muted
- Secondary button: dark surface + hairline border, hover elevation
- Destructive: outlined/error treatment
- Inputs: near-black elevated surface, mono for paths
- Toggle: clear on/off position with high contrast
- Chip: success/amber states
- Card: 10–12px radius, subtle border, layered near-black surface
- Nav item: icon + label, crimson active rail
- Status pill: colored dot + semantic status
- Toast: compact bottom-right message
- Progress: thin crimson task indicator
- Empty state: descriptive text instead of blank control
- Verified row: icon, name, detail, verified/unproven chip, Use action

## Motion
WinForms performance-first implementation:
- hover/press button feedback
- active nav movement through page state
- existing progress/status pulse
- toast timing
- Reduce Motion disables/minimizes nonessential motion

Large blur/parallax animation is intentionally not used because WinForms would pay a significant repaint cost.

## Integration
The existing project is WinForms, so replacing it with WPF/Electron would risk backend/control IDs. The redesign is implemented directly in WinForms while preserving event handlers and backend calls.

1. Keep `Master_v2.2_Source\AizenX` backend files unchanged unless fixing a proven backend bug.
2. UI implementation lives mainly in `MainForm.cs`, `UiTheme.cs`, `AizenButton.cs`, `NavButton.cs`, `BrandMark.cs`, and `VerifiedFormatsPanel.cs`.
3. `app.ico` is the Windows executable icon.
4. Build with `dotnet build AizenX.csproj -c Release`.
5. Run the Blackwater/PSO/YFT/YDD/YTD regression gate before replacing a public release.