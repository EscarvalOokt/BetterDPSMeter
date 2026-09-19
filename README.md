# Better DPS Meter

![Better DPS Meter preview with sample combat statistics](assets/nexus/better-dps-meter-preview.png)

**Track recent combat damage with a configurable DPS overlay.**

[Download on Nexus Mods](https://www.nexusmods.com/terraria/mods/166)

Better DPS Meter adds a configurable combat overlay that tracks the local player's recent damage and displays a rolling DPS value during combat.

The overlay can also show hit count, total damage, average damage per hit, combat timeout, and the final result of the completed combat session. By default, it is only shown while the vanilla DPS Meter effect is active.

## Features

- Display rolling average DPS for the local player.
- Optionally show hits, total damage, and average damage per hit.
- Keep the final full-session combat result visible after combat ends.
- Configure the DPS averaging window.
- Configure how long the tracker remains in combat after the last hit.
- Optionally require the vanilla DPS Meter effect before showing the overlay.

## How it works

During combat, damage reported for the local player is accumulated in a configurable rolling time window. The live DPS, hit count, and damage values are calculated from the damage currently inside that window.

Combat remains active for a configurable period after the last recorded hit. When that timeout expires, Better DPS Meter replaces the rolling values with a final aggregate for the completed combat session. Final DPS uses the full session damage over the elapsed gameplay time from the first accepted hit to the last accepted hit, so the post-hit combat timeout does not reduce the final result.

## Settings

Better DPS Meter exposes its settings through the TerrariaModder configuration system.

- **Show Extra Info** — shows hit count, total damage, and average damage per hit. Default: enabled.
- **Require DPS Meter** — only shows the overlay while the DPS Meter item effect is active. Default: enabled.
- **DPS Window (seconds)** — controls how many seconds of recent damage are used for the live DPS calculation. Range: 2–60 seconds. Default: 10.
- **Combat Timeout (seconds)** — controls how long after the last hit the tracker is still considered in combat. Range: 0.5–30 seconds. Default: 3.0.

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

The automated suite includes pipeline validation and deterministic regression coverage for the project-owned combat tracker, including rolling-window behavior, combat-session transitions, and final-session aggregation. Terraria, TerrariaModder, Harmony, gameplay-clock behavior, and in-game UI integration are validated separately through runtime acceptance.

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