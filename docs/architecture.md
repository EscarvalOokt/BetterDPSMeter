# Better DPS Meter architecture

This document defines the accepted architecture and implementation boundaries of Better DPS Meter as represented by the current project source.

It is not a RoadMap, release history, or implementation guide.

## Sources of truth

1. The current production implementation defines behavior actually present in the built mod.
2. The supported Terraria and TerrariaModder runtime contracts define external integration behavior.
3. `mod/manifest.json` defines package metadata and supported framework requirements.
4. `BetterDPSMeterConfig` defines the typed configuration contract.
5. This document records stable Better DPS Meter architecture and responsibility boundaries.
6. `docs/testing.md` defines automated testing and runtime acceptance boundaries.

When documentation and production code disagree about current project behavior, production code is the source of truth until the discrepancy is resolved explicitly.

## Runtime baseline

- Current project version identifiers are `1.2.0`.
- Terraria 1.4.5.8.
- TerrariaModder 0.4.0 or newer.
- .NET Framework 4.8.
- Harmony provides the Terraria method-patching boundary used by damage tracking.
- TerrariaModder provides mod context, typed configuration, logging, lifecycle callbacks, frame events, and UI rendering infrastructure.
- Tracking and presentation are scoped to the local player represented by `Main.myPlayer`.

## Runtime flow

The current combat tracking flow is:

1. When `RequireDpsMeter` is disabled, `VanillaDpsTrackingPatches` temporarily enables `Player.accDreamCatcher` around the confirmed local-player damage paths that can lead to vanilla DPS reporting.
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

When `RequireDpsMeter` is `false`, Better DPS Meter does not independently calculate Terraria damage. Instead, `VanillaDpsTrackingPatches` temporarily enables the vanilla DPS-reporting condition for confirmed local-player damage scopes:

- `Player.ItemCheck`;
- `Player.ApplyDamageToNPC(..., PlayerNPCHitSource)`;
- locally owned `Projectile.Damage`.

The temporary override is restored in Harmony finalizers so Better DPS Meter does not persistently enable `accDreamCatcher`.

When the vanilla effect is already active, the override is not applied.

## Combat state

`DpsCombatTracker` owns the project-specific deterministic combat model.

- Non-positive damage is ignored.
- Positive damage starts combat when no combat is active.
- Before accepting new positive damage, an already expired active combat session is ended so the new hit starts a clean session.
- `_combatStartTime` records the first accepted hit timestamp of the active combat session.
- `_lastHitTime` records the most recent accepted hit timestamp and drives combat-timeout calculations.
- `_sessionTotalDamage` and `_sessionHitCount` accumulate the full active combat session independently of rolling-window history selection.
- Damage history is retained up to `DpsTrackingConstants.MaxWindowSeconds`, which also defines the maximum supported configurable rolling window.
- The current `WindowSeconds` selects which retained damage events contribute to the live rolling snapshot; reducing the window does not discard retained history that may become relevant again if the window is increased.
- Live rolling duration is based on elapsed time since the combat-session start and is capped at `WindowSeconds`; retained-history trimming does not collapse the rolling denominator.
- `AddDamage()` rebuilds the live display snapshot after accepting a hit.
- `UpdateState()` rebuilds the live rolling snapshot while combat remains active, allowing damage, hit count, covered duration, and DPS to change between hits or when the configured window changes.
- Combat ends when the configured timeout after the last hit reaches zero.
- Ending combat builds a final aggregate from the full session before project-owned session state is cleared.
- Final DPS uses full-session damage divided by the elapsed gameplay time from the first accepted hit to the last accepted hit, so the post-hit combat-timeout tail is excluded.
- If that first-to-last-hit span is zero, the final calculation uses the minimum gameplay interval supplied by the runtime boundary.

The tracker accepts explicit timestamps and the minimum measurable duration from its caller. Terraria-specific clock selection remains outside `DpsCombatTracker` itself.

## Display snapshot

`DpsDisplaySnapshot` is the state-transfer boundary between combat tracking and presentation.

It contains:

- average DPS;
- hit count;
- damage;
- covered duration;
- remaining combat timeout;
- whether the values represent the completed combat-session result.

The existing property names retain their rolling-window terminology, but their semantics depend on `IsLastCombatResult`:

- while combat is active, `AverageDps`, `HitsInWindow`, `DamageInWindow`, and `WindowSecondsCovered` represent the live rolling snapshot;
- when `IsLastCombatResult` is `true`, the same fields represent the final full-session aggregate and its first-to-last-hit duration.

The snapshot contains no TerrariaModder UI behavior and no runtime event ownership.

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
- shows `Combat Ended` for the retained final combat-session result.

Presentation visibility is separate from combat-state maintenance and from the damage-acquisition rules controlled by `RequireDpsMeter`.

## Configuration

`BetterDPSMeterConfig : ModConfig` defines the TerrariaModder configuration contract.

The current typed settings are:

- `ShowExtraInfo` — enabled by default;
- `RequireDpsMeter` — enabled by default;
- `WindowSeconds` — default `10`, range `2–60`, with the maximum supplied by `DpsTrackingConstants.MaxWindowSeconds`;
- `CombatTimeoutSeconds` — default `3.0`, range `0.5–30.0`.

`BetterDPSMeterMod` obtains the configuration through `context.GetConfig<BetterDPSMeterConfig>()` during initialization and copies the current values to runtime-facing properties used by tracking and presentation.

Configuration changes are handled through `IModLifecycle.OnConfigChanged()`.

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
- `DpsDisplayOverlay` owns overlay visibility and rendering only; it does not advance tracker state.
- `PlayerAddDpsPatch` is the canonical Better DPS Meter ingress for Terraria DPS reports.
- `VanillaDpsTrackingPatches` owns the temporary vanilla DPS-reporting override required when `RequireDpsMeter` is disabled.
- `BetterDPSMeterConfig` owns the typed configuration contract.
- `BetterDPSMeterMod` owns TerrariaModder initialization, lifecycle integration, configuration application, periodic tracker updates, and UI overlay subscription.
- Machine-specific Terraria paths belong only in `src/BetterDPSMeter.Local.props` and must not be committed.