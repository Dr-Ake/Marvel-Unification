using GambitXGene.Comps;
using GambitXGene.Utilities;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public class CompAbilityEffect_ThrowCard : CompAbilityEffect_GambitCard
{
    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && EnsureDeckAndCards(parent.pawn, 1, target, false, out _);
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (!EnsureDeckAndCards(parent.pawn, 1, target, true, out var deck))
        {
            return;
        }

        if (!(deck?.TryConsumeSingleCard() ?? false))
        {
            Messages.Message("Card failed to fire.", parent.pawn, MessageTypeDefOf.RejectInput, false);
            return;
        }

        GambitExplosionUtility.DoCardExplosion(parent.pawn, target.Cell);
    }
}
