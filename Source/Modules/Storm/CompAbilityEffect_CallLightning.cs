using System;
using RimWorld;
using Verse;

namespace Rimworld_Storm
{
    public class CompProperties_CallLightning : CompProperties_AbilityEffect
    {
        public CompProperties_CallLightning()
        {
            this.compClass = typeof(CompAbilityEffect_CallLightning);
        }
    }

    public class CompAbilityEffect_CallLightning : CompAbilityEffect
    {
        public new CompProperties_CallLightning Props => (CompProperties_CallLightning)this.props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            if (map != null)
            {
                IntVec3 cell = target.Cell;
                map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(map, cell));
            }
        }
    }
}
