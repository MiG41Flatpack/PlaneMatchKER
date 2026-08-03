# KER Rendezvous Tools v1.0.0 Release Checklist

## Local build

- [ ] Close KSP.
- [ ] Run `scripts\Build-And-Deploy.ps1 -MigrateLegacy`.
- [ ] Restart KSP.
- [ ] Complete `TEST-CARD-1.0.0.md`.
- [ ] Run `scripts\Package-Release.ps1`.

## Binary inspection

- [ ] `dist\KERRendezvousTools-v1.0.0.zip` exists.
- [ ] `GameData` is at the ZIP root.
- [ ] The ZIP contains exactly one plugin DLL:
      `KERRendezvousTools.dll`.
- [ ] No KER DLLs, old standalone DLLs, build outputs, or `.csproj.user` files
      are included.
- [ ] Verify the generated SHA-256 file.

## GitHub

- [ ] Create the public repository and push this source tree.
- [ ] Replace author/repository placeholders in `metadata`.
- [ ] Commit and tag `v1.0.0`.
- [ ] Create a GitHub release from the tag.
- [ ] Paste `publishing\GITHUB-RELEASE-NOTES-v1.0.0.md`.
- [ ] Upload the binary ZIP and SHA-256 file.

## SpaceDock

- [ ] Create the mod as `KER Rendezvous Tools`.
- [ ] Use game version KSP 1.12.5.
- [ ] Select GPLv3 / GPL-3.0-or-later.
- [ ] Paste `publishing\SPACEDOCK-DESCRIPTION.md`.
- [ ] Upload the same binary ZIP as GitHub.
- [ ] Set the GitHub repository as the source-code link.
- [ ] Declare Kerbal Engineer Redux as required in the description.

## Optional CKAN

- [ ] Replace placeholders in `metadata\KERRendezvousTools.netkan.template`.
- [ ] Use dependency identifier `KerbalEngineerRedux`.
- [ ] Submit or request indexing only after the public download URL exists.
