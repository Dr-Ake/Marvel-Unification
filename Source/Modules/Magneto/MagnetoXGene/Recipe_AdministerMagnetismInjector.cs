using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MagnetoXGene
{
    public class Recipe_AdministerMagnetismInjector : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            if (pawn?.health == null)
            {
                return;
            }

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MagnetoDefOf.Magneto_Magnetism);
            if (hediff == null)
            {
                hediff = HediffMaker.MakeHediff(MagnetoDefOf.Magneto_Magnetism, pawn);
                pawn.health.AddHediff(hediff);
            }

            hediff.Severity = 1f;

            if (PawnUtility.ShouldSendNotificationAbout(pawn))
            {
                Messages.Message("Magneto_Message_GeneInjected".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
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
