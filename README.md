# HST Mulligan V2

An overlay plugin for [Hearthstone Deck Tracker (HDT)][hdt] that shows keep-rate
guidance for each mulligan card, using aggregated statistics served by Firestone
or an HSReplay-shaped feed.

## Features

- Per-card **keep-rate percentage** on every mulligan slot.
- **Color-graded decision ring** (strong keep -> keep -> neutral -> toss -> strong toss).
- **Confidence badge** driven by sample size (small n = washed-out ring).
- Auto-detected filters: **opponent class**, **format** (Standard / Wild / Twist),
  **rank bracket**, and **deck code** when the current deck is imported.
- **Details panel** (F9 or click a badge) with per-opponent breakdown and raw
  sample counts.
- Pluggable data sources: `FirestoneDataSource` (default),
  `HsReplayDataSource` (community JSON shape), local file, or your own.
- Cache layer with configurable TTL; works offline once primed.
- Zero external calls until the mulligan phase opens.

## Repository layout

```
src/
  MulliganCore/         Pure .NET class library. No HDT dependency.
                        Models, data sources, keep-rate math, geometry.
  MulliganCore.Tests/   xUnit tests for the core.
  MulliganPlugin/       HDT plugin. Implements IPlugin, hosts the WPF overlay.
scripts/
  fetch-hdt.ps1         Downloads the current HDT release and copies the
                        required DLLs into lib/ so MulliganPlugin can build.
docs/
  SPEC.md               Feature parity spec and data-shape contracts.
```

## Building

1. Install .NET Framework 4.8 Developer Pack and MSBuild 17+ (Visual Studio 2022
   or Build Tools).
2. Run `pwsh scripts/fetch-hdt.ps1` to populate `lib/` with HDT's SDK DLLs.
3. `dotnet restore` then `msbuild HST-MulliganV2.sln /p:Configuration=Release`.

The plugin artifact is
`src/MulliganPlugin/bin/Release/net48/MulliganPlugin.dll` plus its
`MulliganCore.dll` dependency.

## Installing

Copy `MulliganPlugin.dll` and `MulliganCore.dll` into
`%APPDATA%\HearthstoneDeckTracker\Plugins\MulliganV2\` and enable the plugin
under `Options -> Tracker -> Plugins`.

## Data sources

By default the plugin uses `FirestoneDataSource`, which reads the public
mulligan-guide JSON that Firestone publishes. Swap it out by editing
`Config/PluginSettings.cs` or by supplying a URL override in the settings
window.

The JSON shape expected by both built-in sources is documented in
[`docs/SPEC.md`](docs/SPEC.md).

## License

MIT. See `LICENSE` (added on first release). No proprietary Hearthstone or
third-party assets are shipped in this repository.

[hdt]: https://hsdecktracker.net/
