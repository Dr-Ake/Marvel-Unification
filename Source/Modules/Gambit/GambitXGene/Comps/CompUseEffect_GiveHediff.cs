using RimWorld;
using Verse;

namespace GambitXGene.Comps;

public class CompProperties_UseEffectGiveHediff : CompProperties_UseEffect
{
    public HediffDef hediffDef = null!;
    public float severity = 1f;

    public CompProperties_UseEffectGiveHediff()
    {
        compClass = typeof(CompUseEffect_GiveHediff);
    }
}

public class CompUseEffect_GiveHediff : MarvelUnification.OrganicHediffUseEffect
{
        protected override HediffDef GrantedHediff => Props.hediffDef;

    private CompProperties_UseEffectGiveHediff Props => (CompProperties_UseEffectGiveHediff)props;

    public override void DoEffect(Pawn usedBy)
    {
        base.DoEffect(usedBy);

        if (usedBy?.health == null || Props.hediffDef == null)
        {
            return;
        }

        var race = usedBy.RaceProps;
        if (race == null || !(race.Humanlike || race.Animal))
        {
            Messages.Message("Only organic pawns can use this injector.", usedBy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        var existing = usedBy.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
        if (existing != null)
        {
            Messages.Message("Target already carries this graft.", usedBy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }

        var hediff = HediffMaker.MakeHediff(Props.hediffDef, usedBy);
        hediff.Severity = Props.severity;
        usedBy.health.AddHediff(hediff);
        Messages.Message("Injector administered.", usedBy, MessageTypeDefOf.PositiveEvent);
    }
}
