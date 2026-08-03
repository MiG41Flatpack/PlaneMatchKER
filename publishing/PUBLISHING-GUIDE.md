# Publishing guide

## 1. GitHub repository

Create a repository named `KERRendezvousTools` or another preferred name, then upload or
push the complete source tree.

Replace these placeholders before tagging:

- `REPLACE_WITH_RELEASE_AUTHOR`
- `REPLACE_OWNER`
- `REPLACE_REPOSITORY`
- `REPLACE_WITH_GITHUB_REPOSITORY_URL`

Do not commit a real `.csproj.user` file or local KSP paths.

## 2. Build the user package

```powershell
.\scripts\Package-Release.ps1
```

Use the generated binary ZIP as the release asset. GitHub automatically offers
source archives for tagged releases, while the explicit ZIP is the installable
KSP package.

## 3. GitHub release

Create tag:

```text
v1.0.2
```

Paste:

```text
publishing\GITHUB-RELEASE-NOTES-v1.0.2.md
```

Upload:

```text
dist\KERRendezvousTools-v1.0.2.zip
dist\KERRendezvousTools-v1.0.2.zip.sha256
```

## 4. SpaceDock

Create a KSP mod with:

- Name: `KER Rendezvous Tools`
- Version: `1.0.2`
- KSP version: `1.12.5`
- License: `GPLv3` or `GPL-3.0-or-later`
- ZIP: the same binary ZIP uploaded to GitHub
- Source-code link: the GitHub repository

Paste `publishing\SPACEDOCK-DESCRIPTION.md`.

## 5. CKAN

After the public SpaceDock or GitHub download exists, replace placeholders in:

```text
metadata\KERRendezvousTools.netkan.template
```

The required dependency identifier is `KerbalEngineerRedux`.
