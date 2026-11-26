# Better DPS Meter testing policy

This document defines the automated testing and runtime acceptance boundaries for Better DPS Meter.

## Testing principle

Check mod behavior with explicit inputs in automated tests. Verify interactions with Terraria and TerrariaModder in the installed game.

## Automated test boundary

The `BetterDPSMeter.Tests` NUnit project covers four areas of mod behavior.

- **Rolling window.** Hit count, damage, elapsed time and DPS update between hits. Shrinking and expanding the window recalculates the live values from retained history. A hit at the start of an expanded window counts until it moves outside the window.
- **Completed sessions.** A hit after timeout starts a fresh session. Final DPS uses all session damage and the time between the first and last hits, even when the rolling window is empty. A single hit or simultaneous hits use the minimum measurable duration.
- **Large totals.** Live and final damage totals and DPS can exceed the 32-bit integer range.
- **Overlay placement.** Layout tests check the 1024-unit compact width threshold, the number of occupied buff rows at 800×600, and whether all five overlay lines fit above chat.

A separate smoke test checks test discovery and execution. See the [development guide](development.md#tests) for the command to run the suite.

## Runtime acceptance boundary

Enter a single-player world with **Show Extra Info** enabled. Use one local character, keep the overlay visible unless a scenario asks you to hide it, and prevent extra hits between observations. For a release candidate, check that the version in the initialization log matches the installed manifest.

1. **Meter requirement.** With **Require DPS Meter** on and the vanilla meter effect inactive, land one melee hit in a fresh fight. Activate the effect and land a second hit before the timeout. The overlay should show one hit. Turn the requirement off and land one hit in each of two fresh fights, first without the effect and then with it. Both fights should show one hit.
2. **Full-screen map.** Open the map during combat, let a minion land one more hit and stop further damage. The overlay should stay hidden. Let the timeout expire, then close the map. The final hit count should include the minion's hit.
3. **Pause timing.** With **Auto Pause** enabled, hit an enemy, immediately open the inventory and wait longer than the configured timeout. Close the inventory. The fight should still be active. Repeat with **Auto Pause** disabled and no further hits. The overlay should now show **Combat Ended**.
4. **Rolling window and final result.** Set **DPS Window** to 2 seconds and **Combat Timeout** to 30 seconds. Land two hits about 3 seconds apart and note their damage. Only the second hit should appear in the live count. Increase the window to 60 seconds before timeout. The live count should return to two. Restore the 2-second window and let combat end. The final result should show both hits, their total damage and a duration of about 3 seconds.
5. **Compact overlay.** At 800×600 and 100% UI scale, the overlay should align with the buff icons above chat with up to three buff rows. Add a fourth row if available (34 active buffs). The overlay should move back to its top position.
6. **World and settings changes.** After a completed fight, turn **Show Extra Info** off and back on. The hit and damage lines should disappear and return. Leave the world and re-enter. The overlay should start at zero, and the first hit should count once.

## Adding tests

Add an automated test when explicit inputs can demonstrate a new rule or reproduce a bug. For behavior that depends on the game, document the setup, action, and expected result in the relevant in-game scenario.