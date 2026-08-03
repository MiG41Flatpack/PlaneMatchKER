# KER Rendezvous Tools 1.0.2 Stable Smoke Test

## Build

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\scripts\Build-And-Deploy.ps1 -MigrateLegacy
```

Expected log:

```text
[KERRendezvousTools] 1.0.2 registered WIND and PLNE sections in KER.
```

## GOOD-display regression

Use the same launch site and target that previously produced a GOOD result near
window 68.

- [ ] `GOOD Search Windows` advances from SEARCHING to FOUND.
- [ ] After FOUND, leave the vessel on the pad for at least 60 real seconds.
- [ ] `Next GOOD @ Ref` does not alternate back to SEARCHING.
- [ ] `Target Orbits to GOOD` remains continuously visible.
- [ ] `Body Rotations to GOOD` remains continuously visible.
- [ ] `GOOD Window #` remains continuously visible.
- [ ] `GOOD Branch / Azimuth` remains continuously visible.
- [ ] `GOOD Elev @ Plane` remains continuously visible.
- [ ] KER panel height does not pulse or resize.
- [ ] Best-rising rows also remain present and do not change panel height.
- [ ] Ordinary countdown values continue updating.

## Legitimate invalidation

- [ ] Changing to a different target restarts the search.
- [ ] Moving to a materially different surface site restarts the search.
- [ ] A meaningful target maneuver or plane change restarts the search.
- [ ] Time-warping past the stored GOOD restarts from the new current time.
- [ ] Tiny landed-vessel jitter does not restart the search.

## Existing regressions

- [ ] `WIND` and `PLNE` appear for a valid same-body orbiting target.
- [ ] Forecast Reference locks to the launch site after liftoff.
- [ ] Live target range and relative speed continue updating.
- [ ] PLNE retains atmospheric/vacuum cue behavior.
- [ ] No pitch, roll, yaw, throttle, staging, SAS, or time-warp changes.
