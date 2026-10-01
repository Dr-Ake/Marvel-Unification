using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MagnetoXGene
{
    public class HediffCompProperties_Magnetism : HediffCompProperties
    {
        public float launchCooldownSeconds = 4f;

        public HediffCompProperties_Magnetism()
        {
            compClass = typeof(HediffComp_Magnetism);
        }
    }

    public class HediffComp_Magnetism : HediffComp, IThingHolder
    {
        private int nextLaunchTick;
        private ThingOwner<Thing> orbitingItems;
        private Dictionary<Thing, OrbitingMagnetThing> orbitingVisuals = new Dictionary<Thing, OrbitingMagnetThing>();
        private bool visualsDirty;
        private bool deathDropHandled;

        public HediffCompProperties_Magnetism Props => (HediffCompProperties_Magnetism)props;
        private Map Map => Pawn?.Map;
        private float BaseOrbitRadius => 0.8f + 0.12f * Mathf.Min(orbitingItems?.Count ?? 0, 8);
        private float CurrentOrbitRadius => BaseOrbitRadius * MagnetoMod.ActiveSettings.orbitDistanceMultiplier;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref nextLaunchTick, "magnetoNextLaunchTick", 0);
            Scribe_Values.Look(ref deathDropHandled, "magnetoDeathDropHandled", false);
            Scribe_Deep.Look(ref orbitingItems, "magnetoOrbitingItems", new object[] { this });

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                orbitingItems ??= new ThingOwner<Thing>(this, false);
                orbitingVisuals = new Dictionary<Thing, OrbitingMagnetThing>();
                visualsDirty = true;
            }
        }

        public override void CompPostMake()
        {
            base.CompPostMake();
            orbitingItems = new ThingOwner<Thing>(this, false);
            orbitingVisuals = new Dictionary<Thing, OrbitingMagnetThing>();
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            DropAllOrbiting();
            ClearVisuals();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            Pawn pawn = Pawn;

            if (visualsDirty && pawn?.Spawned == true && Map != null)
            {
                RebuildOrbitVisuals();
                visualsDirty = false;
            }

            if (pawn == null)
            {
                return;
            }

            if (pawn.Dead)
            {
                if (!deathDropHandled)
                {
                    deathDropHandled = TryDropOrbitingForDeadPawn();
                }
                return;
            }
            deathDropHandled = false;

            if (pawn?.Spawned != true)
            {
                return;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            Pawn pawn = Pawn;
            if (pawn == null || pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            IEnumerable<Gizmo> baseGizmos = base.CompGetGizmos();
            if (baseGizmos != null)
            {
                foreach (Gizmo gizmo in baseGizmos)
                {
                    yield return gizmo;
                }
            }

            float radius = MagnetoMod.ActiveSettings.magneticRadius;
            Texture2D grabIcon = ContentFinder<Texture2D>.Get("UI/Commands/CommandMagnetoGrab", true);
            Command_Action grab = new Command_Action
            {
                defaultLabel = "Magneto_Gizmo_MagneticGrabLabel".Translate(),
                defaultDesc = "Magneto_Gizmo_MagneticGrabDesc".Translate(radius.ToString("F0"), MagnetoMod.ActiveSettings.maxOrbitingItems),
                icon = grabIcon ?? BaseContent.BadTex
            };
            if (!pawn.Spawned || pawn.Downed || pawn.Dead)
            {
                grab.Disable("Magneto_Gizmo_PawnUnavailable".Translate());
            }
            else
            {
                grab.action = () =>
                {
                    TargetingParameters targetingParams = new TargetingParameters
                    {
                        canTargetItems = true,
                        canTargetPawns = true,
                        canTargetBuildings = false,
                        mapObjectTargetsMustBeAutoAttackable = false,
                        canTargetSelf = false,
                        validator = target =>
                        {
                            if (!target.HasThing)
                            {
                                return false;
                            }

                            if (target.Thing is Pawn pawnTarget)
                            {
                                return IsValidPawnTarget(pawnTarget);
                            }

                            return IsValidGrabTarget(target.Thing);
                        }
                    };

                    Find.Targeter.BeginTargeting(
                        targetingParams,
                        chosen =>
                        {
                            if (!chosen.HasThing)
                            {
                                return;
                            }

                            if (chosen.Thing is Pawn pawnTarget)
                            {
                                TryGrabFromPawn(pawnTarget);
                            }
                            else
                            {
                                TryGrabThing(chosen.Thing);
                            }
                        },
                        null,
                        null,
                        pawn,
                        null,
                        null,
                        false,
                        _ => GenDraw.DrawRadiusRing(pawn.Position, radius),
                        _ => GenDraw.DrawRadiusRing(pawn.Position, radius));
                };
            }
            yield return grab;

            orbitingItems ??= new ThingOwner<Thing>(this, false);

            if (orbitingItems.Count == 0)
            {
                yield break;
            }

            Texture2D launchIcon = ContentFinder<Texture2D>.Get("UI/Commands/CommandMagnetoLaunch", true);
            Command_Target launch = new Command_Target
            {
                defaultLabel = "Magneto_Gizmo_LaunchLabel".Translate(),
                defaultDesc = "Magneto_Gizmo_LaunchDesc".Translate(),
                icon = launchIcon ?? BaseContent.BadTex,
                targetingParams = new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = true,
                    canTargetBuildings = true,
                    mapObjectTargetsMustBeAutoAttackable = false,
                    canTargetSelf = false
                }
            };
            launch.action = target =>
            {
                if (target == null || !target.IsValid)
                {
                    return;
                }

                if (Find.TickManager.TicksGame < nextLaunchTick)
                {
                    Messages.Message("Magneto_Message_LaunchOnCooldown".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }

                if (!LaunchAt(target))
                {
                    Messages.Message("Magneto_Message_NoOrbiters".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
            };

            if (Find.TickManager.TicksGame < nextLaunchTick)
            {
                launch.Disable("Magneto_Message_LaunchOnCooldown".Translate());
            }
            yield return launch;

            Texture2D dropIcon = ContentFinder<Texture2D>.Get("UI/Commands/CommandMagnetoDrop", true);
            Command_Action drop = new Command_Action
            {
                defaultLabel = "Magneto_Gizmo_DropLabel".Translate(),
                defaultDesc = "Magneto_Gizmo_DropDesc".Translate(),
                icon = dropIcon ?? BaseContent.BadTex,
                action = DropAllOrbiting
            };
            yield return drop;
        }

        private bool IsValidGrabTarget(Thing thing)
        {
            if (thing == null || Pawn == null || Map == null || thing.Map != Map || !thing.Spawned)
            {
                return false;
            }

            if (thing.def?.category != ThingCategory.Item)
            {
                return false;
            }

            float radius = MagnetoMod.ActiveSettings.magneticRadius;
            if (!thing.Position.InHorDistOf(Pawn.Position, radius))
            {
                return false;
            }

            return MagnetoUtility.IsMetallic(thing);
        }

        private bool IsValidPawnTarget(Pawn pawn)
        {
            if (pawn == null || Pawn == null || Map == null || pawn.Map != Map)
            {
                return false;
            }

            if (!pawn.Position.InHorDistOf(Pawn.Position, MagnetoMod.ActiveSettings.magneticRadius))
            {
                return false;
            }

            Thing weapon = pawn.equipment?.Primary;
            if (weapon == null)
            {
                return false;
            }

            return MagnetoUtility.IsMetallic(weapon);
        }

        private bool TryGrabThing(Thing thing)
        {
            if (thing == null || Pawn == null || Map == null)
            {
                return false;
            }

            orbitingItems ??= new ThingOwner<Thing>(this, false);

            if (!thing.Spawned)
            {
                Messages.Message("Magneto_Message_TargetUnavailable".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            float radius = MagnetoMod.ActiveSettings.magneticRadius;
            if (!thing.Position.InHorDistOf(Pawn.Position, radius))
            {
                Messages.Message("Magneto_Message_TargetOutOfRange".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (thing.def?.category != ThingCategory.Item)
            {
                Messages.Message("Magneto_Message_TargetNotItem".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (!MagnetoUtility.IsMetallic(thing))
            {
                Messages.Message("Magneto_Message_TargetNotMetallic".Translate(thing.LabelCap), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (orbitingItems.Count >= MagnetoMod.ActiveSettings.maxOrbitingItems)
            {
                Messages.Message("Magneto_Message_OrbitFull".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            Thing toOrbit = thing;
            if (thing.stackCount > 1)
            {
                toOrbit = thing.SplitOff(1);
            }
            else
            {
                thing.DeSpawn(DestroyMode.Vanish);
            }

            AddOrbitingThing(toOrbit);
            SoundDef.Named("EnergyShield_Reset").PlayOneShot(new TargetInfo(Pawn.Position, Map));
            return true;
        }

        private bool TryGrabFromPawn(Pawn targetPawn)
        {
            if (!IsValidPawnTarget(targetPawn))
            {
                Messages.Message("Magneto_Message_TargetNoWeapon".Translate(targetPawn.LabelShortCap), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            ThingWithComps weapon = targetPawn.equipment.Primary;
            if (!targetPawn.equipment.TryDropEquipment(weapon, out ThingWithComps dropped, targetPawn.Position, forbid: false))
            {
                Messages.Message("Magneto_Message_TargetUnavailable".Translate(), Pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            if (dropped.Spawned)
            {
                dropped.DeSpawn(DestroyMode.Vanish);
            }

            AddOrbitingThing(dropped);
            FleckMaker.Static(targetPawn.DrawPos, Map, FleckDefOf.PsycastAreaEffect, 0.7f);
            if (Pawn.Faction == Faction.OfPlayer)
            {
                Messages.Message("Magneto_Message_DisarmedWeapon".Translate(Pawn.LabelShort, targetPawn.LabelShort, dropped.LabelNoCount), new LookTargets(new[] { Pawn, targetPawn }), MessageTypeDefOf.PositiveEvent);
            }

            return true;
        }

        private void AddOrbitingThing(Thing thing)
        {
            if (thing == null)
            {
                return;
            }

            orbitingItems ??= new ThingOwner<Thing>(this, false);

            int maxOrbiters = Mathf.Max(1, MagnetoMod.ActiveSettings.maxOrbitingItems);
            if (orbitingItems.Count >= maxOrbiters)
            {
                if (Map != null && Pawn?.Spawned == true)
                {
                    GenPlace.TryPlaceThing(thing, Pawn.Position, Map, ThingPlaceMode.Near);
                }
                else
                {
                    thing.Destroy();
                }
                return;
            }
            Thing added = thing;

            if (thing.holdingOwner != null)
            {
                thing.holdingOwner.TryTransferToContainer(thing, orbitingItems, thing.stackCount, canMergeWithExistingStacks: false);
                added = thing;
            }
            else if (!orbitingItems.TryAdd(thing, canMergeWithExistingStacks: false))
            {
                thing.Destroy();
                return;
            }

            FleckMaker.Static(Pawn.DrawPos + Vector3Utility.RandomHorizontalOffset(0.3f), Map, FleckDefOf.PsycastAreaEffect, 0.5f);
            SpawnVisualFor(thing);
            RefreshVisualLayout();
        }

        private bool LaunchAt(LocalTargetInfo target)
        {
            if (Map == null || orbitingItems == null || orbitingItems.Count == 0)
            {
                return false;
            }

            Thing payload = orbitingItems.Count > 0 ? orbitingItems[0] : null;
            if (payload == null)
            {
                return false;
            }

            payload = orbitingItems.Take(payload);
            if (payload == null)
            {
                return false;
            }

            RemoveVisual(payload);
            RefreshVisualLayout();

            if (!(GenSpawn.Spawn(MagnetoDefOf.Magneto_ProjectileOrbitItem, Pawn.Position, Map) is Projectile_MagnetoItem projectile))
            {
                GenPlace.TryPlaceThing(payload, Pawn.Position, Map, ThingPlaceMode.Near);
                return false;
            }

            projectile.Prepare(payload);
            projectile.Launch(Pawn, target, target, ProjectileHitFlags.IntendedTarget);

            if (Map != null)
            {
                SoundDef.Named("GunShotA").PlayOneShot(new TargetInfo(Pawn.Position, Map));
            }

            float cooldownSeconds = MagnetoMod.ActiveSettings.launchCooldownSeconds;
            if (cooldownSeconds <= 0f)
            {
                cooldownSeconds = Props.launchCooldownSeconds;
            }
            int cooldownTicks = Mathf.RoundToInt(Mathf.Max(cooldownSeconds, 0.1f) * 60f);
            nextLaunchTick = Find.TickManager.TicksGame + cooldownTicks;
            return true;
        }

        private void DropAllOrbiting()
        {
            if (orbitingItems == null || orbitingItems.Count == 0)
            {
                return;
            }

            DropAllOrbitingInternal(null, IntVec3.Invalid, true);
        }

        private bool DropAllOrbitingInternal(Map overrideMap, IntVec3 overrideCell, bool destroyIfInvalid)
        {
            if (orbitingItems == null || orbitingItems.Count == 0)
            {
                return false;
            }

            Map dropMap = overrideMap ?? Map ?? Pawn?.MapHeld ?? Pawn?.Corpse?.Map;
            IntVec3 dropCell = overrideCell.IsValid ? overrideCell : IntVec3.Invalid;

            if (!dropCell.IsValid && Pawn != null)
            {
                if (Pawn.Spawned)
                {
                    dropCell = Pawn.Position;
                }
                else if (Pawn.PositionHeld.IsValid)
                {
                    dropCell = Pawn.PositionHeld;
                }
                else if (Pawn.Corpse != null)
                {
                    dropCell = Pawn.Corpse.PositionHeld;
                    dropMap ??= Pawn.Corpse.MapHeld;
                }
            }

            if (dropMap == null || !dropCell.IsValid)
            {
                if (destroyIfInvalid)
                {
                    List<Thing> contents = orbitingItems.ToList();
                    foreach (Thing thing in contents)
                    {
                        RemoveVisual(thing);
                    }

                    orbitingItems.ClearAndDestroyContents(DestroyMode.Vanish);
                    visualsDirty = true;
                }
                return false;
            }

            List<Thing> existing = orbitingItems.ToList();
            foreach (Thing thing in existing)
            {
                RemoveVisual(thing);
            }

            orbitingItems.TryDropAll(dropCell, dropMap, ThingPlaceMode.Near);
            visualsDirty = true;
            return true;
        }

        private bool TryDropOrbitingForDeadPawn()
        {
            if (orbitingItems == null || orbitingItems.Count == 0)
            {
                return true;
            }

            Corpse corpse = Pawn?.Corpse;
            Map corpseMap = corpse?.Map ?? corpse?.MapHeld ?? Pawn?.MapHeld;
            IntVec3 dropCell = IntVec3.Invalid;

            if (corpse != null)
            {
                dropCell = corpse.Spawned ? corpse.Position : corpse.PositionHeld;
            }

            if (!dropCell.IsValid && Pawn != null)
            {
                dropCell = Pawn.PositionHeld;
            }

            if (!dropCell.IsValid || corpseMap == null)
            {
                return false;
            }

            return DropAllOrbitingInternal(corpseMap, dropCell, false);
        }

        private void SpawnVisualFor(Thing thing)
        {
            if (thing == null)
            {
                return;
            }

            orbitingVisuals ??= new Dictionary<Thing, OrbitingMagnetThing>();

            if (Pawn?.Spawned != true || Map == null)
            {
                visualsDirty = true;
                return;
            }

            if (orbitingVisuals.TryGetValue(thing, out OrbitingMagnetThing existing) && existing != null)
            {
                existing.MarkParentRemoved();
                if (existing.Spawned)
                {
                    existing.Destroy(DestroyMode.Vanish);
                }
                orbitingVisuals.Remove(thing);
            }

            OrbitingMagnetThing visual = ThingMaker.MakeThing(MagnetoDefOf.Magneto_OrbitingVisual) as OrbitingMagnetThing;
            if (visual == null)
            {
                return;
            }

            visual = (OrbitingMagnetThing)GenSpawn.Spawn(visual, Pawn.Position, Map, WipeMode.Vanish);
            visual.Initialize(thing, Pawn, this, Rand.Range(0f, 360f), BaseOrbitRadius);
            orbitingVisuals[thing] = visual;
        }

        private void RemoveVisual(Thing thing)
        {
            if (thing == null || orbitingVisuals == null)
            {
                return;
            }

            if (orbitingVisuals.TryGetValue(thing, out OrbitingMagnetThing visual) && visual != null)
            {
                orbitingVisuals.Remove(thing);
                visual.MarkParentRemoved();
                if (visual.Spawned)
                {
                    visual.Destroy(DestroyMode.Vanish);
                }
            }
        }

        private void RefreshVisualLayout()
        {
            if (orbitingItems == null || orbitingVisuals == null || orbitingVisuals.Count == 0)
            {
                return;
            }

            float radius = BaseOrbitRadius;
            int count = Math.Max(1, orbitingItems.Count);
            float step = 360f / count;
            int index = 0;

            foreach (Thing thing in orbitingItems)
            {
                if (orbitingVisuals.TryGetValue(thing, out OrbitingMagnetThing visual) && visual != null)
                {
                    visual.SetRadius(radius);
                    visual.SetAngle(step * index);
                }
                index++;
            }
        }

        private void RebuildOrbitVisuals()
        {
            ClearVisuals();
            if (orbitingItems == null || orbitingItems.Count == 0)
            {
                return;
            }

            foreach (Thing thing in orbitingItems)
            {
                SpawnVisualFor(thing);
            }

            RefreshVisualLayout();
        }

        private void ClearVisuals()
        {
            if (orbitingVisuals == null || orbitingVisuals.Count == 0)
            {
                return;
            }

            foreach (OrbitingMagnetThing visual in orbitingVisuals.Values.ToList())
            {
                if (visual != null)
                {
                    visual.MarkParentRemoved();
                    if (visual.Spawned)
                    {
                        visual.Destroy(DestroyMode.Vanish);
                    }
                }
            }

            orbitingVisuals.Clear();
        }

        internal void NotifyVisualRemoved(Thing payload)
        {
            if (payload == null || orbitingVisuals == null)
            {
                return;
            }

            orbitingVisuals.Remove(payload);
            visualsDirty = true;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return orbitingItems;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            if (orbitingItems != null)
            {
                ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, orbitingItems);
            }
        }

        public IThingHolder ParentHolder => Pawn;
    }
}
