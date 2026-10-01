using System.Collections.Generic;
using GambitXGene.Comps;
using RimWorld;
using Verse;

namespace GambitXGene.Genes;

public class Gene_GambitXGene : Gene
{
    private static readonly List<AbilityDef> AbilityOrder = new()
    {
        GambitDefOf.Gambit_TouchCharge,
        GambitDefOf.Gambit_ThrowCard,
        GambitDefOf.Gambit_52CardPickup,
        GambitDefOf.Gambit_KineticToggle
    };

    public override void PostAdd()
    {
        base.PostAdd();
        EnsureDeck();
        EnsureAbilities();
    }

    public override void PostRemove()
    {
        base.PostRemove();
        RemoveDeck();
        RemoveAbilities();
    }

    private HediffComp_GambitDeck? EnsureDeck()
    {
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff);
        if (hediff == null)
        {
            hediff = HediffMaker.MakeHediff(GambitDefOf.Gambit_DeckHediff, pawn);
            pawn.health.AddHediff(hediff);
        }

        return hediff.TryGetComp<HediffComp_GambitDeck>();
    }

    private void RemoveDeck()
    {
        var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff);
        if (hediff != null)
        {
            pawn.health.RemoveHediff(hediff);
        }
    }

    private void EnsureAbilities()
    {
        if (pawn.abilities == null)
        {
            return;
        }

        foreach (var def in AbilityOrder)
        {
            if (pawn.abilities.GetAbility(def) == null)
            {
                pawn.abilities.GainAbility(def);
            }
        }
    }

    private void RemoveAbilities()
    {
        if (pawn.abilities == null)
        {
            return;
        }

        foreach (var def in AbilityOrder)
        {
            pawn.abilities.RemoveAbility(def);
        }
    }
}
