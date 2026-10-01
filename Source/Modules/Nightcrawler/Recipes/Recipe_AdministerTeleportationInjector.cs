using System.Collections.Generic;
using RimWorld;
using Verse;

namespace NightcrawlerTeleportation
{
    public class Recipe_AdministerTeleportationInjector : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            if (pawn?.health == null)
            {
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(NightcrawlerDefOf.Nightcrawler_Teleportation))
            {
                pawn.health.AddHediff(NightcrawlerDefOf.Nightcrawler_Teleportation);
            }

            if (billDoer != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            return false;
        }
    }
}
