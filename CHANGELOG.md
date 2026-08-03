# Changelog

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
