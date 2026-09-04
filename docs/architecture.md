# Better DPS Meter architecture

This document defines the accepted architecture and implementation boundaries of Better DPS Meter.

It describes the current 1.1.0 production baseline. It is not a RoadMap or implementation guide.

## Sources of truth

1. The installed Terraria and TerrariaModder runtime define external runtime behavior and API contracts.
2. The current production implementation defines behavior actually available in the built mod.
3. `mod/manifest.json` defines the current package metadata and supported runtime requirements.
4. `BetterDPSMeterConfig` defines the current typed configuration contract.
5. This document records the current Better DPS Meter architecture.
6. `docs/testing.md` defines automated testing and runtime acceptance boundaries.

When documentation and production code disagree about current project behavior, production code is the source of truth until the discrepancy is resolved explicitly.

## Runtime baseline

- Better DPS Meter 1.1.0.
- Terraria 1.4.5.8.
- TerrariaModder 0.4.0 or newer.
- .NET Framework 4.8.
- Harmony is used to observe local-player DPS updates.
- TerrariaModder provides mod context, typed configuration, logging, lifecycle callbacks, frame events, and UI rendering infrastructure used by the current implementation.
- The current baseline has passed Release build, automated smoke-test, and in-game runtime validation.

## Runtime flow

The current combat tracking path is:

1. `PlayerAddDpsPatch` observes `Player.addDPS` calls through a Harmony postfix.
2. Damage is accepted only for `Main.myPlayer`.
3. `DpsTrackerRepository` stores a `DpsCombatTracker` for the player and supplies the current timestamp.
4. `DpsCombatTracker` keeps recent damage events inside the configured rolling window.
5. The tracker rebuilds `DpsDisplaySnapshot` values for average DPS, hits, total damage, covered time, and combat timeout.
6. `DpsDisplayOverlay` is called from TerrariaModder's UI overlay frame event.
7. The overlay updates combat state and renders the current or last-combat snapshot.

## Combat state

`DpsCombatTracker` owns the project-specific combat model.

- Positive damage starts combat when no combat is active.
- Damage events older than `WindowSeconds` are removed from the rolling window.
- Average DPS is calculated from damage inside the current window and the time covered by that window.
- Combat ends when the configured timeout after the last hit reaches zero.
- Ending combat clears rolling damage events while keeping the last display values and marking them as the last combat result.

## Display behavior

`DpsDisplayOverlay` owns player-facing presentation.

The current overlay:

- does not draw in the game menu;
- does not draw while the game is paused;
- does not draw while the local player inventory is open;
- can require the vanilla DPS Meter effect;
- always shows DPS when visible;
- optionally shows hits, total damage, and average damage per hit;
- shows the remaining combat timeout while combat is active;
- shows `Combat Ended` for the retained last-combat result.

## Configuration

`BetterDPSMeterConfig : ModConfig` defines the current TerrariaModder configuration contract.

The current typed settings are:

- `ShowExtraInfo` — enabled by default;
- `RequireDpsMeter` — enabled by default;
- `WindowSeconds` — default `10`, range `2–60`;
- `CombatTimeoutSeconds` — default `3.0`, range `0.5–30.0`.

`BetterDPSMeterMod` obtains the configuration through `context.GetConfig<BetterDPSMeterConfig>()` during initialization and applies the current values to the runtime-facing properties used by tracking and presentation.

Configuration changes are handled through the mod lifecycle integration and reload the runtime-facing values from the typed configuration object.

## Lifecycle

`BetterDPSMeterMod` implements both `IMod` and `IModLifecycle`.

The current lifecycle responsibilities are:

- initialize logging, typed configuration, the overlay, and frame-event subscription;
- clear project-owned tracker state on world load and world unload;
- reload runtime-facing settings when configuration changes;
- unsubscribe from frame events and clear project-owned state during unload.

## Project boundaries

- Production source remains in a single project.
- `DpsCombatTracker` owns rolling-window and combat-timeout behavior.
- `DpsTrackerRepository` owns tracker lookup and the current timestamp helper.
- `DpsDisplaySnapshot` is the state transferred from tracking to presentation.
- `DpsDisplayOverlay` owns overlay presentation.
- `PlayerAddDpsPatch` is the Harmony boundary for Terraria DPS updates.
- `BetterDPSMeterConfig` owns the typed configuration contract.
- `BetterDPSMeterMod` owns TerrariaModder initialization, lifecycle integration, configuration application, and overlay subscription.
- Machine-specific Terraria paths belong only in `src/BetterDPSMeter.Local.props` and must not be committed.