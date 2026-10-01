using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public class DupeManager : GameComponent
    {
        private List<DupeRecord> records = new();
        private readonly Dictionary<int, DupeLifecycleData> dupeLookup = new();
        private int nextLineageId = 1;

        public DupeManager(Game game)
        {
        }

        public static DupeManager Current
        {
            get
            {
                var game = Verse.Current.Game;
                if (game == null)
                {
                    throw new InvalidOperationException("No active RimWorld game available for DupeManager");
                }

                var component = game.GetComponent<DupeManager>();
                if (component != null)
                {
                    return component;
                }

                component = new DupeManager(game);
                game.components.Add(component);
                return component;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref records, nameof(records), LookMode.Deep);
            Scribe_Values.Look(ref nextLineageId, nameof(nextLineageId), 1);
            if (records == null)
            {
                records = new List<DupeRecord>();
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureLineageIds();
                RebuildLookup();
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 250 != 0)
            {
                return;
            }

            var recordSnapshot = records.ToArray();
            foreach (var record in recordSnapshot)
            {
                if (record == null)
                {
                    continue;
                }

                var dupesSnapshot = record.Dupes.ToArray();
                foreach (var dupe in dupesSnapshot)
                {
                    dupe?.Tick();
                }

                record.Cleanup();
            }

            records.RemoveAll(r => r == null || (!r.HasOriginal && r.ActiveDupeCount == 0));
            RebuildLookup();
        }

        public bool CanSummon(Pawn pawn, out string? reason)
        {
            reason = null;
            if (pawn == null || pawn.Dead || pawn.health?.Downed == true)
            {
                reason = "Pawn cannot summon while dead/downed";
                return false;
            }

            var record = FindRecordForPawn(pawn, out var lifecycleData);
            if (lifecycleData != null && lifecycleData.IsActive)
            {
                reason = "Dupes cannot summon other dupes";
                return false;
            }

            if (record != null && record.Original != pawn)
            {
                reason = "Only the active prime pawn can summon dupes";
                return false;
            }

            record ??= GetOrCreateRecord(pawn);
            if (!DupeSettingsManager.UnlimitedDupes)
            {
                var max = DupeSettingsManager.Settings.MaxActiveDupes;
                if (record.ActiveDupeCount >= max)
                {
                    reason = "Active dupe cap reached";
                    return false;
                }
            }

            var cooldownTicks = (int)(DupeSettingsManager.Settings.SummonCooldownHours * GenDate.TicksPerHour);
            if (cooldownTicks > 0 && Find.TickManager.TicksGame - record.LastSummonTick < cooldownTicks)
            {
                reason = "Summon cooldown";
                return false;
            }

            return true;
        }

        public bool HasActiveDupe(Pawn pawn)
        {
            var record = GetRecord(pawn);
            return record != null && record.ActiveDupeCount > 0;
        }

        public bool TryDismissOldestDupe(Pawn pawn)
        {
            var record = GetRecord(pawn);
            if (record == null)
            {
                return false;
            }

            var oldest = record.GetOldestDupe();
            if (oldest == null)
            {
                return false;
            }

            ForceDespawn(oldest, DupeDespawnReason.Manual);
            return true;
        }

        public DupeLifecycleData RegisterDupe(Pawn original, Pawn dupe)
        {
            var record = GetOrCreateRecord(original);
            var data = new DupeLifecycleData();
            data.Initialize(dupe, original, DupeSettingsManager.DurationInTicks);
            record.AddDupe(data);
            record.LastSummonTick = Find.TickManager.TicksGame;
            dupeLookup[dupe.thingIDNumber] = data;
            return data;
        }

        public void ForceDespawn(DupeLifecycleData data, DupeDespawnReason reason)
        {
            if (data == null || data.Dupe == null)
            {
                return;
            }

            data.DropNonPhantomGear();
            var pawn = data.Dupe;
            dupeLookup.Remove(pawn.thingIDNumber);
            pawn.Destroy(DestroyMode.Vanish);
            FullyErasePawn(pawn);
            NotifyDupeRemoved(pawn);
        }

        public bool TryGetData(Pawn pawn, out DupeLifecycleData data)
        {
            if (pawn != null && dupeLookup.TryGetValue(pawn.thingIDNumber, out data))
            {
                return true;
            }

            data = null!;
            return false;
        }

        public bool IsRegisteredDupe(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (dupeLookup.TryGetValue(pawn.thingIDNumber, out var data))
            {
                return data.IsActive;
            }

            FindRecordForPawn(pawn, out var lifecycleData);
            if (lifecycleData != null && lifecycleData.IsActive)
            {
                dupeLookup[pawn.thingIDNumber] = lifecycleData;
                return true;
            }

            return false;
        }

        public void NotifyDupeRemoved(Pawn pawn)
        {
            dupeLookup.Remove(pawn.thingIDNumber);
            foreach (var record in records)
            {
                if (record.RemoveDupe(pawn))
                {
                    break;
                }
            }
        }

        public void NotifyOriginalDeath(Pawn original)
        {
            if (original == null)
            {
                return;
            }

            var record = GetRecord(original);
            if (record == null)
            {
                return;
            }

            if (!DupeSettingsManager.PromoteDupeOnDeath)
            {
                if (record.ActiveDupeCount == 0)
                {
                    records.Remove(record);
                }
                return;
            }

            var promoteData = record.GetOldestDupe();
            if (promoteData != null)
            {
                PromoteDupe(record, promoteData);
            }
            else
            {
                records.Remove(record);
            }
        }

        private void PromoteDupe(DupeRecord record, DupeLifecycleData data)
        {
            var pawn = data.Dupe;
            if (pawn == null)
            {
                return;
            }

            data.Promote();
            record.RemoveData(data);
            dupeLookup.Remove(pawn.thingIDNumber);

            pawn.kindDef = PawnKindDefOf.Colonist;
            pawn.needs?.AddOrRemoveNeedsAsAppropriate();
            pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();

            record.ReplaceOriginal(pawn);
        }

        private void RebuildLookup()
        {
            dupeLookup.Clear();
            foreach (var record in records)
            {
                foreach (var data in record.Dupes)
                {
                    if (data?.Dupe != null)
                    {
                        dupeLookup[data.Dupe.thingIDNumber] = data;
                    }
                }
            }
        }

        private void EnsureLineageIds()
        {
            foreach (var record in records)
            {
                if (record.LineageId <= 0)
                {
                    record.LineageId = nextLineageId++;
                }
            }
        }

        private DupeRecord GetOrCreateRecord(Pawn original)
        {
            var record = GetRecord(original);
            if (record != null)
            {
                return record;
            }

            record = new DupeRecord { Original = original, LineageId = nextLineageId++ };
            records.Add(record);
            return record;
        }

        private DupeRecord? GetRecord(Pawn pawn)
        {
            foreach (var record in records)
            {
                if (record.Original == pawn)
                {
                    return record;
                }
            }

            return null;
        }

        private DupeRecord? FindRecordForPawn(Pawn pawn, out DupeLifecycleData? lifecycleData)
        {
            lifecycleData = null;
            foreach (var record in records)
            {
                if (record.Original == pawn)
                {
                    return record;
                }

                foreach (var data in record.Dupes)
                {
                    if (data?.Dupe == pawn)
                    {
                        lifecycleData = data;
                        return record;
                    }
                }

                if (record.WasOriginal(pawn))
                {
                    return record;
                }
            }

            return null;
        }

        private void FullyErasePawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            try
            {
                RemoveFromWorldPawns(pawn);
                ClearRelations(pawn);
                ClearPlayLogEntries(pawn);
            }
            catch (Exception ex)
            {
                Log.Warning($"[MultipleManXGene] Failed to fully erase dupe '{pawn?.LabelShortCap ?? "null"}': {ex}");
            }
        }

        private void RemoveFromWorldPawns(Pawn pawn)
        {
            var worldPawns = Find.WorldPawns;
            if (worldPawns == null)
            {
                return;
            }

            // Only call RemovePawn if the pawn is actually tracked by WorldPawns to avoid error spam.
            if (worldPawns.AllPawnsAliveOrDead.Contains(pawn))
            {
                try
                {
                    worldPawns.RemovePawn(pawn);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[MultipleManXGene] RemovePawn failed for dupe '{pawn?.LabelShortCap ?? "null"}': {ex}");
                }
            }
        }

        private void ClearRelations(Pawn pawn)
        {
            if (pawn.relations != null)
            {
                var ownRelations = pawn.relations.DirectRelations?.ToList();
                if (ownRelations != null)
                {
                    foreach (var relation in ownRelations)
                    {
                        pawn.relations.RemoveDirectRelation(relation.def, relation.otherPawn);
                    }
                }
            }

            foreach (var other in PawnsFinder.All_AliveOrDead.ToList())
            {
                if (other == null || other == pawn || other.relations == null)
                {
                    continue;
                }

                var relations = other.relations.DirectRelations?.ToList();
                if (relations == null)
                {
                    continue;
                }

                foreach (var relation in relations)
                {
                    if (relation.otherPawn == pawn)
                    {
                        other.relations.RemoveDirectRelation(relation.def, relation.otherPawn);
                    }
                }
            }
        }

        private void ClearPlayLogEntries(Pawn pawn)
        {
            var playLog = Find.PlayLog;
            if (playLog == null)
            {
                return;
            }

            var field = typeof(PlayLog).GetField("allEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(playLog) is List<LogEntry> entries)
            {
                entries.RemoveAll(e => e != null && e.Concerns(pawn));
            }
        }
    }

    public class DupeRecord : IExposable
    {
        public Pawn? Original;
        public int LineageId;
        public int LastSummonTick;
        public List<DupeLifecycleData> Dupes = new();
        public List<int> PastOriginalIds = new();

        public int ActiveDupeCount
        {
            get
            {
                var count = 0;
                foreach (var data in Dupes)
                {
                    if (data != null && data.IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasOriginal => Original != null && !Original.Destroyed;
        public bool WasOriginal(Pawn pawn) => pawn != null && PastOriginalIds.Contains(pawn.thingIDNumber);

        public void AddDupe(DupeLifecycleData data)
        {
            Dupes.Add(data);
        }

        public bool RemoveDupe(Pawn pawn)
        {
            for (int i = 0; i < Dupes.Count; i++)
            {
                var data = Dupes[i];
                if (data?.Dupe == pawn)
                {
                    Dupes.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void RemoveData(DupeLifecycleData data)
        {
            Dupes.Remove(data);
        }

        public void Cleanup()
        {
            Dupes.RemoveAll(d => d == null || d.Dupe == null || d.Dupe.Destroyed);
            if (Original != null && Original.Destroyed)
            {
                AddPastOriginalId(Original.thingIDNumber);
                Original = null;
            }
        }

        public void ReplaceOriginal(Pawn pawn)
        {
            if (Original != null && Original != pawn)
            {
                AddPastOriginalId(Original.thingIDNumber);
            }

            Original = pawn;
            foreach (var data in Dupes)
            {
                data?.RebindOriginal(pawn);
            }
        }

        private void AddPastOriginalId(int pawnId)
        {
            if (!PastOriginalIds.Contains(pawnId))
            {
                PastOriginalIds.Add(pawnId);
            }
        }

        public DupeLifecycleData? GetOldestDupe()
        {
            DupeLifecycleData? oldest = null;
            var bestTick = int.MaxValue;
            foreach (var data in Dupes)
            {
                if (data == null || data.Dupe == null || !data.IsActive)
                {
                    continue;
                }

                if (data.SpawnTick < bestTick)
                {
                    oldest = data;
                    bestTick = data.SpawnTick;
                }
            }

            return oldest;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Original, nameof(Original));
            Scribe_Values.Look(ref LineageId, nameof(LineageId));
            Scribe_Values.Look(ref LastSummonTick, nameof(LastSummonTick));
            Scribe_Collections.Look(ref Dupes, nameof(Dupes), LookMode.Deep);
            Scribe_Collections.Look(ref PastOriginalIds, nameof(PastOriginalIds), LookMode.Value);
            Dupes ??= new List<DupeLifecycleData>();
            PastOriginalIds ??= new List<int>();
        }
    }
}
