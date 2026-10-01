using System;
using RimWorld;
using Verse;

namespace CyclopsGene
{
    public class CompProperties_UseEffectGiveCyclopsGene : CompProperties_UseEffect
    {
        public HediffDef hediffDef;
        public float severity = 1f;

        public CompProperties_UseEffectGiveCyclopsGene()
        {
            compClass = typeof(CompUseEffectGiveCyclopsGene);
        }
    }

    public class CompUseEffectGiveCyclopsGene : MarvelUnification.OrganicHediffUseEffect
    {
        protected override HediffDef GrantedHediff => Props.hediffDef;

        private CompProperties_UseEffectGiveCyclopsGene Props
        {
            get
            {
                return (CompProperties_UseEffectGiveCyclopsGene)props;
            }
        }

        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);

            if (usedBy == null || usedBy.health == null || Props.hediffDef == null)
            {
                return;
            }

            // Allow humanlikes and animals
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
    }
}
