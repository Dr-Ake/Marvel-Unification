using GambitXGene.Comps;
using GambitXGene.Systems;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public class CompAbilityEffect_52CardPickup : CompAbilityEffect_GambitCard
{
    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && EnsureDeckAndCards(parent.pawn, 1, target, false, out _);
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (!EnsureDeckAndCards(parent.pawn, 1, target, true, out var deck) || deck == null)
        {
            return;
        }

        GambitCardVolleyManager.Instance.StartVolley(parent.pawn, deck, target);
    }
}
