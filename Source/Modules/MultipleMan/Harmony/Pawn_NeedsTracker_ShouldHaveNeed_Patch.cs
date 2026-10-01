using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn_NeedsTracker), "ShouldHaveNeed")]
    public static class Pawn_NeedsTracker_ShouldHaveNeed_Patch
    {
        private static readonly AccessTools.FieldRef<Pawn_NeedsTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_NeedsTracker, Pawn>("pawn");
        private static readonly NeedDef MoodNeed = DefDatabase<NeedDef>.GetNamedSilentFail("Mood");

        public static void Postfix(Pawn_NeedsTracker __instance, NeedDef nd, ref bool __result)
        {
            var pawn = __instance != null ? PawnField(__instance) : null;
            if (__result && pawn != null && pawn.IsDupe())
            {
                if (!DupeSettingsManager.NeedsAllowed)
                {
                    var isMood = nd == MoodNeed || (nd?.defName == "Mood");
                    if (!(isMood && DupeSettingsManager.MoodAllowed))
                    {
                        __result = false;
                    }
                }
            }
        }
    }
}
