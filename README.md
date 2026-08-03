# KER Rendezvous Tools 1.0.2

A single Kerbal Engineer Redux companion plugin containing two field-tested
rendezvous advisory instruments:

- **`WIND` — Launch Window:** live target geometry, sea-level pass prediction,
  target-plane launch timing, launch azimuth, and a rising-horizon heuristic.
- **`PLNE` — Plane Match:** signed target-plane offset, relative inclination,
  normal velocity, short-term plane prediction, and an atmospheric manual bank
  cue for SSTO ascent.

Both modules are display-only. They never steer, throttle, stage, control SAS,
or change time warp.

## Requirements

- Kerbal Space Program 1.12.5
- Kerbal Engineer Redux 1.1.9.5

MechJeb, AtmosphereAutopilot, ModuleManager, Harmony, and Kerbalism are optional
and are not dependencies.

## Installation

The public binary ZIP installs as:

```text
GameData/
└── KERRendezvousTools/
    ├── Plugins/
    │   └── KERRendezvousTools.dll
    ├── KERRendezvousTools.version
    ├── README.md
    ├── CHANGELOG.md
    ├── MIGRATION.md
    ├── LICENSE
    └── NOTICE.md
```

### Required migration from the standalone builds

Delete these old folders before installing the combined suite:

```text
GameData\PlaneMatchKER
GameData\LaunchWindowKER
```

The combined DLL deliberately refuses to register its KER tabs if either legacy
standalone assembly is loaded, preventing two periodic injectors from fighting
over `WIND` and `PLNE`.

For a local development install:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Build-And-Deploy.ps1 -MigrateLegacy
```

## Build

The build project targets .NET Framework 4.7.2 and uses KSPBuildTools 1.1.1.

```powershell
.\scripts\Test-Environment.ps1
.\scripts\Build-And-Deploy.ps1 -MigrateLegacy
```

The KSP root defaults to:

```text
C:\Kerbal Space Program\1.12.5
```

Override it with `-KspRoot`.

## Make the public binary ZIP

```powershell
.\scripts\Package-Release.ps1
```

Output:

```text
dist\KERRendezvousTools-v1.0.2.zip
dist\KERRendezvousTools-v1.0.2.zip.sha256
```

Upload the same binary ZIP to GitHub Releases and SpaceDock.

## `WIND` scope

`WIND` provides generic geometry and launch-opportunity timing. It does not have
a vehicle ascent profile and does not promise a synchronized insertion and
velocity match.

Informal test flights produced close spatial intercepts with different launcher
families, including approximately 2.1 km with high relative velocity. The
separate `Relative Speed` row exists so a close flyby cannot be mistaken for a
dockable rendezvous.

After liftoff, forecast rows remain tied to the launch site while live range,
range rate, relative speed, elevation, and azimuth follow the active vessel.

## Long-horizon `GOOD` search

`WIND` now searches future launch-site target-plane crossings for the first
strict heuristic opportunity:

```text
target rising
and
absolute target elevation at the plane crossing <= 5 degrees
```

The search is incremental, evaluating 32 future plane windows per rendered
frame, so long waits do not freeze the game.

It reports:

```text
Next GOOD @ Ref
Target Orbits to GOOD
Body Rotations to GOOD
GOOD Window #
GOOD Branch / Azimuth
GOOD Elev @ Plane
```

The search examines at most 8,192 future plane windows. This is intentionally
large enough to cover the 40–100 Kerbin-day waits observed during development.

Countdowns are formatted as absolute universal-time hours rather than assuming
a 24-hour day. `Body Rotations to GOOD` is the local-body-day equivalent; on
stock Kerbin, one rotation is one six-hour Kerbin day.

A conservative full-geometry recurrence check can stop the search early when
the launch-site direction and target orbital state repeat. If no strict GOOD
has been found, `WIND` retains the closest rising candidate discovered so far:

```text
Best Rising @ Ref
Best Rising Elev
Best Branch / Azimuth
```

`SEARCH LIMIT REACHED` does not mean that no later GOOD can ever exist. It means
that none was found inside the deliberately bounded 8,192-window search.

## v1.0.2 GOOD-display stability fix

The long-horizon search now ignores ordinary per-frame floating-point jitter in
the target orbit and rotating launch-site transform. It restarts only for a
material change in:

- active vessel or selected target;
- celestial body;
- target orbital period;
- target orbital plane;
- forecast reference site;
- or when the stored GOOD opportunity has passed.

The `WIND` panel also uses a fixed row layout. GOOD values show dashes while
searching rather than removing the rows, so KER no longer repeatedly resizes the
section.

## `PLNE` scope

`PLNE` is an atmospheric flight director for manually flown shallow-ascent SSTO
spaceplanes. It helps reduce expensive post-insertion plane changes. The bank
cue is inhibited in vacuum and at inadequate dynamic pressure; orbital telemetry
remains available.

## Safety and privacy

The suite:

- performs no network communication;
- collects or uploads no telemetry;
- does not write `FlightCtrlState`;
- registers no fly-by-wire or autopilot callback;
- does not control SAS, staging, throttle, or time warp.

## License

GPL-3.0-or-later. This suite directly links against Kerbal Engineer Redux, whose
source is distributed under GNU GPL version 3 or, at your option, any later
version.

See `LICENSE` and `NOTICE.md`.
