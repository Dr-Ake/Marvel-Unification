using System.Collections.Generic;
using RimWorld;
using Verse;

namespace CyclopsGene
{
    public class Recipe_AdministerCyclopsInjector : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
            }

            if (pawn == null || pawn.health == null)
            {
                return;
            }

            HediffDef cyclopsGeneDef = DefDatabase<HediffDef>.GetNamed("Cyclops_Gene", true);

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(cyclopsGeneDef);
            if (existing == null)
            {
                pawn.health.AddHediff(cyclopsGeneDef);
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
