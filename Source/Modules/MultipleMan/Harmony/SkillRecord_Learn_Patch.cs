using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
    public static class SkillRecord_Learn_Patch
    {
        private static readonly AccessTools.FieldRef<SkillRecord, Pawn> PawnField =
            AccessTools.FieldRefAccess<SkillRecord, Pawn>("pawn");

        public static bool Prefix(SkillRecord __instance)
        {
            var pawn = __instance != null ? PawnField(__instance) : null;
            if (pawn != null && pawn.IsDupe() && !DupeSettingsManager.Settings.SkillXpGainEnabled)
            {
                return false;
            }

            return true;
        }
    }
}
