using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Drake.Hulk
{
    public static class HulkSerumUtility
    {
        public static bool CanReceiveGammaSerum(Pawn pawn, out string reason)
        {
            if (pawn == null)
            {
                reason = "HulkSerumNoPawnSelected".Translate();
                return false;
            }

            if (pawn.Dead)
            {
                reason = "HulkSerumCannotAdministerDead".Translate();
                return false;
            }

            if (pawn.health == null || pawn.story == null)
            {
                reason = "HulkSerumOnlyLivingHumanlikes".Translate();
                return false;
            }

            if (!pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid)
            {
                reason = "HulkSerumOnlyLivingHumanlikes".Translate();
                return false;
            }

            if (HulkUtility.HasGammaIdentity(pawn))
            {
                reason = "HulkSerumAlreadyBonded".Translate(pawn.LabelShortCap);
                return false;
            }

            reason = null;
            return true;
        }

        public static bool TryGrantGammaSerum(Pawn pawn, bool showMessages = true)
        {
            if (!CanReceiveGammaSerum(pawn, out var reason))
            {
                if (showMessages && !string.IsNullOrWhiteSpace(reason))
                {
                    Messages.Message(reason, pawn, MessageTypeDefOf.RejectInput, false);
                }

                return false;
            }

            HulkUtility.GrantGammaIdentity(pawn);
            if (showMessages)
            {
                Messages.Message("HulkSerumNowBonded".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
            }

            return true;
        }
    }

    public sealed class CompProperties_UseEffectGiveHulkSerum : CompProperties_UseEffect
    {
        public CompProperties_UseEffectGiveHulkSerum()
        {
            compClass = typeof(CompUseEffectGiveHulkSerum);
        }
    }

    public sealed class CompUseEffectGiveHulkSerum : CompUseEffect
    {
        public override void DoEffect(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            HulkSerumUtility.TryGrantGammaSerum(usedBy);
        }
    }

    public sealed class Recipe_AdministerHulkSerum : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null && CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
            {
                return;
            }

            if (!HulkSerumUtility.TryGrantGammaSerum(pawn, showMessages: false))
            {
                return;
            }

            if (billDoer != null)
            {
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
            }

            Messages.Message("HulkSerumSurgerySuccess".Translate(pawn.LabelShortCap), pawn, MessageTypeDefOf.PositiveEvent);
        }

        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            return false;
        }
    }
}
