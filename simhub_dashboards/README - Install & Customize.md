# OpenFFBoard Companion - Input Display

A SimHub input-display overlay inspired by Romainrob's "Input Display - Modular" layout, bundled with the OpenFFBoard companion plugin.
Works with iRacing, ACC, AC, and any other SimHub-supported game (uses generic telemetry properties).

## What's on it (left to right)

- **Input trace graph** — scrolling history of throttle (green), brake (red), clutch (blue), and a faint white steering trace, with 25/50/75% guide lines
- **Pedal bars** — live Clutch / Brake / Throttle bars with % values
- **Gear, speed, RPM** — big gear digit, speed in your local unit (km/h or mph), rpm, plus a 6-LED shift light strip
- **ABS / TC / BB** — ABS lights red and TC lights orange when active; BB shows brake bias (games that report it)
- **Steering wheel** — rotating wheel indicator

## Install

**Option A (easiest):** double-click `OpenFFBoard Companion - Input Display.simhubdash` — SimHub imports it automatically.

**Option B (manual):** copy the folder `OpenFFBoard Companion - Input Display` into your SimHub install folder under `DashTemplates`, e.g. `C:\Program Files (x86)\SimHub\DashTemplates\`. Restart SimHub.

## Use as an on-screen overlay

1. In SimHub, go to **Dash Studio → Overlays → New overlay**
2. Pick **OpenFFBoard Companion - Input Display**, position it on screen, and enable it
3. It only shows while a game is running

## Configure via the plugin (no dashboard editing needed)

With the OpenFFBoard companion plugin installed, open **OpenFFBoard companion → Dashboard Extras** in SimHub's left menu. Everything below is published as SimHub properties (`OpenFFBoardPlugin.InputDisplay.*`) that the dashboard reads live:

| Setting | Property | Default |
|---|---|---|
| Steering rotation (°, lock-to-lock) | `InputDisplay.SteeringRotationDegrees` | 480 |
| Background opacity (%) | `InputDisplay.BackgroundOpacity` | 70 |
| Shift light threshold (% of max RPM) | `InputDisplay.ShiftLightThresholdPercent` | 85 |
| Show/hide Traces, Pedals, Gear & Speed, ABS/TC/BB, Steering | `InputDisplay.Show*` | all on |

If the plugin isn't installed, the dashboard falls back to the defaults above — it still works standalone.

## Notes & tweaks

- Steering rotation from real wheel angle (iRacing) is used as-is; on games that only report normalized steering (AC/ACC), the configured rotation range is applied (±range/2).
- The shift LED strip lights progressively from the configured threshold up to max RPM; the last LED is red.
- Deeper edits (colors, layout, trace speed via `PointsCount`) are still done in **Dash Studio → Dashboards → Edit dashboard** — every element is grouped (Traces, Pedals, Gear and Speed, Extras, Steering).
- On iRacing, ABS/TC boxes only light for cars that have those assists; BB shows `--` when the game doesn't report brake bias.
