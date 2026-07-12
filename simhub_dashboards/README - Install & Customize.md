# OpenFFBoard Companion - Input Display

A SimHub input-display overlay inspired by Romainrob's "Input Display - Modular" layout, bundled with the OpenFFBoard companion plugin.
Works with iRacing, ACC, AC, and any other SimHub-supported game (uses generic telemetry properties).

## What's on it (left to right)

- **Input trace graph** — scrolling history of throttle (green), brake (red), clutch (blue), and a faint white steering trace, with 25/50/75% guide lines
- **Pedal bars** — live Clutch / Brake / Throttle bars with % values
- **Gear, speed, RPM** — big gear digit, speed in your local unit (km/h or mph), rpm, plus a 6-LED shift light strip
- **ABS / TC / BB / CLIP** — ABS blinks red when active and a yellow stripe appears under the brake trace exactly where ABS triggered; TC blinks magenta with a magenta stripe under the throttle trace where it intervened; BB shows brake bias (games that report it); CLIP blinks orange when the game's own force-feedback signal is pegged at its max (AC/ACC/iRacing)
- **Steering wheel** — rotating wheel indicator

## Install

**Option A (easiest):** double-click `OpenFFBoard Companion - Input Display.simhubdash` — SimHub imports it automatically.

**Option B (manual):** copy the folder `OpenFFBoard Companion - Input Display` into your SimHub install folder under `DashTemplates`, e.g. `C:\Program Files (x86)\SimHub\DashTemplates\`. Restart SimHub.

## Use as an on-screen overlay

1. In SimHub, go to **Dash Studio → Overlays → New overlay**
2. Pick **OpenFFBoard Companion - Input Display**, position it on screen, and enable it
3. It only shows while a game is running

## Configure via the plugin (no dashboard editing needed)

With the OpenFFBoard companion plugin installed, open **OpenFFBoard companion → Extra configurations → Dashboard Extras** in SimHub's left menu. Everything below is published as SimHub properties (`OpenFFBoardDataPlugin.InputDisplay.*`) that the dashboard reads live:

| Setting | Property | Default |
|---|---|---|
| Steering rotation (°, lock-to-lock) | `InputDisplay.SteeringRotationDegrees` | 480 |
| Background opacity (%) | `InputDisplay.BackgroundOpacity` | 70 |
| Shift light threshold (% of max RPM) | `InputDisplay.ShiftLightThresholdPercent` | 85 |
| Show/hide Traces, Pedals, Gear & Speed, ABS/TC/BB, Steering, FFB Clipping | `InputDisplay.Show*` | all on |
| Steering wheel graphic (Classic / GT / GT3) | `InputDisplay.WheelImage` | Classic |
| Corner minimum speed (read-only, computed by the plugin) | `InputDisplay.CornerMinSpeedKmh`, `InputDisplay.LastCornerMinSpeedKmh` | — |

If the plugin isn't installed, the dashboard falls back to the defaults above — it still works standalone.

## Notes & tweaks

- Steering rotation from real wheel angle (iRacing) is used as-is; on games that only report normalized steering (AC/ACC), the configured rotation range is applied (±range/2).
- The shift LED strip lights progressively from the configured threshold up to max RPM; the last LED is red.
- Deeper edits (colors, layout, trace speed via `PointsCount`) are still done in **Dash Studio → Dashboards → Edit dashboard** — every element is grouped (Traces, Pedals, Gear and Speed, Extras, Steering).
- On iRacing, ABS/TC boxes only light for cars that have those assists; BB shows `--` when the game doesn't report brake bias.
- FFB clipping is read straight from the game's own telemetry, not the board: `Physics.FinalFF` on AC/ACC (the actual signal sent to the wheel, clips at ±1) or `SteeringWheelPctTorque` on iRacing, both treated as clipping at ~98% of max. Games that report neither just never show CLIP.
- The "MIN" readout shows the last corner's minimum speed for ~4 seconds after you accelerate out of it, then the label switches to "SPD" and it falls back to showing live speed until the next corner.
