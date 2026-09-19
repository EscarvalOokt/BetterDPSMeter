# Better DPS Meter testing policy

This document defines the automated testing and runtime acceptance boundaries for Better DPS Meter.

## Testing principle

Automated tests should protect meaningful project-owned contracts and realistic regressions. Test count and broad method-level coverage are not goals by themselves.

Deterministic project behavior should be tested with explicit inputs. Behavior owned by Terraria, TerrariaModder, Harmony, or the in-game runtime should be validated through runtime acceptance rather than fragile simulations.

## Validation status

The current `1.2.0` implementation has completed release validation without identified problems.

Validation includes:

- a successful Release build of the production and test projects;
- a successful full NUnit regression-test run for project-owned deterministic combat behavior;
- completed in-game runtime acceptance in the Terraria 1.4.5.8 / TerrariaModder 0.4.0+ target environment.

The runtime acceptance covered the supported local-player tracking paths, vanilla-independent tracking, double-counting checks, restoration of temporary vanilla DPS-reporting state, tracker updates while the overlay is hidden, pause-aware combat timing, rolling-window changes with retained history, final combat-session results, lifecycle behavior, and configuration reload behavior.

## Automated test boundary

The NUnit-based `BetterDPSMeter.Tests` project is reserved for deterministic behavior owned by Better DPS Meter.

The current suite includes pipeline validation and regression coverage for the combat tracker. The deterministic contracts currently protected include:

- damage and hit accumulation inside the current rolling window;
- rolling-window filtering and snapshot refresh between hits;
- single-hit live DPS progression as covered duration grows;
- combat timeout transitions;
- splitting an expired combat session before accepting new damage;
- new-session isolation;
- stable rolling duration after older hits leave the current window;
- applying a decreased rolling-window size during active combat;
- applying an increased rolling-window size using retained history;
- restoring retained history after reducing and then increasing the rolling-window size;
- final-session aggregation even when the current rolling window has already emptied;
- exclusion of the combat-timeout tail from final DPS duration;
- minimum-duration handling for same-timestamp and single-hit final sessions.

Tests should use explicit timestamps and values rather than depending on wall-clock timing or Terraria runtime state.

## Runtime acceptance boundary

Behavior that depends on the complete Terraria, TerrariaModder, XNA, or Harmony runtime should be validated separately in game.

This includes:

- whether the mod is discovered and loaded by the supported TerrariaModder runtime;
- typed configuration loading and configuration-change behavior through TerrariaModder;
- Harmony patch application to `Player.addDPS` and the temporary vanilla DPS-reporting override scopes;
- actual local-player damage reporting from Terraria;
- absence of double counting with the real vanilla DPS Meter effect;
- restoration of temporary `accDreamCatcher` state after patched scopes;
- TerrariaModder lifecycle and `FrameEvents.OnPostUpdate` / `FrameEvents.OnUIOverlay` integration;
- tracker maintenance while presentation is hidden;
- behavior of the selected Terraria gameplay clock during pause and resume;
- inventory behavior with and without Terraria Auto Pause;
- rolling-window changes during active combat, including increasing the window after previously using a smaller value;
- final full-session combat result behavior, including exclusion of the post-hit timeout tail;
- in-game overlay rendering and visibility rules;
- world-load, world-unload, and config-change behavior as invoked by the supported loader runtime;
- runtime-reported project version and deployed package metadata when validating a release candidate.

## Adding tests

Add deterministic tests when they protect a meaningful project-owned contract or reproduce a realistic regression.

Related deterministic fixes may be covered together in a dedicated regression-test change when that keeps the implementation sequence clear and avoids duplicating temporary expectations between adjacent fixes.

Keep runtime acceptance scenarios separate when the relevant semantics are owned by Terraria, TerrariaModder, Harmony, or the in-game UI environment.