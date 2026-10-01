using RimWorld;
using Verse;

namespace MarvelUnification
{
    public static class InjectorUtility
    {
        public static AcceptanceReport OrganicRecipient(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health == null || pawn.RaceProps == null ||
                pawn.RaceProps.IsMechanoid || !(pawn.RaceProps.Humanlike || pawn.RaceProps.Animal))
                return "Only living organic pawns can use this injector.";
            return true;
        }
    }

    public abstract class OrganicHediffUseEffect : CompUseEffect
    {
        protected abstract HediffDef GrantedHediff { get; }

        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            var original = base.CanBeUsedBy(pawn);
            if (!original.Accepted) return original;
            var recipient = InjectorUtility.OrganicRecipient(pawn);
            if (!recipient.Accepted) return recipient;
            if (GrantedHediff == null) return "Injector power is unavailable.";
            if (pawn.health.hediffSet.HasHediff(GrantedHediff)) return "Already has: " + GrantedHediff.LabelCap;
            return true;
        }
    }
}
