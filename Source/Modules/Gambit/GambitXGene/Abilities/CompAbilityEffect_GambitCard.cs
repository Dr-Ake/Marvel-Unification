using GambitXGene.Comps;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public abstract class CompAbilityEffect_GambitCard : CompAbilityEffect
{
    protected HediffComp_GambitDeck? GetDeckComp(Pawn pawn)
    {
        return pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff)?.TryGetComp<HediffComp_GambitDeck>();
    }

    protected bool EnsureDeckAndCards(Pawn pawn, int cardsRequired, LocalTargetInfo target, bool showMessage, out HediffComp_GambitDeck? deck)
    {
        deck = GetDeckComp(pawn);
        if (deck == null)
        {
            if (showMessage)
            {
                Messages.Message("Requires Gambit deck.", pawn, MessageTypeDefOf.RejectInput, false);
            }
            return false;
        }

        if (!deck.HasCards(cardsRequired))
        {
            if (showMessage)
            {
                Messages.Message("Deck is empty.", pawn, MessageTypeDefOf.RejectInput, false);
            }
            return false;
        }

        return true;
    }
}
