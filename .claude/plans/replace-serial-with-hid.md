# Plan: Replace Serial Communication with HID

## Context
The plugin currently supports two communication modes — Serial (`OpenFFBoard.Serial`) and HID (`OpenFFBoard.Hid`). The decision is made at runtime in `ConnectToBoard()` based on whether a COM port string is provided. The goal is to remove the serial path entirely so the plugin always uses HID communication.

---

## Files to Modify

### 1. `DataPlugin.cs`
**Changes:**
- Remove `public string[] Boards = null` field (was for serial COM port list)
- Simplify `ConnectToBoard()`: remove the `comPort`/`baudRate` parameters and the serial branch. Always connect using `BoardsHid[index]`. New signature: `ConnectToBoard(int hidDeviceIndex = 0)`
- Update `Init()` to call `ConnectToBoard()` without serial args (no longer pass `Settings.ConnectTo` / `Settings.BaudRate`)
- Remove the commented-out `//Boards = global::OpenFFBoard.Serial.GetBoards();` line

### 2. `DataPluginSettings.cs`
**Changes:**
- Remove `BaudRate` field (serial-only)
- Remove `ConnectTo` field (was the COM port string)
- Add `SelectedHidDeviceId = null` (string) to persist which HID device was last selected, so it can be pre-selected in the UI on next launch

### 3. `SettingsControl.xaml.cs`
**Changes:**
- `ViewBoards_Loaded`: iterate `Plugin.BoardsHid` instead of `Plugin.Boards`; populate `BoardText` with each device's `DeviceId` string; pre-select based on `Plugin.Settings.SelectedHidDeviceId`
- Update `BoardText` helper class: the `Name` property now holds the HID `DeviceId` string (display-friendly)
- `ViewSelectedCom_Connect`: call `Plugin.ConnectToBoard(selectedIndex)` using the index of the selected HID device instead of passing a COM port name
- `UpdateConnectedTo`: update `Plugin.Settings.SelectedHidDeviceId` instead of `Plugin.Settings.ConnectTo`

### 4. `SettingsControl.xaml`
**Changes:**
- Change `"COM Selected: "` label text → `"Device: "`

### 5. `Utils/ProfileToSerialConverter.cs`
**Changes:**
- Rename class from `ProfileToSerialConverter` → `ProfileToCommandConverter`
- Update the reference in `DataPlugin.cs` (line 192: `ProfileToSerialConverter.ConvertProfileToCommands`)
- No logic changes needed — the converter operates on `OpenFFBoard.Board` which is already protocol-agnostic

---

## Key Reused Code
- `OpenFFBoard.Hid.GetBoardsAsync()` — already used in `Init()` at line 215, no change needed
- `OpenFFBoard.Hid(BoardsHid[0])` — already the HID path in `ConnectToBoard()`, just becomes the only path
- `ProfileToCommandConverter.ConvertProfileToCommands()` — same logic, just renamed

---

## Verification
1. Build: `msbuild OpenFFBoardPlugin.sln /p:Configuration=Debug`
2. Confirm no compile errors related to removed serial fields or renamed class
3. Launch in SimHub, open plugin settings — board list should show HID devices (not COM ports)
4. Connect to a board, confirm it uses the HID path without errors
5. Trigger profile update (`SetProfileToCurrentGame`) and confirm commands execute via HID
