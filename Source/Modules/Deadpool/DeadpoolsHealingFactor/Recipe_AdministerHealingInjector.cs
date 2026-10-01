using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DeadpoolsHealingFactor
{
    public class DP_Recipe_AdministerHealingInjector : Recipe_Surgery
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

            Hediff existing = pawn.health.hediffSet.GetFirstHediffOfDef(DPDefOf.DP_HealingFactor);
            if (existing == null)
            {
                pawn.health.AddHediff(DPDefOf.DP_HealingFactor);
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


