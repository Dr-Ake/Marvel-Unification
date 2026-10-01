using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public class DupeLifecycleData : IExposable
    {
        public Pawn? Original;
        public Pawn? Dupe;
        public int SpawnTick;
        private int expirationTick = -1;
        private bool promoted;
        private readonly HashSet<int> phantomThingIds = new();
        private List<int>? phantomIdsCache;

        public bool IsActive => !promoted && Dupe != null && Dupe.Spawned && !Dupe.Destroyed;

        public void Initialize(Pawn dupe, Pawn original, float durationTicks)
        {
            Dupe = dupe;
            Original = original;
            SpawnTick = Find.TickManager.TicksGame;
            if (durationTicks > 0)
            {
                expirationTick = SpawnTick + (int)durationTicks;
            }
            else
            {
                expirationTick = -1;
            }
        }

        public void Tick()
        {
            if (!IsActive || Dupe == null)
            {
                return;
            }

            if (Dupe.Drafted || (Dupe.jobs?.curJob?.playerForced ?? false))
            {
                return;
            }

            if (expirationTick > 0 && Find.TickManager.TicksGame >= expirationTick)
            {
                DupeManager.Current.ForceDespawn(this, DupeDespawnReason.TimerExpired);
                return;
            }

            if (DupeSettingsManager.Settings.VanishWhenOriginalSleeps && Original != null && Original.Spawned)
            {
                if (Original.CurJobDef == JobDefOf.LayDown && Original.jobs?.curDriver?.asleep == true)
                {
                    DupeManager.Current.ForceDespawn(this, DupeDespawnReason.MasterSleeping);
                }
            }
        }

        public void RegisterPhantomThing(Thing thing)
        {
            phantomThingIds.Add(thing.thingIDNumber);
        }

        public bool IsPhantom(Thing thing)
        {
            return phantomThingIds.Contains(thing.thingIDNumber);
        }

        public void NotifyThingRemoved(Thing thing)
        {
            phantomThingIds.Remove(thing.thingIDNumber);
        }

        public void DropNonPhantomGear()
        {
            if (Dupe == null)
            {
                return;
            }

            if (Dupe.apparel != null)
            {
                var buffer = new List<Apparel>(Dupe.apparel.WornApparel);
                foreach (var apparel in buffer)
                {
                    if (IsPhantom(apparel))
                    {
                        Dupe.apparel.Remove(apparel);
                        apparel.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Dupe.apparel.TryDrop(apparel, out _, Dupe.PositionHeld, forbid: false);
                    }
                }
            }

            if (Dupe.equipment != null)
            {
                var buffer = new List<ThingWithComps>(Dupe.equipment.AllEquipmentListForReading);
                foreach (var eq in buffer)
                {
                    if (IsPhantom(eq))
                    {
                        Dupe.equipment.Remove(eq);
                        eq.Destroy(DestroyMode.Vanish);
                    }
                    else
                    {
                        Dupe.equipment.TryDropEquipment(eq, out _, Dupe.PositionHeld, forbid: false);
                    }
                }
            }

            if (Dupe.inventory != null)
            {
                var buffer = new List<Thing>(Dupe.inventory.innerContainer);
                foreach (var thing in buffer)
                {
                    Dupe.inventory.innerContainer.TryDrop(thing, Dupe.PositionHeld, Dupe.MapHeld, ThingPlaceMode.Near, out _);
                }
            }
        }

        public void Promote()
        {
            promoted = true;
            phantomThingIds.Clear();
            Original = null;
            expirationTick = -1;
        }

        public void RebindOriginal(Pawn newOriginal)
        {
            Original = newOriginal;
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref Original, nameof(Original));
            Scribe_References.Look(ref Dupe, nameof(Dupe));
            Scribe_Values.Look(ref SpawnTick, nameof(SpawnTick));
            Scribe_Values.Look(ref expirationTick, nameof(expirationTick), -1);
            Scribe_Values.Look(ref promoted, nameof(promoted));

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                phantomIdsCache ??= new List<int>();
                phantomIdsCache.Clear();
                foreach (var id in phantomThingIds)
                {
                    phantomIdsCache.Add(id);
                }
            }

            Scribe_Collections.Look(ref phantomIdsCache, nameof(phantomIdsCache), LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && phantomIdsCache != null)
            {
                phantomThingIds.Clear();
                foreach (var id in phantomIdsCache)
                {
                    phantomThingIds.Add(id);
                }
            }
        }
    }
}
