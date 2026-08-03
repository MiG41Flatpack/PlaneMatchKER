# KER Rendezvous Tools v1.0.2 Release Checklist

## Build and regression

- [ ] Close KSP.
- [ ] Run `scripts\Build-And-Deploy.ps1 -MigrateLegacy`.
- [ ] Restart KSP.
- [ ] Complete `TEST-CARD-1.0.2.md`.
- [ ] Confirm a FOUND result remains stable for at least 60 real seconds.
- [ ] Confirm the WIND panel height no longer flickers.
- [ ] Run `scripts\Package-Release.ps1`.

## Binary inspection

- [ ] `dist\KERRendezvousTools-v1.0.2.zip` exists.
- [ ] `GameData` is at the ZIP root.
- [ ] Exactly one plugin DLL is included.
- [ ] No KER DLLs, legacy DLLs, build outputs, or `.csproj.user` files appear.
- [ ] Verify the generated SHA-256 file.

## Publishing

- [ ] Tag `v1.0.2`.
- [ ] Paste `publishing\GITHUB-RELEASE-NOTES-v1.0.2.md`.
- [ ] Upload the binary ZIP and SHA-256 file to GitHub Releases.
- [ ] Upload the same binary ZIP to SpaceDock.
- [ ] Keep GPL-3.0-or-later and Kerbal Engineer Redux as the dependency.
