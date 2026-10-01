using System;
using RimWorld;
using Verse;

namespace DeadpoolsHealingFactor
{
    /// <summary>
    /// Comp properties for applying a specific hediff to the user when the item is used.
    /// </summary>
    public class CompProperties_UseEffectGiveHediffDP : CompProperties_UseEffect
    {
        public HediffDef hediffDef;
        public float severity = 1f;

        public CompProperties_UseEffectGiveHediffDP()
        {
            compClass = typeof(CompUseEffectGiveHediffDP);
        }
    }

    /// <summary>
    /// Applies the configured hediff to the user when the item is used via CompUsable.
    /// </summary>
    public class CompUseEffectGiveHediffDP : MarvelUnification.OrganicHediffUseEffect
    {
        protected override HediffDef GrantedHediff => Props.hediffDef;

        private CompProperties_UseEffectGiveHediffDP Props => (CompProperties_UseEffectGiveHediffDP)props;

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (usedBy == null || usedBy.health == null || Props.hediffDef == null)
            {
                return;
            }

            // Allow humanlikes and animals; reject mechanoids and other non-organic pawns.
            bool isEligible = usedBy.RaceProps != null && (usedBy.RaceProps.Humanlike || usedBy.RaceProps.Animal);
            if (!isEligible)
            {
                Messages.Message("Only living flesh pawns can use this injector.".Translate(), usedBy, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            Hediff existing = usedBy.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (existing == null)
            {
                usedBy.health.AddHediff(Props.hediffDef);
                Messages.Message("Injected: " + Props.hediffDef.LabelCap, usedBy, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("Already has: " + Props.hediffDef.LabelCap, usedBy, MessageTypeDefOf.NeutralEvent);
            }
        }

        // Rely on DoEffect to perform eligibility checks to avoid API differences across game versions.
    }
}


