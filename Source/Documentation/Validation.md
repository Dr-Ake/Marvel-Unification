# Validation record

Validated on October 1, 2026 against the installed RimWorld 1.6.4871 rev591 game and Harmony. Testing used disposable native-game sessions, rather than mocked game assemblies. Original mod folders remained unchanged.

## Results

| Session | Result |
| --- | --- |
| Core + Biotech | Full character/power/system coverage; 260 successful checks. One duplicate-copy fixture assertion was subsequently corrected and retested. |
| Core + Biotech, focused duplicate retest | 38 checks passed, zero failures. |
| Core + all five DLCs | 264 checks passed, zero failures, suite completed. |
| Native save/load with all DLCs and focused kidnapping check | 15 checks passed, zero failures, suite completed. |
| Import/layout audit | All 198 original files unchanged; all imported files present; no modified textures/sounds, malformed XML, duplicate definitions, extra runtime DLLs, or validation code in the production assembly. |

The duplicate fixture now starts with one known weapon and compares the clone's effective skill to the source pawn's effective skill. Random starting equipment and backstory restrictions had invalidated its earlier fixed-value assertion. Failed fixture checks were investigated and rerun; passing gameplay checks were not repeatedly looped.

Final reports are under `Source/Documentation/Validation`. The earlier Biotech full report is retained alongside its successful focused retest so the resolved fixture assertion is visible. The all-DLC report covers the corrected duplication fixture and every listed power in one completed session.

## Gameplay coverage

| Character/system | Executed native-game checks |
| --- | --- |
| Cyclops | All four optic attacks, real target damage, strain costs/recovery, exhaustion gating, control visor, leadership aura. |
| Deadpool/Wolverine | Injury/disease/scar healing, missing-part regrowth, mood/trait effects, excluded conditions, healing-disable setting, corpse resurrection, insufficient chamber fuel, infusion job, hidden subject/restoration, fuel consumption, skeleton/claws, extend/retract and weapon restoration, hand wounds, protected limbs, kidnapping setting enabled/disabled. |
| Gambit | Card throw, 52-card pickup, deck consumption/recharge, touch charge, kinetic toggle/detonation, own-explosion immunity, native gene extraction, xenogerm implantation, recipient body/head preservation, removal of linked powers on gene removal. |
| Hulk | Transform/revert, body restoration, color set/reset, clap, boulder impact, gamma-leap flyer/landing, healing/regrowth, manual/forced timers, hostile-carry rage, emergency resurrection, corpse destruction/Green Door return, recovery coma. |
| Jean Grey/Phoenix | Telekinetic push, molecular deconstruction, shield absorption, death/resurrection and cooldown. |
| Magneto | Metallic stack grab, held item/orbit, controls, drop, nonmetal rejection, payload launch/impact, disarm. |
| Multiple Man | Registered clone creation, skill/equipment copying, recursive-clone rejection, dismiss/self-dismiss, expiry/drafted persistence, cap, XP permission, movement multiplier, death cleanup, native xenogerm implantation, archite extraction restriction, alternate injector mode. |
| Nightcrawler | Toggle, valid/invalid destination checks, real ordered movement job and teleport, restoring ordinary pathing. Native asynchronous pathfinding is allowed to finish between frames. |
| Storm | Six weather choices, summon/duration expiry, called lightning, tornado creation and scratch immunity, native lightning event immunity, configured wind deflection. |
| Items/recipes/assets | Nine native injector/ingestion workflows and consumption, duplicate checks where applicable, 20 recipe definitions/workers/products, six custom administration surgery workers, native graphics resolution. |
| Initialization/settings | Nine character identities and gizmos; all eight settings modules instantiated and rendered; 50 combined Harmony patches installed exactly once. |
| Persistence | All nine identities, health/abilities and stored resources; held metallic item, teleport toggle, weather state, Hulk color, registered clone. Optic strain comparison permits the normal recovery tick performed during native loading. |

Settings screens and native carrier portraits were visually inspected. Gameplay timings were shortened through temporary fixture settings or internal timers where needed; the original settings were restored. No balance changes were made to shorten production behavior.

## Protection and delivery

Every completed session restored the production DLL byte for byte and verified normal save/configuration hashes, including added/deleted files. All recorded protection checks passed. Sessions were capped at seven minutes inside the suite and ten minutes in the launcher, and exited once their checks finished.

The production assembly was rebuilt after comment-only housekeeping. Its SHA-256 remained identical to the gameplay-tested production build:

```text
7057bba7fea960b64dc211d6e5c3214cb0b4b0bba4cd1e60e409c519b78f0205
```

`Source/Validation` is reusable test source and is excluded from normal builds. Disposable test assemblies, test saves, game logs, compiler output, and inspection/import tools are removed from the delivery. Only the production DLL is installed under `Assemblies`.

## Boundaries

This is coverage of the listed functions and native-game scenarios, not exhaustive proof of every possible input or third-party mod combination. Long colony campaigns, conversion of a save originally using the nine separate DLLs, and automatic migration of their old settings were not tested.

RimForge is not installed on this machine. The existing Plasteel fallback and fuel workflow were tested; the optional RimForge-specific Adamantium branch was not exercised with RimForge loaded. Its original integration code is retained. Multiple Man's archite gene is intentionally not extractable through the vanilla extractor; implantation was tested with a native genepack/xenogerm.
