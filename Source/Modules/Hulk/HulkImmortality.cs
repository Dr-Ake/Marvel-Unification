using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Drake.Hulk
{
    [StaticConstructorOnStartup]
    internal static class HulkHarmonyBootstrap
    {
        static HulkHarmonyBootstrap()
        {
            MarvelUnification.MarvelHarmony.EnsurePatched();
        }
    }

    internal static class HulkImmortalityManager
    {
        private static readonly HashSet<Pawn> PendingResurrections = new HashSet<Pawn>();
        private static readonly HashSet<Pawn> SuppressedImmediateResurrections = new HashSet<Pawn>();
        private static int lastProcessedTick = -1;
        private static readonly MethodInfo TryDropCarriedThingMethod = typeof(Pawn_CarryTracker)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(method =>
            {
                if (method.Name != "TryDropCarriedThing")
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length >= 3 &&
                       parameters[0].ParameterType == typeof(IntVec3) &&
                       parameters[1].ParameterType == typeof(ThingPlaceMode) &&
                       parameters[2].ParameterType.IsByRef;
            });

        public static void ResetTransientState()
        {
            PendingResurrections.Clear();
            SuppressedImmediateResurrections.Clear();
            lastProcessedTick = -1;
            HulkGreenDoorManager.InitializeForGame();
        }

        public static void QueueResurrection(Pawn pawn)
        {
            if (pawn != null)
            {
                PendingResurrections.Add(pawn);
            }
        }

        public static void RemovePendingResurrection(Pawn pawn)
        {
            if (pawn != null)
            {
                PendingResurrections.Remove(pawn);
            }
        }

        public static void SuppressImmediateResurrectionForNextDeath(Pawn pawn)
        {
            if (pawn != null)
            {
                SuppressedImmediateResurrections.Add(pawn);
            }
        }

        public static bool ConsumeImmediateResurrectionSuppression(Pawn pawn)
        {
            return pawn != null && SuppressedImmediateResurrections.Remove(pawn);
        }

        public static void RecordAggressor(Pawn pawn, Thing aggressor)
        {
            GetIdentityComp(pawn)?.RecordAggressor(aggressor);
        }

        private static bool IsPawnPermanentlyGone(Pawn pawn)
        {
            return pawn == null || pawn.Discarded || (pawn.Destroyed && !pawn.Dead);
        }

        public static Thing GetRevengeTarget(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.CurrentRageTarget;
        }

        public static bool HasActiveRage(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.HasActiveRage == true;
        }

        public static void Tick()
        {
            var currentTick = Find.TickManager?.TicksGame ?? -1;
            if (currentTick >= 0)
            {
                if (lastProcessedTick == currentTick)
                {
                    return;
                }

                lastProcessedTick = currentTick;
            }

            DiscoverDeadGammaPawns();
            ProcessPendingResurrections();
            HulkGreenDoorManager.Tick();
            MaintainActiveRage();
        }

        public static bool TryTriggerHostileCarryRage(Pawn victim, Pawn carrier)
        {
            if (!ShouldTriggerHostileCarryRage(victim, carrier))
            {
                return false;
            }

            return TriggerHostileCarryRage(victim, carrier);
        }

        public static bool TriggerHostileCarryRage(Pawn victim, Pawn carrier)
        {
            if (victim == null || carrier == null || victim.Dead)
            {
                return false;
            }

            HulkUtility.EnsureBannerState(victim);
            TriggerEmergencyHulk(victim, carrier, 1800, 0.92f, emergencyRecovery: true);
            return true;
        }

        private static void ProcessPendingResurrections()
        {
            if (PendingResurrections.Count == 0 || Current.Game == null)
            {
                return;
            }

            foreach (var pawn in PendingResurrections.ToList())
            {
                if (IsPawnPermanentlyGone(pawn))
                {
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (pawn.Dead && pawn.Corpse == null && pawn.MapHeld == null)
                {
                    HulkGreenDoorManager.ScheduleReturn(pawn);
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (!pawn.Dead)
                {
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (HulkGreenDoorManager.HasScheduledReturn(pawn))
                {
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (!HulkHealingUtility.Settings.autoResurrect)
                {
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (GetIdentityComp(pawn) == null && !HulkUtility.HasBannerTrait(pawn))
                {
                    PendingResurrections.Remove(pawn);
                    continue;
                }

                if (!TryEmergencyResurrect(pawn))
                {
                    continue;
                }

                PendingResurrections.Remove(pawn);
            }
        }

        private static void DiscoverDeadGammaPawns()
        {
            if (Current.Game == null || Find.TickManager == null || Find.TickManager.TicksGame % 10 != 0)
            {
                return;
            }

            foreach (var map in Find.Maps)
            {
                foreach (var corpse in map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>())
                {
                    var pawn = corpse.InnerPawn;
                    if (pawn?.Dead == true && (GetIdentityComp(pawn) != null || HulkUtility.HasBannerTrait(pawn)))
                    {
                        PendingResurrections.Add(pawn);
                    }
                }
            }
        }

        internal static bool TryEmergencyResurrect(Pawn pawn)
        {
            var identityComp = GetIdentityComp(pawn);
            var revengeTarget = identityComp?.ConsumeFreshAggressor(2500);
            if (identityComp == null && !HulkUtility.HasBannerTrait(pawn))
            {
                return false;
            }

            var corpse = pawn.Corpse;
            var rot = corpse?.TryGetComp<CompRottable>();
            float? previousRotProgress = null;

            try
            {
                if (rot != null)
                {
                    previousRotProgress = rot.RotProgress;
                    rot.RotProgress = 0f;
                }
            }
            catch
            {
            }

            try
            {
                ResurrectionUtility.TryResurrect(pawn);
            }
            catch
            {
                return false;
            }
            finally
            {
                try
                {
                    if (rot != null && previousRotProgress.HasValue)
                    {
                        rot.RotProgress = previousRotProgress.Value;
                    }
                }
                catch
                {
                }
            }

            if (pawn.Dead)
            {
                return false;
            }

            HulkGreenDoorManager.CancelForPawn(pawn);
            HulkUtility.EnsureBannerState(pawn);
            identityComp = GetIdentityComp(pawn);
            TriggerEmergencyHulk(pawn, revengeTarget, 2500, 1f, emergencyRecovery: true);

            try
            {
                if (pawn.Spawned)
                {
                    var fire = pawn.Position.GetFirstThing(pawn.Map, ThingDefOf.Fire);
                    fire?.Destroy(DestroyMode.Vanish);
                }

                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }
            catch
            {
            }

            return true;
        }

        private static void MaintainActiveRage()
        {
            if (Current.Game == null)
            {
                return;
            }

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned.ToList())
                {
                    HandleHostileCarryEscape(pawn);

                    if (pawn == null || pawn.Dead)
                    {
                        continue;
                    }

                    var identityComp = GetIdentityComp(pawn);
                    var hadActiveRage = identityComp?.HasActiveRage == true;
                    if (hadActiveRage)
                    {
                        identityComp.TickRage();
                    }

                    var hasActiveRage = identityComp?.HasActiveRage == true;
                    if (hadActiveRage && !hasActiveRage && HulkHealingUtility.Settings.enableForcedHulkRevert && HulkUtility.IsTransformed(pawn))
                    {
                        HulkUtility.Revert(pawn);
                        identityComp?.ClearFormTimerStateIfInactive();
                        continue;
                    }

                    if (hasActiveRage && !HulkUtility.IsTransformed(pawn))
                    {
                        HulkUtility.TransformForEmergency(pawn);
                    }

                    if (HulkUtility.IsTransformed(pawn))
                    {
                        if (identityComp?.TickManualFormTimer(!hasActiveRage) == true && HulkHealingUtility.Settings.enableTimeRestrictedHulk)
                        {
                            HulkUtility.Revert(pawn);
                            identityComp?.ClearFormTimerStateIfInactive();
                            continue;
                        }

                        MaintainHulkForm(pawn);
                    }
                    else
                    {
                        MaintainPassiveGammaRecovery(pawn);
                        identityComp?.ClearFormTimerStateIfInactive();
                        continue;
                    }

                    if (hasActiveRage && pawn.IsHashIntervalTick(60))
                    {
                        TryForceRevengeJob(pawn, identityComp.CurrentRageTarget);
                    }
                }
            }
        }

        private static void MaintainHulkForm(Pawn pawn)
        {
            var form = HulkUtility.GetFormHediff(pawn);
            if (form == null)
            {
                return;
            }

            HulkUtility.EnsureHulkAppearance(pawn);
            form.Severity = Mathf.Clamp(HulkUtility.GetRageLevel(pawn), 0.28f, 1f);

            var formComp = form.TryGetComp<HediffComp_HulkForm>();
            var pulseTicks = formComp?.Props.ticksPerPulse ?? 45;
            var baseHealing = formComp?.Props.healPerPulse ?? 3f;

            if (pawn.IsHashIntervalTick(pulseTicks))
            {
                var scaledHealing = baseHealing * Mathf.Lerp(0.85f, 2.6f, form.Severity);
                HulkUtility.ApplyRegenerationPulse(pawn, scaledHealing);
            }

            HulkHealingUtility.TickMissingPartRegrowth(pawn, Mathf.Lerp(1.15f, 2.2f, form.Severity), allowNewStarts: true);
        }

        private static void MaintainPassiveGammaRecovery(Pawn pawn)
        {
            if (!HulkUtility.HasGammaIdentity(pawn))
            {
                return;
            }

            if (HulkHealingUtility.Settings.healOutsideHulkForm && pawn.IsHashIntervalTick(90))
            {
                var passiveHealing = (HulkDefOf.Drake_HulkForm?.comps?.OfType<HediffCompProperties_HulkForm>().FirstOrDefault()?.healPerPulse ?? 3f) * 0.45f;
                HulkUtility.ApplyRegenerationPulse(pawn, passiveHealing);
            }

            HulkHealingUtility.TickMissingPartRegrowth(pawn, 0.65f, allowNewStarts: HulkHealingUtility.Settings.healOutsideHulkForm);
        }

        private static void HandleHostileCarryEscape(Pawn carrier)
        {
            if (carrier?.carryTracker?.CarriedThing is not Pawn victim)
            {
                return;
            }

            if (!ShouldTriggerHostileCarryRage(victim, carrier))
            {
                return;
            }

            TryDropCarriedThing(carrier);
            TriggerEmergencyHulk(victim, carrier, 1800, 0.92f, emergencyRecovery: true);
        }

        private static void TriggerEmergencyHulk(Pawn pawn, Thing aggressor, int rageTicks, float severityFloor, bool emergencyRecovery)
        {
            if (pawn == null)
            {
                return;
            }

            HulkUtility.EnsureBannerState(pawn);
            HulkUtility.TransformForEmergency(pawn);

            if (emergencyRecovery)
            {
                HulkHealingUtility.ApplyEmergencyRecovery(pawn);
            }

            var identityComp = GetIdentityComp(pawn);
            identityComp?.BeginRage(aggressor, rageTicks, severityFloor);
            TryForceRevengeJob(pawn, aggressor);
        }

        private static bool TryForceRevengeJob(Pawn pawn, Thing target)
        {
            if (pawn?.jobs == null || !IsValidRevengeTarget(pawn, target))
            {
                return false;
            }

            if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.AttackMelee && pawn.CurJob.targetA.Thing == target)
            {
                return true;
            }

            var job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            job.expiryInterval = 2000;
            job.checkOverrideOnExpire = true;
            job.killIncappedTarget = true;
            job.canBashDoors = true;
            pawn.jobs.TryTakeOrderedJob(job);
            return true;
        }

        private static bool ShouldTriggerHostileCarryRage(Pawn victim, Pawn carrier)
        {
            if (!HulkHealingUtility.Settings.triggerRageOnHostileCarry || victim == null || carrier == null)
            {
                return false;
            }

            if (victim.Dead || !victim.Downed || victim == carrier)
            {
                return false;
            }

            if (GetIdentityComp(victim) == null)
            {
                return false;
            }

            if (victim.Faction != null && carrier.Faction != null)
            {
                return carrier.Faction != victim.Faction;
            }

            return carrier.HostileTo(victim);
        }

        private static bool IsValidRevengeTarget(Pawn pawn, Thing target)
        {
            if (pawn == null || target == null || target.Destroyed)
            {
                return false;
            }

            if (target is Pawn targetPawn && targetPawn.Dead)
            {
                return false;
            }

            return pawn.Map != null && target.Map == pawn.Map;
        }

        private static void TryDropCarriedThing(Pawn carrier)
        {
            if (carrier?.carryTracker == null || TryDropCarriedThingMethod == null)
            {
                return;
            }

            try
            {
                var parameters = TryDropCarriedThingMethod.GetParameters();
                var args = new object[parameters.Length];
                args[0] = carrier.PositionHeld;
                args[1] = ThingPlaceMode.Near;
                args[2] = null;
                for (var i = 3; i < parameters.Length; i++)
                {
                    args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                }

                TryDropCarriedThingMethod.Invoke(carrier.carryTracker, args);
            }
            catch
            {
            }
        }
        private static HediffComp_HulkIdentity GetIdentityComp(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkIdentity)?.TryGetComp<HediffComp_HulkIdentity>();
        }
    }

    public sealed class HulkEmergencyGameComponent : GameComponent
    {
        public HulkEmergencyGameComponent(Game game)
        {
            HulkImmortalityManager.ResetTransientState();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            HulkGreenDoorManager.ExposeData();
        }

        public override void GameComponentTick()
        {
            HulkImmortalityManager.Tick();
        }
    }

    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    internal static class TickManager_DoSingleTick_HulkPatch
    {
        private static void Postfix()
        {
            HulkImmortalityManager.Tick();
        }
    }

    internal static class HulkViolenceOverrideUtility
    {
        private static readonly FieldInfo PawnStoryPawnField = AccessTools.Field(typeof(Pawn_StoryTracker), "pawn");

        public static WorkTags RemoveViolentRestrictionIfHulk(Pawn pawn, WorkTags tags)
        {
            if (!ShouldIgnoreViolentWorkTag(pawn))
            {
                return tags;
            }

            return tags & ~WorkTags.Violent;
        }

        public static bool ShouldIgnoreViolentWorkTag(Pawn pawn)
        {
            return pawn != null && HulkUtility.IsTransformed(pawn);
        }

        public static Pawn GetPawn(Pawn_StoryTracker story)
        {
            return PawnStoryPawnField?.GetValue(story) as Pawn;
        }
    }

    [HarmonyPatch(typeof(Pawn), "get_CombinedDisabledWorkTags")]
    internal static class Pawn_CombinedDisabledWorkTags_HulkPatch
    {
        private static void Postfix(Pawn __instance, ref WorkTags __result)
        {
            __result = HulkViolenceOverrideUtility.RemoveViolentRestrictionIfHulk(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(Pawn), "WorkTagIsDisabled")]
    internal static class Pawn_WorkTagIsDisabled_HulkPatch
    {
        private static void Postfix(Pawn __instance, WorkTags w, ref bool __result)
        {
            if (!__result || w != WorkTags.Violent || !HulkViolenceOverrideUtility.ShouldIgnoreViolentWorkTag(__instance))
            {
                return;
            }

            __result = false;
        }
    }

    [HarmonyPatch(typeof(Pawn_StoryTracker), "get_DisabledWorkTagsBackstoryAndTraits")]
    internal static class PawnStory_DisabledWorkTagsBackstoryAndTraits_HulkPatch
    {
        private static void Postfix(Pawn_StoryTracker __instance, ref WorkTags __result)
        {
            __result = HulkViolenceOverrideUtility.RemoveViolentRestrictionIfHulk(HulkViolenceOverrideUtility.GetPawn(__instance), __result);
        }
    }

    [HarmonyPatch(typeof(Pawn_StoryTracker), "get_DisabledWorkTagsBackstoryTraitsAndGenes")]
    internal static class PawnStory_DisabledWorkTagsBackstoryTraitsAndGenes_HulkPatch
    {
        private static void Postfix(Pawn_StoryTracker __instance, ref WorkTags __result)
        {
            __result = HulkViolenceOverrideUtility.RemoveViolentRestrictionIfHulk(HulkViolenceOverrideUtility.GetPawn(__instance), __result);
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "PreApplyDamage")]
    internal static class PawnHealthTracker_PreApplyDamage_HulkPatch
    {
        private static void Prefix(Pawn ___pawn, ref DamageInfo dinfo)
        {
            if (___pawn == null)
            {
                return;
            }

            HulkImmortalityManager.RecordAggressor(___pawn, dinfo.Instigator);
        }
    }

    [HarmonyPatch(typeof(Pawn), "Kill")]
    internal static class Pawn_Kill_HulkPatch
    {
        private static void Postfix(Pawn __instance)
        {
            if (__instance?.Dead != true)
            {
                return;
            }

            if (HulkImmortalityManager.ConsumeImmediateResurrectionSuppression(__instance))
            {
                return;
            }

            if (!HulkHealingUtility.Settings.autoResurrect ||
                (__instance.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkIdentity) == null && !HulkUtility.HasBannerTrait(__instance)))
            {
                return;
            }

            if (!HulkImmortalityManager.TryEmergencyResurrect(__instance))
            {
                HulkImmortalityManager.QueueResurrection(__instance);
            }
        }
    }

    [HarmonyPatch(typeof(Corpse), "Destroy")]
    internal static class Corpse_Destroy_HulkPatch
    {
        private static bool Prefix(Corpse __instance)
        {
            return !HulkGreenDoorManager.TryCaptureDestroyedCorpse(__instance);
        }
    }
}
