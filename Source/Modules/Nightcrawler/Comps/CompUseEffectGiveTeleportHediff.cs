using RimWorld;
using Verse;

namespace NightcrawlerTeleportation
{
    /// <summary>
    /// Applies the teleportation hediff when the injector is used.
    /// </summary>
    public class CompProperties_UseEffectGiveTeleportHediff : CompProperties_UseEffect
    {
        public HediffDef hediffDef;
        public float severity = 1f;

        public CompProperties_UseEffectGiveTeleportHediff()
        {
            compClass = typeof(CompUseEffectGiveTeleportHediff);
        }
    }

    public class CompUseEffectGiveTeleportHediff : MarvelUnification.OrganicHediffUseEffect
    {
        protected override HediffDef GrantedHediff => Props.hediffDef;

        private CompProperties_UseEffectGiveTeleportHediff Props => (CompProperties_UseEffectGiveTeleportHediff)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (usedBy?.health == null || Props.hediffDef == null)
            {
                return;
            }

            var race = usedBy.RaceProps;
            if (race == null || (!race.Humanlike && !race.Animal))
            {
                Messages.Message("NightcrawlerTeleportation.InjectorInvalidPawn".Translate(), usedBy, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            if (usedBy.health.hediffSet.HasHediff(Props.hediffDef))
            {
                Messages.Message("NightcrawlerTeleportation.InjectorAlreadyHas".Translate(Props.hediffDef.LabelCap), usedBy, MessageTypeDefOf.NeutralEvent);
                return;
            }

            Hediff newHediff = usedBy.health.AddHediff(Props.hediffDef);
            if (newHediff != null)
            {
                newHediff.Severity = Props.severity;
            }

            Messages.Message("NightcrawlerTeleportation.InjectorApplied".Translate(Props.hediffDef.LabelCap), usedBy, MessageTypeDefOf.PositiveEvent);
        }
    }
}
