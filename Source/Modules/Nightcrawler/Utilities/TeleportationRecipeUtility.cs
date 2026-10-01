using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace NightcrawlerTeleportation
{
    [StaticConstructorOnStartup]
    public static class TeleportationRecipeUtility
    {
        private static readonly FieldInfo AllRecipesCachedField = AccessTools.Field(typeof(ThingDef), "allRecipesCached");

        static TeleportationRecipeUtility()
        {
            EnsureTeleportInjectorRecipeOnAnimals();
        }

        public static void EnsureTeleportInjectorRecipeOnAnimals()
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("Nightcrawler_AdministerTeleportationInjector");
            if (recipe == null)
            {
                return;
            }

            List<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            foreach (ThingDef thing in allDefs)
            {
                if (thing?.race == null || !thing.race.Animal)
                {
                    continue;
                }

                thing.recipes ??= new List<RecipeDef>();
                if (thing.recipes.Contains(recipe))
                {
                    continue;
                }

                thing.recipes.Add(recipe);
                AllRecipesCachedField?.SetValue(thing, null);
            }
        }
    }
}
