# Changelog

## 1.0.2

- Fixed `WIND` GOOD-result rows flickering between `SEARCHING` and `FOUND`.
- Replaced overly sensitive per-frame search invalidation with material-change
  thresholds for target period, target plane, and launch-site direction.
- Compared forecast reference sites geometrically at the same universal time,
  avoiding false resets caused by equivalent rotating-frame epochs.
- Kept all GOOD and best-rising rows permanently present, using dashes while a
  value is unavailable.
- Prevented repeated KER section-height changes during long-horizon searches.
- Preserved the 8,192-window incremental search and all display-only safety
  boundaries.

## 1.0.1

- Added an incremental long-horizon search for the first strict `GOOD` launch
  opportunity.
- Added `Next GOOD @ Ref`.
- Added target-orbit and body-rotation counts to the GOOD opportunity.
- Added one-based future plane-window numbering.
- Added GOOD branch, launch azimuth, and target elevation.
- Added an 8,192-plane-window safety limit.
- Added conservative full-geometry recurrence detection.
- Added a best-rising fallback when no strict GOOD has yet been found.
- Changed long countdowns to absolute UT hours and added body-rotation counts to avoid 24-hour-day ambiguity.
- Replaced the WIND calculation log prefix with the combined-suite name.
- Preserved display-only behavior and all previously tested calculations.

## 1.0.0

- Combined PlaneMatchKER 1.0.2 and LaunchWindowKER 1.0.0 into one DLL.
- Added one shared KER startup injector and integrity-repair loop.
- Preserved two separate KER tabs: `WIND` and `PLNE`.
- Preserved the field-tested orbital and atmospheric calculations.
- Added automatic cleanup of stale serialized sections and old readout names.
- Added detection of loaded standalone legacy assemblies.
- Added a migration script and guarded local deployment.
- Added one KSP-AVC version file, one release package, and one CKAN template.
- Added SpaceDock and GitHub publishing text.
- Added SHA-256 generation for the public binary ZIP.
- No flight-control, staging, SAS, throttle, or time-warp functionality.

## Module lineage

### PlaneMatchKER 1.0.2

- Stable atmospheric target-plane flight director.
- Field-tested in heavily modded career-mode SSTO station flights.
- Typical observed cleanup plane changes: approximately 15–30 m/s.

### LaunchWindowKER 1.0.0

- Stable generic target-pass and target-plane launch advisory.
- Field-tested with Proton- and Delta IV-style launchers.
- Produced useful close spatial intercept geometry without claiming terminal
  velocity matching.
