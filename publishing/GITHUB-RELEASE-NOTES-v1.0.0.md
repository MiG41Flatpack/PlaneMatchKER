# KER Rendezvous Tools v1.0.0

First combined stable release.

## Included KER tabs

- `WIND` — live target geometry, pass timing, target-plane launch opportunity,
  launch azimuth, and rising-horizon heuristic.
- `PLNE` — signed target-plane error and atmospheric manual flight-director
  readouts for shallow-ascent SSTO spaceplanes.

## Important migration

Remove the old standalone folders before installation:

```text
GameData\PlaneMatchKER
GameData\LaunchWindowKER
```

Then install the ZIP's `GameData` folder.

## Requirements

- KSP 1.12.5
- Kerbal Engineer Redux 1.1.9.5

## Scope

Display-only. No steering, throttle, staging, SAS, or time-warp control.

`WIND` advises launch geometry but does not guarantee terminal velocity
matching. `PLNE` advises atmospheric plane alignment but does not fly the
spaceplane.

## Files

- `KERRendezvousTools-v1.0.0.zip` — user installation
- `KERRendezvousTools-v1.0.0.zip.sha256` — checksum

License: GPL-3.0-or-later.
