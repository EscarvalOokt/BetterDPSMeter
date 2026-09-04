# Better DPS Meter

*Track recent combat damage with a configurable DPS overlay.*

Better DPS Meter adds a configurable combat overlay that tracks the local player's recent damage and displays an average DPS value.

The overlay can also show hit count, total damage, average damage per hit, combat timeout, and the last combat result. By default, it is only shown while the vanilla DPS Meter effect is active.

## Features

- Display rolling average DPS for the local player.
- Optionally show hits, total damage, and average damage per hit.
- Keep the final combat values visible after combat ends.
- Configure the DPS averaging window.
- Configure how long the tracker remains in combat after the last hit.
- Optionally require the vanilla DPS Meter effect before showing the overlay.

## How it works

Damage reported for the local player is accumulated in a rolling time window.

The displayed DPS is calculated from damage currently inside that window. Combat remains active for a configurable period after the last recorded hit, after which the overlay marks the displayed values as the last combat result.

## Settings

Better DPS Meter exposes its settings through the TerrariaModder configuration system.

- **Show Extra Info** — shows hit count, total damage, and average damage per hit. Default: enabled.
- **Require DPS Meter** — only shows the overlay while the vanilla DPS Meter effect is active. Default: enabled.
- **DPS Window (seconds)** — controls how many seconds of recent damage are used for the DPS calculation. Range: 2–60 seconds. Default: 10.
- **Combat Timeout (seconds)** — controls how long combat remains active after the last hit. Range: 0.5–30 seconds. Default: 3.0.

## Requirements

- Terraria 1.4.5.8
- TerrariaModder 0.4.0 or newer

## Installation

Copy the `better-dps-meter` mod folder into:

```text
Terraria/TerrariaModder/mods/
```

The installed directory should contain:

```text
better-dps-meter/
├── BetterDPSMeter.dll
└── manifest.json
```

Launch Terraria using the TerrariaModder installation normally used for the game.

## Compatibility

The current implementation tracks damage for the local player and keeps its combat data in memory.

It does not add persistent character or world content. Project-owned tracking data is cleared on world load and world unload, so the mod does not require project-owned save migration when added to or removed from existing characters or worlds.

Multiplayer behavior has not been separately validated by the current project documentation.

## Development

### Prerequisites

- Terraria 1.4.5.8 installed locally.
- TerrariaModder 0.4.0 or newer installed locally.
- .NET Framework 4.8 Developer Pack and a compatible .NET SDK/build environment.

### Local configuration

Copy:

```text
docs/BetterDPSMeter.Local.props.example
```

to:

```text
src/BetterDPSMeter.Local.props
```

Then configure:

- `TerrariaDirectory` — root directory of the local Terraria installation.
- `XnaContentPipelineDll` — optional override for `Microsoft.Xna.Framework.Content.Pipeline.dll` if it is not located in the Terraria installation directory.
- `DeployToGame` — set to `true` to enable post-build deployment.
- `TerrariaModDirectory` — target `TerrariaModder/mods/better-dps-meter` directory.

The local props file contains machine-specific paths and is intentionally excluded from Git.

### Build

```bash
dotnet build src/BetterDPSMeter.sln -c Release
```

When `DeployToGame` is enabled, the production project copies `BetterDPSMeter.dll` and `mod/manifest.json` to the configured TerrariaModder mod directory after a successful build.

### Tests

```bash
dotnet test tests/BetterDPSMeter.Tests/BetterDPSMeter.Tests.csproj -c Release
```

The initial automated suite contains a pipeline smoke test. Deterministic project-owned DPS calculations are suitable for future automated coverage, while Terraria, TerrariaModder, Harmony, and in-game UI behavior should be validated separately in game.

## Repository structure

```text
docs/    Project documentation and development configuration examples
mod/     TerrariaModder package metadata
src/     Production source and solution
tests/   Automated tests
```

## Technical documentation

- [Architecture](docs/architecture.md)
- [Testing policy](docs/testing.md)

## Links

* [Nexus Mods](https://www.nexusmods.com/terraria/mods/166)
* [GitHub Releases](https://github.com/EscarvalOokt/BetterDPSMeter/releases)