using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DeadpoolsHealingFactor
{
    public class HediffCompProperties_WolverineClaws : HediffCompProperties
    {
        public HediffCompProperties_WolverineClaws()
        {
            compClass = typeof(HediffComp_WolverineClaws);
        }
    }

    [StaticConstructorOnStartup]
    public class HediffComp_WolverineClaws : HediffComp
    {
        private ThingWithComps storedWeapon;

        private static readonly Texture2D ToggleIcon = ContentFinder<Texture2D>.Get("UI/Commands/CommandWolverineClaws", true);

        private Pawn CurrentPawn => parent?.pawn;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_References.Look(ref storedWeapon, "storedWeapon");
        }

        private static bool HasClawsEquipped(Pawn pawn)
        {
            if (pawn?.equipment == null)
            {
                return false;
            }
            return pawn.equipment.Primary?.def == DPDefOf.DP_WolverineClawWeapon;
        }

        private static readonly int GizmoErrorKey = 0x5F12A6C3;

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            IEnumerable<Gizmo> baseGizmos;
            try
            {
                baseGizmos = base.CompGetGizmos() ?? Enumerable.Empty<Gizmo>();
            }
            catch (System.Exception ex)
            {
                Log.WarningOnce($"[DeadpoolsHealingFactor] Wolverine claws base gizmos failed: {ex}", GizmoErrorKey ^ 0x11);
                baseGizmos = Enumerable.Empty<Gizmo>();
            }

            foreach (var gizmo in baseGizmos)
            {
                if (gizmo != null)
                {
                    yield return gizmo;
                }
            }

            Pawn pawn = CurrentPawn;
            if (pawn == null)
            {
                yield break;
            }
            if (!pawn.Spawned || pawn.DestroyedOrNull() || pawn.Dead)
            {
                yield break;
            }
            if (pawn.MapHeld == null)
            {
                yield break;
            }
            if (!IsEligibleForToggle(pawn))
            {
                yield break;
            }
            if (pawn.equipment == null)
            {
                yield break;
            }

            bool clawsExtended = HasClawsEquipped(pawn);

            Texture2D icon = ToggleIcon ?? TexCommand.Attack;
            string label = clawsExtended ? "Retract claws" : "Extend claws";

            float damage = DeadpoolsHealingFactorMod.settings?.clawDamage ?? 18f;
            float armorPen = (DeadpoolsHealingFactorMod.settings?.clawArmorPen ?? 0.35f) * 100f;
            float cooldown = DeadpoolsHealingFactorMod.settings?.clawCooldown ?? 1.5f;
            string desc = "Extend or retract the pawn's adamantium claws. Retracting them slices the hands, causing bleeding." +
                $"\n\nCurrent Stats:" +
                $"\n  Damage: {damage:F1}" +
                $"\n  Armor Penetration: {armorPen:F0}%" +
                $"\n  Cooldown: {cooldown:F2}s";

            yield return new Command_Action
            {
                defaultLabel = label,
                defaultDesc = desc,
                icon = icon,
                action = delegate
                {
                    Pawn targetPawn = CurrentPawn;
                    if (targetPawn == null)
                    {
                        return;
                    }
                    if (!targetPawn.Spawned || targetPawn.DestroyedOrNull() || targetPawn.Dead)
                    {
                        return;
                    }
                    if (!IsEligibleForToggle(targetPawn))
                    {
                        return;
                    }
                    if (HasClawsEquipped(targetPawn))
                    {
                        RetractClaws();
                    }
                    else
                    {
                        ExtendClaws();
                    }
                }
            };
        }

        private static bool IsEligibleForToggle(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true)
            {
                return false;
            }
            if (pawn.Dead || pawn.DestroyedOrNull())
            {
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                return false;
            }
            if (pawn.Drafted && pawn.stances?.FullBodyBusy == true)
            {
                return false;
            }
            if (DPAdamantiumUtility.IsUndergoingInfusion(pawn))
            {
                return false;
            }
            return true;
        }

        private void ExtendClaws()
        {
            Pawn pawn = CurrentPawn;
            if (pawn == null || pawn.equipment == null)
            {
                return;
            }
            if (HasClawsEquipped(pawn))
            {
                return;
            }
            try
            {
                ThingWithComps primary = pawn.equipment.Primary;
                if (primary != null && primary.def != DPDefOf.DP_WolverineClawWeapon)
                {
                    storedWeapon = primary;
                    if (pawn.inventory?.innerContainer != null)
                    {
                        if (!pawn.equipment.TryTransferEquipmentToContainer(primary, pawn.inventory.innerContainer))
                        {
                            pawn.equipment.Remove(primary);
                            pawn.inventory.innerContainer.TryAdd(primary, true);
                        }
                    }
                    else
                    {
                        pawn.equipment.Remove(primary);
                        primary.Destroy(DestroyMode.Vanish);
                        storedWeapon = null;
                    }
                }
                else
                {
                    storedWeapon = null;
                }

                Thing claws = ThingMaker.MakeThing(DPDefOf.DP_WolverineClawWeapon);
                if (claws is ThingWithComps weapon)
                {
                    pawn.equipment.AddEquipment(weapon);
                    PlayClawExtendSound(pawn);
                }
            }
            catch
            {
            }
        }

        private void RetractClaws()
        {
            Pawn pawn = CurrentPawn;
            if (pawn == null || pawn.equipment == null)
            {
                return;
            }
            try
            {
                ThingWithComps primary = pawn.equipment.Primary;
                if (primary != null && primary.def == DPDefOf.DP_WolverineClawWeapon)
                {
                    pawn.equipment.Remove(primary);
                    primary.Destroy(DestroyMode.Vanish);
                }
            }
            catch
            {
            }

            RestoreStoredWeapon();
            CauseRetractionInjuries();
        }

        private void RestoreStoredWeapon()
        {
            Pawn pawn = CurrentPawn;
            if (pawn == null || pawn.equipment == null)
            {
                storedWeapon = null;
                return;
            }
            if (storedWeapon == null || storedWeapon.Destroyed)
            {
                storedWeapon = null;
                return;
            }
            try
            {
                if (pawn.inventory != null && pawn.inventory.innerContainer.Contains(storedWeapon))
                {
                    pawn.inventory.innerContainer.Remove(storedWeapon);
                    pawn.equipment.AddEquipment(storedWeapon);
                }
                else if (!storedWeapon.Spawned)
                {
                    pawn.equipment.AddEquipment(storedWeapon);
                }
                else
                {
                    pawn.equipment.AddEquipment(storedWeapon);
                }
            }
            catch
            {
            }
            storedWeapon = null;
        }

        private void PlayClawExtendSound(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }
            DeadpoolsHealingFactorSettings settings = DeadpoolsHealingFactorMod.settings;
            if (settings != null && !settings.enableClawSound)
            {
                return;
            }
            SoundDef sound = DPDefOf.DP_WolverineClawExtend;
            if (sound == null)
            {
                return;
            }
            try
            {
                if (pawn.Spawned && pawn.MapHeld != null)
                {
                    SoundStarter.PlayOneShot(sound, SoundInfo.InMap(new TargetInfo(pawn.PositionHeld, pawn.MapHeld)));
                }
            }
            catch
            {
            }
        }

        private void CauseRetractionInjuries()
        {
            if (CurrentPawn?.health?.hediffSet == null)
            {
                return;
            }
            try
            {
                var hands = CurrentPawn.health.hediffSet.GetNotMissingParts()
                    .Where(part => part.def == BodyPartDefOf.Hand)
                    .ToList();
                foreach (var hand in hands)
                {
                    float dmg = Rand.Range(4f, 7f);
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Cut, dmg, 0f, -1f, CurrentPawn, hand);
                    CurrentPawn.TakeDamage(dinfo);
                }
            }
            catch
            {
            }
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (CurrentPawn != null && HasClawsEquipped(CurrentPawn))
            {
                RetractClaws();
            }
            storedWeapon = null;
        }
    }
}
