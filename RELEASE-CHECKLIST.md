# PlaneMatchKER v1.0.2 Release Checklist

## Build

- [ ] Close KSP.
- [ ] Run `scripts\Test-Environment.ps1`.
- [ ] Run `scripts\Package-Release.ps1`.
- [ ] Confirm `dist\PlaneMatchKER-v1.0.2.zip` exists.
- [ ] Open the ZIP and confirm `GameData` is at the root.
- [ ] Confirm the ZIP contains no KER DLLs and no `.csproj.user`.

## Smoke test

- [ ] KSP 1.12.5 starts without a PlaneMatchKER exception.
- [ ] No target: no `PLNE` button.
- [ ] Valid same-body vessel target: `PLNE` button appears.
- [ ] Clearing target hides the tab and closes a floating section.
- [ ] Relative inclination agrees with KER within 0.05 degrees in fast flight.
- [ ] Plane Offset includes an explicit sign.
- [ ] `Atmospheric Bank Cue` shows `N/A — VACUUM` outside atmosphere.
- [ ] No flight control or SAS behavior changes.

## Distribution

- [ ] Publish the matching source archive.
- [ ] Mark the release license GPL-3.0-or-later, or GPLv3 if the host only
      offers that label.
- [ ] State the dependency: Kerbal Engineer Redux.
- [ ] Add repository/download URLs to `PlaneMatchKER.version` only after the
      public URLs exist.
- [ ] For CKAN, use dependency identifier `KerbalEngineerRedux`.
