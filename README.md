# Marvel Unification

RimWorld 1.6 collection combining the nine original Marvel mods in one assembly: `Assemblies/MarvelUnification.dll`.

Includes Cyclops, Deadpool and Wolverine, Gambit, Hulk, Jean Grey/Phoenix, Magneto, Multiple Man, Nightcrawler, and Storm. Powers, costs, recipes, assets, definition names, namespaces, and the eight original settings panels are retained.

## Installation

1. Place this folder in RimWorld's `Mods` directory.
2. Enable Harmony, Biotech, and Marvel Unification, in that order.
3. Disable the nine individual Marvel mods listed in `About/About.xml`. Loading them together would duplicate definitions and patches.

The compiled mod does not require the original folders. Royalty, Ideology, Anomaly, and Odyssey are optional. Biotech is required by the existing genetics systems. Nightcrawler retains its in-game toggle; the other modules retain their settings panels.

## Scope of the merge

- All character code compiles into one DLL. A single Harmony owner installs the combined patches once.
- Textures, sounds, and translations share the root `Textures`, `Sounds`, and `Languages` folders. Original asset paths remain valid, and textures and sound files are unchanged.
- Compatible injectors share recipient and duplicate-power checks. Single-use injectors consume themselves once, using the existing destroy-self component.
- Deadpool's existing kidnapping setting now checks the chosen victim. Its original patch checked the carrier.
- No new powers, balance pass, broad code cleanup, or general gameplay redesign is included.

Gambit's existing Biotech gene and Multiple Man's gene/injector options remain intact. Multiple Man's archite gene retains the game's extraction restriction. Hediff-based powers have not been converted into new genes.

## Source and build

`Source/Modules` keeps each original module recognizable. `Source/Shared` contains the small shared merge helpers. `Source/Documentation/Import-Manifest.json` records source paths and original file hashes.

Build from the mod folder with PowerShell:

```powershell
& .\Source\Build.ps1 -CompilerPath 'C:\path\to\current\Roslyn\csc.exe'
```

The script detects a local Visual Studio 2022 compiler when available. On the original development installation it can also use the existing Multiple Man compiler package. A recipient can supply their own current Roslyn compiler; the compiler package is not needed to play the mod.

For a different game or Harmony location:

```powershell
& .\Source\Build.ps1 -CompilerPath 'C:\path\to\csc.exe' -RimWorldDir 'D:\Games\RimWorld' -HarmonyPath 'D:\Mods\Harmony\Current\Assemblies\0Harmony.dll'
```

Alternatively, build `Source/MarvelUnification.csproj` with a .NET SDK and .NET Framework 4.8 reference assemblies, supplying `RimWorldDir` and `HarmonyPath` MSBuild properties as needed. The production build includes only modules, shared helpers, and assembly metadata.

## Validation

See `Source/Documentation/Validation.md` for tested behavior and limits, and `Source/Validation` for the bounded native-game suite. Test helpers are excluded from the production DLL. Tests use a separate save-data folder, restore the production DLL, and compare normal saves and configuration before and after the session.

To run a test session, close RimWorld first:

```powershell
& .\Source\Validation\Run.ps1 -Profile biotech
& .\Source\Validation\Run.ps1 -Profile all-dlc
& .\Source\Validation\Run.ps1 -Profile reload
python .\Source\Validation\Audit.py
```

The reload profile uses the all-DLC test save. Live testing additionally requires the installed RimBridge server (`brrainz.rimbridgeserver`); it is not a dependency for normal gameplay. Each session has a seven-minute suite bound and a ten-minute launcher bound. Generated files go into `Source/obj`.

Back up an existing colony before switching from the separate mods. Save/reload with the unified mod is validated; converting a colony originally saved with the separate DLLs and automatically importing their settings are not validated.

## Notices

See `NOTICE.md` and the retained `LICENSE` from the Deadpool source mod.
