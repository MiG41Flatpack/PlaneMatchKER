# PlaneMatchKER 1.0.2 Stable Smoke Test

1. Remove `GameData\PlaneMatchGuidance` and `GameData\PlaneMatchDisplay`.
2. Build and deploy PlaneMatchKER.
3. Restart KSP.
4. With no target, verify no `PLNE` tab is shown.
5. Select an orbiting same-body vessel and verify the `PLNE` tab appears.
6. Confirm Plane Offset shows an explicit `+` or `-`.
7. Compare PlaneMatchKER and KER relative inclination in fast flight.
8. Pause the game, clear or change the target, and verify the tab responds.
9. Reach vacuum and verify:
   - orbital telemetry remains;
   - Atmospheric Bank Cue reads `N/A — VACUUM`;
   - stopping-bank fields are not shown.
10. Float the PLNE section, clear the target, and confirm the window closes.
11. Confirm no pitch, roll, yaw, throttle, or SAS behavior changes.

Expected log:

```text
[PlaneMatchKER] 1.0.2 registered PLNE section in KER.
```
