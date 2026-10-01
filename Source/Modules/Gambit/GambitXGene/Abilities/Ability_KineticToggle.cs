using System.Collections.Generic;
using GambitXGene.Comps;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public class Ability_KineticToggle : Ability
{
    public Ability_KineticToggle()
    {
    }

    public Ability_KineticToggle(Pawn pawn) : base(pawn)
    {
    }

    public Ability_KineticToggle(Pawn pawn, AbilityDef def) : base(pawn, def)
    {
    }

    public override IEnumerable<Command> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            if (gizmo is Command_Ability command)
            {
                var deck = pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff)
                    ?.TryGetComp<HediffComp_GambitDeck>();
                if (deck != null)
                {
                    command.defaultLabel = deck.KineticMeleeEnabled
                        ? "Kinetic Melee: ON"
                        : "Kinetic Melee: OFF";
                }
            }

            yield return gizmo;
        }
    }
}
