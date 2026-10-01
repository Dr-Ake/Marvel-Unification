using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GambitXGene.Recipes;

public class Recipe_AdministerGambitInjector : Recipe_Surgery
{
    public override void ApplyOnPawn(Pawn pawn, BodyPartRecord? part, Pawn? billDoer, List<Thing>? ingredients, Bill bill)
    {
        if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
        {
            return;
        }

        if (pawn?.health == null || pawn.health.hediffSet == null)
        {
            return;
        }

        if (!pawn.health.hediffSet.HasHediff(GambitDefOf.Gambit_XGene_Hediff))
        {
            pawn.health.AddHediff(GambitDefOf.Gambit_XGene_Hediff);
        }

        if (billDoer != null)
        {
            TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
        }
    }

    public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord? part, Faction? billDoerFaction)
    {
        return false;
    }
}
