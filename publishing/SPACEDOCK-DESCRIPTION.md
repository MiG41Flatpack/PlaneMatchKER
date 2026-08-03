# KER Rendezvous Tools

Two display-only rendezvous instruments integrated into Kerbal Engineer Redux.

## WIND — Launch Window

Shows live target range, range rate, relative speed, sea-level elevation and
azimuth, next rise/pass/set, the next launch-site crossing of the target orbital
plane, launch azimuth, and a rising-near-horizon phase heuristic.

The heuristic can identify favorable launch geometry, but it is not a complete
launch-to-rendezvous or velocity-matching solver.

## PLNE — Plane Match

Shows signed target-plane offset, plane angle, normal velocity, predicted
short-term plane error, relative inclination, and an atmospheric manual bank
cue. It is intended to reduce expensive post-insertion plane changes for
hand-flown shallow-ascent SSTO spaceplanes.

## Requirements

- Kerbal Space Program 1.12.5
- Kerbal Engineer Redux 1.1.9.5

## Migration

Remove old standalone installations first:

```text
GameData\PlaneMatchKER
GameData\LaunchWindowKER
```

## Safety

No steering, throttle, staging, SAS, or time-warp control. No network
communication or telemetry upload.

## License

GPL-3.0-or-later.

## Source code

REPLACE_WITH_GITHUB_REPOSITORY_URL
