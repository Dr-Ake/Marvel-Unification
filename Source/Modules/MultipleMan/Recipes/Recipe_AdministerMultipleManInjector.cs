using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public class Recipe_AdministerMultipleManInjector : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            if (pawn == null)
            {
                return;
            }

            var addedGene = false;
            if (DupeSettingsManager.UseGeneMode)
            {
                if (pawn.genes == null)
                {
                    addedGene = false;
                }
                else if (!pawn.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))
                {
                    pawn.genes.AddGene(MMDefOf.MM_MultipleManGene, xenogene: true);
                    addedGene = true;
                }
            }

            if (!addedGene)
            {
                DupeUtility.EnsureGeneMarkerHediff(pawn);
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
