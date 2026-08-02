# Changelog

## 1.0.2 — stable polish

- Switched processor caching from universal time to Unity frame count so target
  changes update while paused.
- Reject landed, splashed, and prelaunch target vessels.
- Suppress atmospheric bank and stopping cues in vacuum and at very low speed.
- Renamed `Plane Crossing ETA` to `Linear Plane ETA`.
- Assigned the readout a unique internal name: `PlaneMatchKER.Panel`.
- Hardened cleanup of stale floating/editor sections.
- Added periodic repair if KER reloads or replaces its runtime collections.
- Centralized mod version and section identifiers.
- Removed the committed machine-specific `.csproj.user`.
- Made build scripts portable by passing `KSPBT_GameRoot` directly.
- Added a one-command Release-mode binary packager.
- Added SPDX copyright headers and release documentation.
- No orbital geometry or flight-control functionality was added.

## 1.0.1

- Corrected the KER `ISectionModule` namespace in the readout override.

## 1.0.0

- Initial KER `PLNE` tab implementation.
