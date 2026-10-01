using Verse;
using UnityEngine;

namespace Rimworld_Storm
{
    public class StormSettings : ModSettings
    {
        public static float deflectionChance = 0.5f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref deflectionChance, "deflectionChance", 0.5f);
            base.ExposeData();
        }
    }
}
