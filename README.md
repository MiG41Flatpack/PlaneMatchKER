# PlaneMatchKER 1.0.2

PlaneMatchKER adds a compact `PLNE` tab to Kerbal Engineer Redux whenever a
valid same-body target vessel is selected.

It is a **display-only atmospheric lateral flight director**. It never commands
pitch, roll, yaw, throttle, SAS, staging, or any other flight control.

## Requirements

- Kerbal Space Program 1.12.5
- Kerbal Engineer Redux 1.1.9.5

AtmosphereAutopilot, MechJeb, Harmony, and ModuleManager are not required.

## Readouts

- Signed plane offset in `+/- km` or metres
- Signed plane angle
- Velocity normal to the target plane
- Predicted plane error after 20 seconds
- Linear local plane-crossing estimate
- Relative inclination
- Atmospheric manual bank cue
- Capture feasibility within a nominal 20-degree bank
- Required stopping bank
- Projected stopping offset
- Horizontal inertial speed

## Meaning of the signs

Positive plane offset means the active craft is on the side pointed to by the
target orbit's **directed orbital normal**. Negative means the opposite side.

These signs are orbital-normal and orbital-antinormal—not necessarily geographic
north and south.

`Plane Offset = 0` means the craft is crossing the target plane. It does not by
itself mean the craft's velocity is aligned with that plane. Relative
inclination and normal velocity show the remaining alignment error.

## Atmospheric cue scope

The bank cue is advisory and is shown only when:

- the craft is airborne;
- horizontal inertial speed is at least 25 m/s;
- dynamic pressure is at least 0.05 kPa;
- the current track gives useful bank-to-plane authority.

In vacuum it displays `N/A — VACUUM`; all orbital telemetry remains available.

## Installation

Copy the release ZIP's `GameData` folder into the KSP installation.

Remove older experimental folders if present:

```text
GameData\PlaneMatchGuidance
GameData\PlaneMatchDisplay
```

## Building

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Test-Environment.ps1
.\scripts\Build-And-Deploy.ps1
```

The build script passes the KSP path directly to MSBuild. No machine-specific
`.csproj.user` file is required.

## Packaging a binary release

```powershell
.\scripts\Package-Release.ps1
```

This creates:

```text
dist\PlaneMatchKER-v1.0.2.zip
```

with `GameData` at the ZIP root.

## Privacy and control safety

PlaneMatchKER:

- performs no network communication;
- collects or uploads no telemetry;
- does not access or change SAS;
- does not register fly-by-wire or autopilot callbacks;
- does not write `FlightCtrlState`.

## Compatibility

The supported and tested baseline is KSP 1.12.5 with KER 1.1.9.5. Older or
modified KER builds are not guaranteed.

## License

GPL-3.0-or-later. PlaneMatchKER directly links against Kerbal Engineer Redux,
which is distributed under GPL version 3 or later.

See `LICENSE` and `NOTICE.md`.
