# KER Rendezvous Tools 1.0.0 Stable Smoke Test

## Build and migration

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Build-And-Deploy.ps1 -MigrateLegacy
```

Expected log:

```text
[KERRendezvousTools] 1.0.0 registered WIND and PLNE sections in KER.
```

## Installation checks

- [ ] Only `GameData\KERRendezvousTools\Plugins\KERRendezvousTools.dll` is installed.
- [ ] `PlaneMatchKER.dll` and `LaunchWindowKER.dll` are absent.
- [ ] KSP loads without a KER Rendezvous Tools exception.

## Tab behavior

- [ ] No target: neither `WIND` nor `PLNE` is shown.
- [ ] Valid same-body orbiting vessel target: both tabs appear.
- [ ] Clearing the target hides both and closes floating copies.
- [ ] Re-selecting the same target restores both.
- [ ] KER settings reload or section changes do not create duplicate tabs.

## WIND

- [ ] Pad plane countdown agrees with MechJeb target-plane timing within about
      2 seconds when MechJeb is available.
- [ ] Rise and set occur near 0 degrees sea-level elevation.
- [ ] Forecast Reference changes from `CURRENT SURFACE SITE` to
      `LAUNCH SITE LOCKED` after liftoff.
- [ ] Live range and relative speed continue updating during ascent.
- [ ] A high-speed close flyby shows small range and large relative speed.
- [ ] 2.5 km proximity and margin change correctly.

## PLNE

- [ ] Relative inclination closely agrees with KER in fast flight.
- [ ] Plane Offset includes an explicit sign.
- [ ] Atmospheric Bank Cue is available with adequate dynamic pressure.
- [ ] Bank cue reads `N/A — VACUUM` outside aerodynamic flight.
- [ ] No flight-control or SAS behavior changes.

## Safety

- [ ] No pitch, roll, yaw, throttle, staging, SAS, or time-warp changes.
