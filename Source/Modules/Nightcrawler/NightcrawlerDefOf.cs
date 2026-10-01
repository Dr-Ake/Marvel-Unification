using RimWorld;
using Verse;

namespace NightcrawlerTeleportation
{
    [DefOf]
    public static class NightcrawlerDefOf
    {
        public static HediffDef Nightcrawler_Teleportation;
        public static RecipeDef Nightcrawler_AdministerTeleportationInjector;

        static NightcrawlerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(NightcrawlerDefOf));
        }
    }
}
