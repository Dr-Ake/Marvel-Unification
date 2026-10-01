using System;
using RimWorld;
using Verse;

namespace Rimworld_Storm
{
    public class CompProperties_SummonTornado : CompProperties_AbilityEffect
    {
        public CompProperties_SummonTornado()
        {
            this.compClass = typeof(CompAbilityEffect_SummonTornado);
        }
    }

    public class CompAbilityEffect_SummonTornado : CompAbilityEffect
    {
        public new CompProperties_SummonTornado Props => (CompProperties_SummonTornado)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map != null)
            {
                IntVec3 cell = target.Cell;
                Tornado tornado = (Tornado)GenSpawn.Spawn(ThingDef.Named("Storm_Tornado"), cell, map);
            }
        }
    }
}
