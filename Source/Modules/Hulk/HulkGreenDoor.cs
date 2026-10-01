using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Drake.Hulk
{
    internal static class HulkGreenDoorManager
    {
        private const int TicksPerHour = 2500;
        private const int ManifestCheckIntervalTicks = 60;
        private const int HulkReturnDurationTicks = 180;
        private const int DoorCleanupDelayTicks = 300;

        private static readonly Color GreenDoorColor = new Color(0.22f, 0.95f, 0.28f, 1f);
        private static List<HulkGreenDoorReturnRecord> PendingReturns = new List<HulkGreenDoorReturnRecord>();
        private static bool suppressCorpseCapture;
        private static readonly FieldInfo WeatherManagerField = AccessTools.Field(typeof(Map), "weatherManager");
        private static readonly MethodInfo WeatherTransitionMethod = AccessTools.Method(AccessTools.TypeByName("WeatherManager"), "TransitionTo");
        private static readonly FieldInfo TicksToDisappearField = AccessTools.Field(typeof(HediffComp_Disappears), "ticksToDisappear");

        private static bool IsPawnPermanentlyGone(Pawn pawn)
        {
            return pawn == null || pawn.Discarded || (pawn.Destroyed && !pawn.Dead);
        }

        public static void InitializeForGame()
        {
            PendingReturns ??= new List<HulkGreenDoorReturnRecord>();
            PendingReturns.Clear();
        }

        public static void ExposeData()
        {
            Scribe_Collections.Look(ref PendingReturns, "greenDoorReturns", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                PendingReturns ??= new List<HulkGreenDoorReturnRecord>();
                PendingReturns.RemoveAll(record => record == null || record.Pawn == null);
            }
        }

        public static void Tick()
        {
            if (Current.Game == null ||
                Find.TickManager == null ||
                PendingReturns.Count == 0 ||
                !HulkHealingUtility.Settings.autoResurrect ||
                !HulkHealingUtility.Settings.enableGreenDoorReturn ||
                Find.TickManager.TicksGame % ManifestCheckIntervalTicks != 0)
            {
                return;
            }

            foreach (var record in PendingReturns.ToList())
            {
                if (record == null)
                {
                    PendingReturns.Remove(record);
                    continue;
                }

                record.Tick();
                if (record.IsComplete)
                {
                    PendingReturns.Remove(record);
                }
            }
        }

        public static bool ScheduleReturn(Pawn pawn, Corpse storedCorpse = null)
        {
            if (!ShouldUseGreenDoor(pawn))
            {
                return false;
            }

            var mapReference = storedCorpse?.Map ?? pawn.MapHeld;
            var existingRecord = PendingReturns.FirstOrDefault(record => record?.Pawn == pawn);
            if (existingRecord != null)
            {
                existingRecord.StoredCorpse ??= storedCorpse;
                existingRecord.MapReference ??= mapReference;
                return false;
            }

            PendingReturns.Add(new HulkGreenDoorReturnRecord
            {
                Pawn = pawn,
                StoredCorpse = storedCorpse,
                MapReference = mapReference,
                scheduledManifestTick = (Find.TickManager?.TicksGame ?? 0) + HulkHealingUtility.Settings.greenDoorDelayHours * TicksPerHour
            });

            return true;
        }

        public static bool HasScheduledReturn(Pawn pawn)
        {
            return pawn != null && PendingReturns.Any(record => record?.Pawn == pawn);
        }

        public static bool TryForceManifestNow(Pawn pawn)
        {
            var record = GetRecord(pawn);
            if (record == null || record.IsComplete || record.Pawn?.Dead != true)
            {
                return false;
            }

            return record.ForceManifestNow(Find.TickManager?.TicksGame ?? 0);
        }

        public static string GetDebugStateSummary(Pawn pawn)
        {
            if (pawn == null)
            {
                return "Green Door state: no pawn selected.";
            }

            var record = GetRecord(pawn);
            if (record == null)
            {
                return $"Green Door state for {pawn.LabelShortCap}: pending=no, dead={FormatBool(pawn.Dead)}, discarded={FormatBool(pawn.Discarded)}, corpse={FormatBool(pawn.Corpse != null)}, map={GetMapLabel(pawn.MapHeld)}.";
            }

            return record.GetDebugSummary(Find.TickManager?.TicksGame ?? 0);
        }

        public static bool TryCaptureDestroyedCorpse(Corpse corpse)
        {
            var pawn = corpse?.InnerPawn;
            if (suppressCorpseCapture || corpse == null || pawn == null || !ShouldUseGreenDoor(pawn))
            {
                return false;
            }

            ScheduleReturn(pawn, corpse);
            HulkImmortalityManager.RemovePendingResurrection(pawn);

            try
            {
                var record = PendingReturns.FirstOrDefault(entry => entry?.Pawn == pawn);
                if (record != null)
                {
                    record.StoredCorpse = corpse;
                    record.MapReference ??= corpse.Map;
                }

                if (corpse.Spawned)
                {
                    corpse.DeSpawn(DestroyMode.Vanish);
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static void CancelForPawn(Pawn pawn)
        {
            if (pawn == null || PendingReturns.Count == 0)
            {
                return;
            }

            foreach (var record in PendingReturns.Where(entry => entry?.Pawn == pawn).ToList())
            {
                record.CleanupDoor();
                record.CleanupStoredCorpse();
                record.IsComplete = true;
                PendingReturns.Remove(record);
            }
        }

        private static HulkGreenDoorReturnRecord GetRecord(Pawn pawn)
        {
            return pawn == null ? null : PendingReturns.FirstOrDefault(record => record?.Pawn == pawn);
        }

        private static bool ShouldUseGreenDoor(Pawn pawn)
        {
            return pawn?.Dead == true &&
                   HulkHealingUtility.Settings.autoResurrect &&
                   HulkHealingUtility.Settings.enableGreenDoorReturn &&
                   (HulkUtility.HasBannerTrait(pawn) || HulkUtility.HasGammaIdentity(pawn));
        }

        private static bool TryManifestDoor(HulkGreenDoorReturnRecord record)
        {
            if (record?.Pawn == null)
            {
                return false;
            }

            var targetMap = record.MapReference != null && Find.Maps.Contains(record.MapReference)
                ? record.MapReference
                : ChooseReturnMap();

            if (targetMap == null)
            {
                return false;
            }

            if (!record.DoorCell.IsValid || !IsValidDoorCell(record.DoorCell, targetMap))
            {
                if (!TryFindDoorCell(targetMap, out var newDoorCell))
                {
                    return false;
                }

                record.DoorCell = newDoorCell;
            }

            if (record.GreenDoor == null || record.GreenDoor.Destroyed || record.GreenDoor.Map != targetMap)
            {
                var doorThing = ThingMaker.MakeThing(HulkDefOf.Drake_HulkGreenDoor) as HulkGreenDoorBuilding;
                if (doorThing == null)
                {
                    return false;
                }

                doorThing.AssignArrivalPawn(record.Pawn);
                doorThing.SetFaction(Faction.OfPlayer);
                record.GreenDoor = GenSpawn.Spawn(doorThing, record.DoorCell, targetMap) as HulkGreenDoorBuilding;
                if (record.GreenDoor == null)
                {
                    return false;
                }
            }

            record.MapReference = targetMap;
            TriggerAnomalyEffects(record);
            return true;
        }

        private static string FormatBool(bool value)
        {
            return value ? "yes" : "no";
        }

        private static string GetMapLabel(Map map)
        {
            return map?.Parent?.LabelCap ?? map?.ToString() ?? "none";
        }

        private static string FormatStage(HulkGreenDoorStage stage)
        {
            switch (stage)
            {
                case HulkGreenDoorStage.WaitingForManifestation:
                    return "waiting_to_manifest";
                case HulkGreenDoorStage.WaitingToOpen:
                    return "waiting_to_open";
                case HulkGreenDoorStage.WaitingToRevert:
                    return "waiting_to_revert";
                default:
                    return "unknown";
            }
        }

        private static string FormatRemainingTime(int ticks)
        {
            if (ticks <= 0)
            {
                return "ready";
            }

            return $"{ticks} ticks ({ticks / (float)TicksPerHour:0.0}h)";
        }

        private static void TriggerAnomalyEffects(HulkGreenDoorReturnRecord record)
        {
            if (record == null || record.MapReference == null || !record.DoorCell.IsValid)
            {
                return;
            }

            var weather = DefDatabase<WeatherDef>.GetNamedSilentFail("RainyThunderstorm") ??
                          DefDatabase<WeatherDef>.GetNamedSilentFail("DryThunderstorm");
            if (weather != null && WeatherManagerField != null && WeatherTransitionMethod != null)
            {
                try
                {
                    var weatherManager = WeatherManagerField.GetValue(record.MapReference);
                    if (weatherManager != null)
                    {
                        WeatherTransitionMethod.Invoke(weatherManager, new object[] { weather });
                    }
                }
                catch
                {
                }
            }

            FleckMaker.ThrowDustPuffThick(record.DoorCell.ToVector3Shifted(), record.MapReference, 4f, GreenDoorColor);
            MoteMaker.ThrowText(record.DoorCell.ToVector3Shifted(), record.MapReference, "HulkMoteGreenDoor".Translate(), GreenDoorColor);

            if (record.notificationSent)
            {
                return;
            }

            Messages.Message(
                "HulkGreenDoorManifestMessage".Translate(record.Pawn.LabelShortCap),
                new TargetInfo(record.DoorCell, record.MapReference),
                MessageTypeDefOf.PositiveEvent,
                false);

            record.notificationSent = true;
        }

        private static bool TryReturnPawn(HulkGreenDoorReturnRecord record)
        {
            if (record?.Pawn == null)
            {
                return false;
            }

            if (!TryManifestDoor(record))
            {
                return false;
            }

            var targetMap = record.MapReference;
            if (targetMap == null || record.GreenDoor == null || record.GreenDoor.Destroyed)
            {
                return false;
            }

            if (!TryFindReturnCell(targetMap, record.DoorCell, out var returnCell))
            {
                return false;
            }

            record.GreenDoor.AssignArrivalPawn(record.Pawn);
            record.GreenDoor.BeginArrival();
            if (!EnsureCorpseAvailable(record, targetMap, returnCell))
            {
                return false;
            }

            try
            {
                ResurrectionUtility.TryResurrect(record.Pawn);
            }
            catch
            {
                return false;
            }

            if (record.Pawn.Dead)
            {
                return false;
            }

            if (!record.Pawn.Spawned || record.Pawn.Map != targetMap)
            {
                if (record.Pawn.Spawned)
                {
                    record.Pawn.DeSpawn();
                }

                GenSpawn.Spawn(record.Pawn, returnCell, targetMap);
            }
            else if (record.Pawn.Position != returnCell)
            {
                record.Pawn.DeSpawn();
                GenSpawn.Spawn(record.Pawn, returnCell, targetMap);
            }

            record.Pawn.jobs?.StopAll();
            record.Pawn.pather?.StopDead();

            HulkUtility.EnsureBannerState(record.Pawn);
            HulkHealingUtility.ApplyEmergencyRecovery(record.Pawn);
            HulkUtility.TransformForEmergency(record.Pawn);

            FleckMaker.ThrowDustPuffThick(record.Pawn.DrawPos, targetMap, 5.2f, HulkUtility.HulkSkinColor);
            MoteMaker.ThrowText(record.Pawn.DrawPos, targetMap, "HulkMoteGreenDoorReturn".Translate(), HulkUtility.HulkSkinColor);
            Messages.Message("HulkGreenDoorReturnMessage".Translate(record.Pawn.LabelShortCap), record.Pawn, MessageTypeDefOf.PositiveEvent, false);
            return true;
        }

        private static void ApplyRecoveryComa(HulkGreenDoorReturnRecord record)
        {
            if (record?.Pawn == null || record.Pawn.Dead)
            {
                return;
            }

            HulkUtility.Revert(record.Pawn);

            var recovery = record.Pawn.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkRecoveryComa) ??
                           record.Pawn.health?.AddHediff(HulkDefOf.Drake_HulkRecoveryComa);
            if (recovery != null)
            {
                recovery.Severity = 1f;
                var disappears = recovery.TryGetComp<HediffComp_Disappears>();
                if (disappears != null && TicksToDisappearField != null)
                {
                    try
                    {
                        TicksToDisappearField.SetValue(disappears, HulkHealingUtility.Settings.greenDoorRecoveryComaHours * TicksPerHour);
                    }
                    catch
                    {
                    }
                }
            }

            record.Pawn.jobs?.StopAll();
            Messages.Message("HulkGreenDoorRecoveryMessage".Translate(record.Pawn.LabelShortCap), record.Pawn, MessageTypeDefOf.PositiveEvent, false);
        }

        private static Map ChooseReturnMap()
        {
            return Find.Maps.FirstOrDefault(map => map != null && map.IsPlayerHome) ?? Find.CurrentMap;
        }

        private static bool TryFindDoorCell(Map map, out IntVec3 cell)
        {
            if (map == null)
            {
                cell = IntVec3.Invalid;
                return false;
            }

            return CellFinder.TryRandomClosewalkCellNear(
                map.Center,
                map,
                32,
                out cell,
                candidate => IsValidDoorCell(candidate, map));
        }

        private static bool TryFindReturnCell(Map map, IntVec3 nearCell, out IntVec3 cell)
        {
            foreach (var offset in GenAdj.CardinalDirections)
            {
                var candidate = nearCell + offset;
                if (IsValidReturnCell(candidate, map))
                {
                    cell = candidate;
                    return true;
                }
            }

            return CellFinder.TryRandomClosewalkCellNear(
                nearCell,
                map,
                8,
                out cell,
                candidate => IsValidReturnCell(candidate, map));
        }

        private static bool IsValidDoorCell(IntVec3 cell, Map map)
        {
            return IsValidReturnCell(cell, map) && cell.GetEdifice(map) == null;
        }

        private static bool IsValidReturnCell(IntVec3 cell, Map map)
        {
            return map != null &&
                   cell.IsValid &&
                   cell.InBounds(map) &&
                   cell.Standable(map) &&
                   !cell.Fogged(map) &&
                   !cell.GetThingList(map).OfType<Pawn>().Any();
        }

        private static bool EnsureCorpseAvailable(HulkGreenDoorReturnRecord record, Map map, IntVec3 cell)
        {
            var pawn = record?.Pawn;
            if (pawn == null || map == null || !cell.IsValid)
            {
                return false;
            }

            try
            {
                var corpse = record.StoredCorpse ?? pawn.Corpse;
                if (corpse == null)
                {
                    var generatedCorpse = ThingMaker.MakeThing(pawn.RaceProps.corpseDef) as Corpse;
                    if (generatedCorpse == null)
                    {
                        return false;
                    }

                    generatedCorpse.InnerPawn = pawn;
                    record.StoredCorpse = generatedCorpse;
                    GenSpawn.Spawn(generatedCorpse, cell, map);
                    return true;
                }

                if (!corpse.Spawned || corpse.Map != map)
                {
                    if (corpse.Spawned)
                    {
                        corpse.DeSpawn();
                    }

                    GenSpawn.Spawn(corpse, cell, map);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal sealed class HulkGreenDoorReturnRecord : IExposable
        {
            public Pawn Pawn;
            public Corpse StoredCorpse;
            public Map MapReference;
            public HulkGreenDoorBuilding GreenDoor;
            public IntVec3 DoorCell = IntVec3.Invalid;
            public int scheduledManifestTick;
            public int openDoorTick;
            public int revertToHumanTick;
            public int cleanupDoorTick;
            public bool notificationSent;
            public bool IsComplete;

            private HulkGreenDoorStage stage = HulkGreenDoorStage.WaitingForManifestation;

            public void ExposeData()
            {
                Scribe_References.Look(ref Pawn, "pawn");
                Scribe_References.Look(ref StoredCorpse, "storedCorpse");
                Scribe_References.Look(ref MapReference, "mapReference");
                Scribe_References.Look(ref GreenDoor, "greenDoor");
                Scribe_Values.Look(ref DoorCell, "doorCell");
                Scribe_Values.Look(ref scheduledManifestTick, "scheduledManifestTick", 0);
                Scribe_Values.Look(ref openDoorTick, "openDoorTick", 0);
                Scribe_Values.Look(ref revertToHumanTick, "revertToHumanTick", 0);
                Scribe_Values.Look(ref cleanupDoorTick, "cleanupDoorTick", 0);
                Scribe_Values.Look(ref notificationSent, "notificationSent", false);
                Scribe_Values.Look(ref IsComplete, "isComplete", false);
                Scribe_Values.Look(ref stage, "stage", HulkGreenDoorStage.WaitingForManifestation);
            }

            public bool ForceManifestNow(int currentTick)
            {
                switch (stage)
                {
                    case HulkGreenDoorStage.WaitingForManifestation:
                        if (!TryManifestDoor(this))
                        {
                            return false;
                        }

                        openDoorTick = currentTick;
                        stage = HulkGreenDoorStage.WaitingToOpen;
                        return true;

                    case HulkGreenDoorStage.WaitingToOpen:
                        openDoorTick = currentTick;
                        return true;

                    default:
                        return false;
                }
            }

            public string GetDebugSummary(int currentTick)
            {
                return $"Green Door state for {Pawn?.LabelShortCap ?? "unknown"}: pending=yes, stage={FormatStage(stage)}, dead={FormatBool(Pawn?.Dead == true)}, discarded={FormatBool(Pawn?.Discarded == true)}, storedCorpse={FormatBool(StoredCorpse != null && !StoredCorpse.Destroyed)}, door={FormatBool(GreenDoor != null && !GreenDoor.Destroyed)}, map={GetMapLabel(MapReference)}, manifestIn={FormatRemainingTime(GetTicksUntilManifest(currentTick))}, openIn={FormatRemainingTime(GetTicksUntilOpen(currentTick))}, revertIn={FormatRemainingTime(GetTicksUntilRevert(currentTick))}.";
            }

            public void Tick()
            {
                if (IsComplete)
                {
                    CleanupDoor();
                    CleanupStoredCorpse();
                    return;
                }

                if (IsPawnPermanentlyGone(Pawn))
                {
                    CleanupDoor();
                    CleanupStoredCorpse();
                    IsComplete = true;
                    return;
                }

                if (!Pawn.Dead && stage != HulkGreenDoorStage.WaitingToRevert)
                {
                    CleanupDoor();
                    CleanupStoredCorpse();
                    IsComplete = true;
                    return;
                }

                var currentTick = Find.TickManager?.TicksGame ?? 0;
                switch (stage)
                {
                    case HulkGreenDoorStage.WaitingForManifestation:
                        if (currentTick < scheduledManifestTick)
                        {
                            return;
                        }

                        if (!TryManifestDoor(this))
                        {
                            return;
                        }

                        openDoorTick = currentTick + HulkHealingUtility.Settings.greenDoorDoorOpenDelayHours * TicksPerHour;
                        stage = HulkGreenDoorStage.WaitingToOpen;
                        return;

                    case HulkGreenDoorStage.WaitingToOpen:
                        if (currentTick < openDoorTick)
                        {
                            return;
                        }

                        if (!TryReturnPawn(this))
                        {
                            return;
                        }

                        revertToHumanTick = currentTick + HulkReturnDurationTicks;
                        cleanupDoorTick = currentTick + DoorCleanupDelayTicks;
                        stage = HulkGreenDoorStage.WaitingToRevert;
                        return;

                    case HulkGreenDoorStage.WaitingToRevert:
                        if (currentTick >= cleanupDoorTick)
                        {
                            CleanupDoor();
                        }

                        if (Pawn.Dead)
                        {
                            CleanupDoor();
                            CleanupStoredCorpse();
                            IsComplete = true;
                            return;
                        }

                        if (currentTick < revertToHumanTick)
                        {
                            return;
                        }

                        ApplyRecoveryComa(this);
                        CleanupDoor();
                        CleanupStoredCorpse();
                        IsComplete = true;
                        return;
                }
            }

            private int GetTicksUntilManifest(int currentTick)
            {
                return stage == HulkGreenDoorStage.WaitingForManifestation
                    ? Mathf.Max(0, scheduledManifestTick - currentTick)
                    : 0;
            }

            private int GetTicksUntilOpen(int currentTick)
            {
                switch (stage)
                {
                    case HulkGreenDoorStage.WaitingForManifestation:
                        return Mathf.Max(0, (scheduledManifestTick + (HulkHealingUtility.Settings.greenDoorDoorOpenDelayHours * TicksPerHour)) - currentTick);
                    case HulkGreenDoorStage.WaitingToOpen:
                        return Mathf.Max(0, openDoorTick - currentTick);
                    default:
                        return 0;
                }
            }

            private int GetTicksUntilRevert(int currentTick)
            {
                switch (stage)
                {
                    case HulkGreenDoorStage.WaitingForManifestation:
                        return Mathf.Max(0, (scheduledManifestTick + (HulkHealingUtility.Settings.greenDoorDoorOpenDelayHours * TicksPerHour) + HulkReturnDurationTicks) - currentTick);
                    case HulkGreenDoorStage.WaitingToOpen:
                        return Mathf.Max(0, (openDoorTick + HulkReturnDurationTicks) - currentTick);
                    case HulkGreenDoorStage.WaitingToRevert:
                        return Mathf.Max(0, revertToHumanTick - currentTick);
                    default:
                        return 0;
                }
            }

            public void CleanupDoor()
            {
                try
                {
                    if (GreenDoor != null && !GreenDoor.Destroyed)
                    {
                        GreenDoor.Destroy(DestroyMode.Vanish);
                    }
                }
                catch
                {
                }

                GreenDoor = null;
            }

            public void CleanupStoredCorpse()
            {
                try
                {
                    if (StoredCorpse != null && !StoredCorpse.Destroyed)
                    {
                        suppressCorpseCapture = true;
                        StoredCorpse.Destroy(DestroyMode.Vanish);
                    }
                }
                catch
                {
                }
                finally
                {
                    suppressCorpseCapture = false;
                }

                StoredCorpse = null;
            }
        }

        private enum HulkGreenDoorStage
        {
            WaitingForManifestation,
            WaitingToOpen,
            WaitingToRevert
        }
    }

    public sealed class HulkGreenDoorBuilding : Building_Door
    {
        private Pawn arrivalPawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref arrivalPawn, "arrivalPawn");
        }

        public void AssignArrivalPawn(Pawn pawn)
        {
            arrivalPawn = pawn;
        }

        public void BeginArrival()
        {
            DoorOpen(Math.Max(TicksToOpenNow + 120, 300));
        }

        public override bool PawnCanOpen(Pawn pawn)
        {
            return pawn == arrivalPawn && base.PawnCanOpen(pawn);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            yield break;
        }
    }
}
