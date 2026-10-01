using System;
using RimWorld;
using Verse;

namespace MagnetoXGene
{
    public class CompProperties_UseEffectGiveMagnetism : CompProperties_UseEffect
    {
        public HediffDef hediffDef;
        public float severity = 1f;

        public CompProperties_UseEffectGiveMagnetism()
        {
            compClass = typeof(CompUseEffectGiveMagnetism);
        }
    }

    public class CompUseEffectGiveMagnetism : MarvelUnification.OrganicHediffUseEffect
    {
        protected override HediffDef GrantedHediff => Props.hediffDef;

        public CompProperties_UseEffectGiveMagnetism Props => (CompProperties_UseEffectGiveMagnetism)props;

        public override void DoEffect(Pawn user)
        {
            base.DoEffect(user);

            if (user == null || user.health == null || Props.hediffDef == null)
            {
                return;
            }

            Hediff existing = user.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (existing != null)
            {
                existing.Severity = Math.Max(existing.Severity, Props.severity);
            }
            else
            {
                Hediff hediff = HediffMaker.MakeHediff(Props.hediffDef, user);
                hediff.Severity = Props.severity;
                user.health.AddHediff(hediff);
            }

            if (PawnUtility.ShouldSendNotificationAbout(user))
            {
                Messages.Message("Magneto_Message_GeneInjected".Translate(user.LabelShortCap), user, MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
