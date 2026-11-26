# Better DPS Meter development guide

## Prerequisites

- A .NET SDK with a C# 12 or later compiler, such as .NET SDK 8, and the .NET Framework 4.8 development tools for building the `net48` solution on Windows.
- A local installation of [Terraria and TerrariaModder Core](../README.md#requirements). The project references `Terraria.exe`, `TerrariaModder.Core.dll`, and the `0Harmony.dll` distributed with Core.
- `Microsoft.Xna.Framework.Content.Pipeline.dll`. The project looks for this assembly in the Terraria installation folder by default.

## Local configuration

Copy [BetterDPSMeter.Local.props.example](BetterDPSMeter.Local.props.example) to `src/BetterDPSMeter.Local.props` and set the paths for your installation. Git ignores the local file.

| Property | Purpose |
| --- | --- |
| `TerrariaDirectory` | Path to the folder containing `Terraria.exe` and `TerrariaModder`. |
| `XnaContentPipelineDll` | Path to `Microsoft.Xna.Framework.Content.Pipeline.dll` if it is installed outside `TerrariaDirectory`. |
| `DeployToGame` | Set to `true` to copy the mod into the game after a successful build. Defaults to `false`. |
| `TerrariaModDirectory` | Deployment destination. Defaults to `TerrariaModder/mods/better-dps-meter` under `TerrariaDirectory`. |

## Build

From the repository root, build the solution in Release configuration with deployment disabled.

```powershell
dotnet build src/BetterDPSMeter.sln -c Release -p:DeployToGame=false
```

To deploy to your local game, close Terraria and run the same command with `-p:DeployToGame=true`. After a successful build, the project copies `BetterDPSMeter.dll` and `mod/manifest.json` to `TerrariaModDirectory`.

## Tests

From the repository root, run the NUnit test project. This command also builds the mod with deployment disabled, even if deployment is enabled in your local configuration.

```powershell
dotnet test tests/BetterDPSMeter.Tests/BetterDPSMeter.Tests.csproj -c Release -p:DeployToGame=false
```

The [testing policy](testing.md) describes what the NUnit suite covers and what to check in game.