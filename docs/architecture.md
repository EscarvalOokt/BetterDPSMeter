# Better DPS Meter architecture

This document describes the runtime behavior of Better DPS Meter and the responsibilities of its components.

## Sources of truth

- The [manifest](../mod/manifest.json) declares runtime requirements. The [production project](../src/BetterDPSMeter/BetterDPSMeter.csproj) defines the build target and references.
- Terraria and TerrariaModder define the external APIs. The production source defines how the mod uses them, and the [configuration model](../src/BetterDPSMeter/BetterDPSMeterConfig.cs) defines its settings.
- The [development guide](development.md) covers local setup, build, and test commands. The [testing policy](testing.md) describes automated coverage and in-game checks.

## Runtime baseline

- The manifest for version `1.3.0` specifies Terraria `1.4.6`, TerrariaModder Core `>=0.4.3`, and `client-only` multiplayer scope.
- The tested Windows configuration used Terraria `1.4.6.1` and Core `0.4.3`.
- The production project targets .NET Framework 4.8.
- TerrariaModder Core provides lifecycle callbacks, configuration, logging, and UI services. Harmony provides runtime method patching.
- Tracking and presentation are scoped to the local player represented by `Main.myPlayer`.

## Runtime flow

The current combat tracking flow is:

1. When `RequireDpsMeter` is disabled, the Harmony prefixes call `VanillaDpsTrackingOverride.Enable` to temporarily enable `Player.accDreamCatcher` on local-player damage paths that can trigger vanilla DPS reporting.
2. `PlayerAddDpsPatch` observes `Player.addDPS` through a Harmony postfix and accepts positive damage only for `Main.myPlayer`.
3. `DpsTrackerRepository` resolves the player's `DpsCombatTracker`, applies the configured rolling-window size, and supplies the current gameplay timestamp and minimum measurable gameplay interval.
4. `DpsCombatTracker` owns combat-session state, retained damage history, session aggregates, rolling statistics, final-session statistics, and timeout transitions.
5. `BetterDPSMeterMod` updates the tracker from TerrariaModder's `FrameEvents.OnPostUpdate`, independently of overlay visibility.
6. The tracker exposes presentation state through `DpsDisplaySnapshot`.
7. `BetterDPSMeterMod` also subscribes `DpsDisplayOverlay` to `FrameEvents.OnUIOverlay`.
8. `DpsDisplayOverlay` applies presentation visibility rules, reads the current snapshot, and renders the overlay without advancing combat state.

`DpsTrackerRepository.NowSeconds()` uses the pause-aware Terraria gameplay time expression `Main.GameUpdateCount * Main.TARGET_FRAME_TIME`. The same runtime boundary passes `Main.TARGET_FRAME_TIME` to the tracker as the minimum duration used when a completed session has no positive first-to-last-hit span.

## Vanilla-independent damage acquisition

`Player.addDPS` remains the canonical damage-reporting ingress used by Better DPS Meter.

When `RequireDpsMeter` is `false`, the three Harmony patches in [VanillaDpsTrackingPatches.cs](../src/BetterDPSMeter/VanillaDpsTrackingPatches.cs) call `VanillaDpsTrackingOverride.Enable` before Terraria runs the following methods.

- `Player.ItemCheck`;
- `Player.ApplyDamageToNPC(..., PlayerNPCHitSource)`;
- locally owned `Projectile.Damage`.

Each patch calls `VanillaDpsTrackingOverride.Restore` in its finalizer. The helper restores `accDreamCatcher` only when the corresponding prefix changed it, preserving an existing vanilla effect.

## Combat state

`DpsCombatTracker` accepts positive damage with an explicit gameplay timestamp. The first hit starts a session. A hit arriving after the combat timeout ends the previous session before starting the next one.

