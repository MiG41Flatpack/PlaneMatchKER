# KER Rendezvous Tools 1.0.0

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
dist\KERRendezvousTools-v1.0.0.zip
dist\KERRendezvousTools-v1.0.0.zip.sha256
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
