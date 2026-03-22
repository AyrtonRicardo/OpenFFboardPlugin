# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

MSBuild is not on PATH by default. Use the full path:

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" OpenFFBoardPlugin.sln /p:Configuration=Debug
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" OpenFFBoardPlugin.sln /p:Configuration=Release
```

The post-build event runs `XCOPY` to copy outputs to `%SIMHUB_INSTALL_PATH%`. This will fail if the env var isn't set — that's expected in CI/dev without SimHub installed; the DLL still compiles correctly.

To debug, set the external program target to `C:\Program Files (x86)\SimHub\SimHubWPF.exe`.

## SimHub Dependencies

SimHub DLL references (e.g. `SimHub.Plugins`, `GameReaderCommon`, `MahApps.Metro`) use the `$(SIMHUB_INSTALL_PATH)` MSBuild variable in the `.csproj`. When adding a new SimHub DLL reference, edit the `.csproj` manually using this pattern — do not use NuGet for SimHub-provided assemblies.

New dependencies must also be added to the post-build XCOPY if they aren't already present in SimHub's install directory.

## Architecture

The plugin is a SimHub plugin (`IPlugin`, `IDataPlugin`, `IWPFSettingsV2`) that communicates with OpenFFBoard hardware exclusively over HID.

**Data flow:**
```
SimHub game change / manual trigger
  → DataPlugin.UpdateProfileDataIfConnected()
  → ProfileHolder.LoadFromJson(profiles.json)     # matched by game name (case-insensitive)
  → ProfileToCommandConverter.ConvertProfileToCommands()
  → OpenFFBoard.Board (HID) method calls
```

**HID connection lifecycle** (`DataPlugin.cs`):
- `Init()` calls `OpenFFBoard.Hid.GetBoardsAsync()` to discover devices, then `ConnectToBoard(hidDeviceIndex)`.
- `ConnectToBoard()` creates `OpenFFBoard.Hid`, connects, and validates the board main class must be `FFBMain = 1`.
- The selected device index is persisted via `DataPluginSettings.SelectedHidDeviceId`.

**Profile system** (`DTO/Profile.cs`, `Utils/ProfileToCommandConverter.cs`):
- Profiles are loaded from a user-configured `profiles.json` file path.
- Each profile entry has a `Cls` ("fx" or "axis"), a `Cmd` string, and a `Value`.
- `ProfileToCommandConverter.MapCommand()` routes these to typed `OpenFFBoard.Board` API calls (`board.FX.*`, `board.Axis.*`).
- Supported `fx` commands: `filterCfFreq`, `filterCfQ`, `spring`, `friction`, `damper`, `inertia`.
- Supported `axis` commands: `power`, `degrees`, `fxratio`, `esgain`, `idlespring`, `axisdamper`, `axisfriction`, `axisinertia`.

**Settings** (`DataPluginSettings.cs`) are serialized to JSON via SimHub's `SaveCommonSettings` / `ReadCommonSettings`. The storage path is `{SimHub common storage}/{PluginName}.OpenFFBoardPluginSettings.json`.
