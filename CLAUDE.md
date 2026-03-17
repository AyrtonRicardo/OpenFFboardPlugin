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

## Tests

Tests live in `OpenFFBoardPlugin.Tests/`. The test project targets .NET Framework 4.8 and uses **NUnit 3**.

### First-time setup — restore NuGet packages

```
nuget restore OpenFFBoardPlugin.sln
```

Or open the solution in Visual Studio and let it restore automatically. NUnit packages land in the shared `packages/` folder.

### Build the test project

```
powershell -Command "& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' OpenFFBoardPlugin.sln /p:Configuration=Debug /v:minimal"
```

### Run all tests (command line)

```
packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe OpenFFBoardPlugin.Tests\bin\Debug\OpenFFBoardPlugin.Tests.dll
```

### Run a single test

```
packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe OpenFFBoardPlugin.Tests\bin\Debug\OpenFFBoardPlugin.Tests.dll --test=OpenFFBoardPlugin.Tests.JsonHandlerTests.LoadFromJsonFile_FileNotFound_ReturnsNull
```

### Run a single test fixture

```
packages\NUnit.ConsoleRunner.3.17.0\tools\nunit3-console.exe OpenFFBoardPlugin.Tests\bin\Debug\OpenFFBoardPlugin.Tests.dll --test=OpenFFBoardPlugin.Tests.ProfileToCommandConverterTests
```

### Run via Visual Studio

Open **Test Explorer** (`Test → Test Explorer`) and click **Run All**.

### Test coverage

| File | What is tested |
|---|---|
| `JsonHandlerTests.cs` | File-not-found, valid JSON, camelCase mapping, nested objects, lists, null fields, extra fields, missing fields, empty object, null JSON literal |
| `ProfileHolderTests.cs` | Loading profiles from file, profile count, names, data field values, all FX/Axis command names, case-insensitive game matching, order preservation |
| `ProfileToCommandConverterTests.cs` | All 14 known command routes (6 FX + 8 Axis) return non-null lambdas, empty data → empty list, unknown class returns null, case sensitivity, value casting edge cases (0, 255, 256, -1, max ushort, ushort overflow), duplicate commands, instance field ignored |
| `DataPluginSettingsTests.cs` | Default field values, mutation, null/empty assignments, instance independence |
| `BoardTextTests.cs` | Name/DeviceIndex/IsEnabled get-set, `INotifyPropertyChanged` fires on `IsEnabled`, multiple subscribers, no-subscriber safety |

### Known limitation

Some error paths in `JsonHandler` and `ProfileToCommandConverter` call `SimHub.Logging.Current.Error()`. If `SimHub.Logging.Current` is null in the test process, those paths throw `NullReferenceException` instead of returning null. Affected tests are marked `[Description("...")]` and assert `Inconclusive` rather than failing hard.
