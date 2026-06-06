# NINA SeeDither Plugin

*NINA 3.x plugin that performs absolute GoTo coordinate-offset dithering after exposures, designed for Seestar mounts whose guide pulses are unreliable.*

SeeDither adds a sequencer trigger that fires after a configurable number of exposures and performs a random dither via absolute mount slewing — no guider required. Plate scale is auto-detected from the connected Seestar camera name.

Please note this is in test phase and is just something I've been trying myself to see if I could improve Seestar dithering. It seems to do a good job but I'd like to see if it works for others.  You could just blip through fits images to see the dither movement (although that could also be center after drift). I have an in development plugin called SeeDrift which attempts to chart drift and dither etc and advise if walking noise is likely.

## Version

Current plugin version: `1.6.0.0`

## Features

- Absolute GoTo dithering — works without a guider by slewing to offset coordinates
- Auto-detects Seestar model (S30, S30 Pro, S50) from camera name to set plate scale
- Configurable min/max offset range in arcseconds (1–500) with live pixel equivalent display
- Configurable exposures-between-dither counter with inline progress (e.g. `3/5`)
- Input validation with red border and tooltips for out-of-range values
- Dithers from current mount position each time (not cached base coordinates)
- Uses NINA's mount-level settle time — no separate settle setting needed

## Requirements

- NINA 3.x (minimum application version `3.2.0.9001`)
- Seestar mount connected in NINA
- Seestar camera connected (for auto plate-scale detection)

## Install

Since SeeDither is not currently in the NINA plugin repository, install it manually:

1. Create this folder if it does not exist:
   - `%LOCALAPPDATA%\NINA\Plugins\3.0.0\SeeDither\`
2. Put `NINA.Plugin.SeeDither.dll` in that folder — either from a **GitHub Release** asset or copy it from `NINA.Plugin.SeeDither\bin\Release\net8.0-windows\` after a local Release build.
3. Restart NINA.

## Usage

1. Connect your Seestar mount and camera in NINA.
2. In plugin options, set your min/max offset range — changes are saved automatically.
3. In the Advanced Sequencer, add the **SeeDither After Exposures** trigger under a capture container.
4. Set the "After exposures" value to control dither frequency (e.g. `5` = dither every 5 exposures).
5. The trigger shows a progress counter (`1/5`, `2/5`, …) during the sequence.

## Settings

Settings are persisted automatically to:

- `%LOCALAPPDATA%\NINA\SeeDither\settings.json`

| Setting | Default | Description |
|---|---|---|
| Min Offset | 20 arcsec (~5 px) | Minimum random dither offset |
| Max Offset | 150 arcsec (~40 px) | Maximum random dither offset |
| Plate Scale | Auto-detected | S30/S30 Pro: 3.99"/px, S50: 2.39"/px |

## Notes

Example (from my own seedrift nina plugin) showing frames (circles) drifting, SeeDither (triangles) dithering in both RA and DEC, and center after drift (squares) recentering.

<img width="1116" height="1116" alt="image" src="https://github.com/user-attachments/assets/d7581e12-adc0-41f2-946d-7d77c5c74166" />

- Plugin identity GUID is stable and must not be changed after publish.
- Existing dependency warnings (for some transitive packages) may appear at build time, but Release builds succeed.
- The dither uses absolute mount slewing — NINA's mount-level settle time applies after each slew.

## Note

Hopefully ZWO can address what some of us have found appear to be dithering issues (e.g. not using both RA and DEC axes effectively). If you experience dithering problems in the Seestar app or in the NINA Seestar ASCOM Alpaca setup, it's always best to report them so ZWO is aware.

This plugin presents a basic, alternative way to get some RA and DEC dithering for Seestars, and is hopefully a temporary measure until ZWO can address it long-term.

## Support

If you use and like anything I've done, support on [Ko-fi](https://ko-fi.com/turnpike47298) is appreciated to encourage me to keep going!

## About

NINA plugin for absolute GoTo coordinate-offset dithering on Seestar mounts.

### Resources

[Readme](#readme-ov-file)

### License

[MPL-2.0 license](#MPL-2.0-1-ov-file)
