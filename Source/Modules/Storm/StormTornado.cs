using System;
using RimWorld;
using Verse;
using UnityEngine;

namespace Rimworld_Storm
{
    public class StormTornado : Tornado
    {
        private int ticksLeftToDisappear = 2500;

        protected override void Tick()
        {
            // Custom logic could go here to make it follow enemies
            // For now, just base behavior but maybe faster?
            base.Tick();
            
            ticksLeftToDisappear--;
            if (ticksLeftToDisappear <= 0)
            {
                Destroy();
            }
        }
        
        // We can override ExposeData to save state
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeftToDisappear, "ticksLeftToDisappear", 2500);
        }
    }
}
