# Migration to KER Rendezvous Tools 1.0.0

`PlaneMatchKER` and `LaunchWindowKER` are now distributed as one assembly.

## Remove

```text
GameData\PlaneMatchKER
GameData\LaunchWindowKER
```

Optional obsolete experimental folders:

```text
GameData\PlaneMatchGuidance
GameData\PlaneMatchDisplay
```

## Install

```text
GameData\KERRendezvousTools
```

Do not retain the old standalone DLLs. Each standalone mod has its own periodic
KER registration repair, so duplicate installations can repeatedly replace one
another's `PLNE` and `WIND` sections.

The combined DLL detects loaded legacy assemblies and refuses to register until
they are removed and KSP is restarted.

For a local source build:

```powershell
.\scripts\Build-And-Deploy.ps1 -MigrateLegacy
```