- The tracker retains up to 60 seconds of damage history (`DpsTrackingConstants.MaxWindowSeconds`). A second queue holds hits in the current window and maintains their running damage total. Increasing `WindowSeconds` rebuilds this queue from retained history.
- Live DPS divides damage in the current window by time elapsed since the first hit, capped at `WindowSeconds`. The tracker updates the snapshot on each hit and between hits, so expiring hits and configuration changes affect the live result.
- The timeout runs from the most recent hit. When it expires, the tracker saves the final snapshot before clearing session state. Final DPS divides total session damage by the gameplay time from first hit to last. A single hit or several hits on the same tick use the minimum gameplay interval supplied by the caller.
- Damage totals, DPS, and the full-session hit count use 64-bit integers.

## Display snapshot

`DpsDisplaySnapshot` carries the tracker's current result to the overlay. During combat, `AverageDps`, `HitsInWindow`, `DamageInWindow`, and `WindowSecondsCovered` describe the rolling window. `TimeoutRemaining` gives the time left before the session ends.

When `IsLastCombatResult` is true, the same fields hold the full-session DPS, hit count, total damage, and duration used to calculate final DPS. This result remains available until a new session starts or the trackers are cleared.

## Display behavior

`DpsDisplayOverlay` owns player-facing presentation.

The current overlay:

- does not draw in the game menu;
- does not draw while the game is paused;
- does not draw while the local player inventory is open;
- can require the vanilla DPS Meter effect;
- always shows DPS when visible;
- optionally shows hits, total damage, and average damage per hit;
- shows `Combat Ended` for the retained final combat-session result.

The overlay stays hidden while the full-screen map is open. With extra info enabled, the DPS line shows its covered duration to three decimal places for positive durations below 0.1 seconds and to one decimal place otherwise. The active combat countdown shows the remaining time to two decimal places.

`DpsOverlayLayout` uses the viewport dimensions in UI coordinates. At widths up to 1024, it checks the space below the occupied buff rows and above ten chat lines. It anchors a five-line block above chat when it fits. The default position at (500, 20) applies on wider screens and when the compact area is full.

Presentation visibility is separate from combat-state maintenance and from the damage-acquisition rules controlled by `RequireDpsMeter`.

## Configuration

`BetterDPSMeterMod` loads `BetterDPSMeterConfig` through TerrariaModder during initialization and reapplies the settings in `OnConfigChanged`. `ShowExtraInfo` controls the detail lines. `RequireDpsMeter` controls both the temporary damage-reporting override and overlay visibility.

`WindowSeconds` selects the live damage window, and `CombatTimeoutSeconds` sets the interval that ends a session after the last hit. The [settings reference](../README.md#settings) lists their defaults and ranges.

## Lifecycle

`BetterDPSMeterMod` implements both `IMod` and `IModLifecycle`.

The current lifecycle responsibilities are:

- initialize logging, typed configuration, the overlay, and the `OnPostUpdate` / `OnUIOverlay` frame-event subscriptions;
- update the local player's tracker from `OnPostUpdate`;
- clear project-owned tracker state on world load and world unload;
- reload runtime-facing settings when configuration changes;
- unsubscribe from frame events and clear project-owned state during unload.

## Project boundaries

- Production source remains in a single project.
- `DpsTrackingConstants` owns the shared maximum supported rolling-window value used by configuration and retained-history trimming.
- `DpsCombatTracker` owns deterministic rolling-window behavior, retained damage history, combat-session aggregation, final-session calculation, and timeout transitions.
- `DpsTrackerRepository` owns tracker lookup, propagation of configured window size, the Terraria gameplay-clock boundary, and propagation of the minimum gameplay interval.
- `DpsDisplaySnapshot` transfers active rolling state or the completed session aggregate to presentation.
- `DpsDisplayOverlay` owns overlay visibility, text formatting, and rendering. It reads the current snapshot independently of tracker updates.
- `DpsOverlayLayout` computes the overlay position from the viewport dimensions and occupied buff rows.
- `PlayerAddDpsPatch` is the canonical Better DPS Meter ingress for Terraria DPS reports.
- `VanillaDpsTrackingOverride` manages the temporary `accDreamCatcher` override used by the three local-player damage patches when `RequireDpsMeter` is disabled.
- `BetterDPSMeterConfig` owns the typed configuration contract.
- `BetterDPSMeterMod` owns TerrariaModder initialization, lifecycle integration, configuration application, periodic tracker updates, and UI overlay subscription.