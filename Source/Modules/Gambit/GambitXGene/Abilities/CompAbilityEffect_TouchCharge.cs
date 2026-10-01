using GambitXGene.Utilities;
using RimWorld;
using Verse;

namespace GambitXGene.Abilities;

public class CompAbilityEffect_TouchCharge : CompAbilityEffect
{
    public override bool GizmoDisabled(out string reason)
    {
        if (!parent.pawn.Spawned)
        {
            reason = "Pawn must be present.";
            return true;
        }

        return base.GizmoDisabled(out reason);
    }

    public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest)
    {
        return base.CanApplyOn(target, dest) && ValidateAdjacent(target, false);
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (!ValidateAdjacent(target))
        {
            return;
        }

        Building? building = target.Thing as Building;
        GambitExplosionUtility.DoTouchChargeExplosion(parent.pawn, target.Cell, building);
    }

    private bool ValidateAdjacent(LocalTargetInfo target, bool showMessage = true)
    {
        var pawn = parent.pawn;
        if (pawn.Map == null)
        {
            return false;
        }

        if (!target.Cell.AdjacentTo8WayOrInside(pawn.Position))
        {
            if (showMessage)
            {
                Messages.Message("Target must be adjacent.", pawn, MessageTypeDefOf.RejectInput, false);
            }

            return false;
        }

        return true;
    }
}
