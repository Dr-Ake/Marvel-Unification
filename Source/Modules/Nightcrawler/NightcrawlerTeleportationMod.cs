using HarmonyLib;
using Verse;

namespace NightcrawlerTeleportation
{
    public class NightcrawlerTeleportationMod : Mod
    {
        public NightcrawlerTeleportationMod(ModContentPack content) : base(content)
        {
            MarvelUnification.MarvelHarmony.EnsurePatched();
        }
    }
}
