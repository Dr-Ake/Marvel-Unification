using RimWorld;
using Verse;

namespace GambitXGene.Comps;

public class HediffCompProperties_GambitGeneLink : HediffCompProperties
{
    public HediffCompProperties_GambitGeneLink()
    {
        compClass = typeof(HediffComp_GambitGeneLink);
    }
}

public class HediffComp_GambitGeneLink : HediffComp
{
    public HediffCompProperties_GambitGeneLink Props => (HediffCompProperties_GambitGeneLink)props;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        EnsureGene();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (parent.pawn.IsHashIntervalTick(60))
        {
            EnsureGene();
        }
    }

    private void EnsureGene()
    {
        var pawn = parent.pawn;
        if (pawn?.genes == null || GambitDefOf.Gambit_XGene == null)
        {
            return;
        }

        if (pawn.genes.GetGene(GambitDefOf.Gambit_XGene) == null)
        {
            pawn.genes.AddGene(GambitDefOf.Gambit_XGene, xenogene: true);
        }
    }
}
