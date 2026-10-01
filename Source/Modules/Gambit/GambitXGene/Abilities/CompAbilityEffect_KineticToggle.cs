using GambitXGene.Comps;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public class CompAbilityEffect_KineticToggle : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        var deck = GetDeck();
        if (deck == null)
        {
            Messages.Message("Requires Gambit deck.", parent.pawn, MessageTypeDefOf.RejectInput, false);
            return;
        }

        deck.ToggleKinetic();
        Messages.Message($"Kinetic Melee {(deck.KineticMeleeEnabled ? "enabled" : "disabled")}.", parent.pawn,
            MessageTypeDefOf.NeutralEvent, false);
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (GetDeck() == null)
        {
            reason = "Requires Gambit deck.";
            return true;
        }

        return base.GizmoDisabled(out reason);
    }

    public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
    {
        var deck = GetDeck();
        if (deck != null)
        {
            return deck.KineticMeleeEnabled ? "ON" : "OFF";
        }

        return base.ExtraLabelMouseAttachment(target);
    }

    private HediffComp_GambitDeck? GetDeck()
    {
        return parent.pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff)
            ?.TryGetComp<HediffComp_GambitDeck>();
    }
}
