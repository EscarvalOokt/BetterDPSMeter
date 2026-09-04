# Better DPS Meter testing policy

This document defines the automated testing and runtime acceptance boundaries for Better DPS Meter.

## Testing principle

Automated tests should protect meaningful project-owned contracts and realistic regressions. Test count and broad method-level coverage are not goals by themselves.

## Current validation baseline

The current production baseline has been validated with:

- a successful Release build of the production and test projects;
- a successful NUnit smoke-test run;
- in-game runtime validation with no issues found in the current Terraria 1.4.5.8 / TerrariaModder 0.4.0+ target environment.

This section records the current validation baseline only. It does not replace the testing boundaries below or serve as release history.

## Automated test boundary

The NUnit-based `BetterDPSMeter.Tests` project is reserved for deterministic behavior owned by Better DPS Meter.

The initial smoke test validates that the test project is discoverable and executable. It is infrastructure validation, not product behavior coverage.

Good candidates for deterministic tests include:

- rolling-window trimming in `DpsCombatTracker`;
- damage and hit accumulation;
- average DPS calculation;
- combat timeout transitions;
- retention of the last combat result;
- `DpsDisplaySnapshot` state transitions.

Tests should use explicit timestamps and values rather than depending on wall-clock timing.

## Runtime acceptance boundary

Behavior that depends on the complete Terraria, TerrariaModder, XNA, or Harmony runtime should be validated separately in game rather than reproduced through fragile simulations.

This includes:

- whether the mod is discovered and loaded by the supported TerrariaModder runtime;
- typed configuration loading and configuration-change behavior through TerrariaModder;
- Harmony patch application to `Player.addDPS`;
- actual local-player damage reporting from Terraria;
- the vanilla DPS Meter effect requirement;
- TerrariaModder lifecycle and frame-event integration;
- in-game overlay rendering and visibility rules;
- world-load, world-unload, and config-change behavior as invoked by the supported loader runtime.

## Adding tests

Add deterministic tests together with project-owned behavior when they protect a meaningful contract.

Keep runtime acceptance scenarios separate when the relevant semantics are owned by Terraria, TerrariaModder, Harmony, or the in-game UI environment.