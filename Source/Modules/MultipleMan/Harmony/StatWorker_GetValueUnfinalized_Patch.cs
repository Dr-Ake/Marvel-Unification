using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized))]
    public static class StatWorker_GetValueUnfinalized_Patch
    {
        private static readonly AccessTools.FieldRef<StatWorker, StatDef> StatField =
            AccessTools.FieldRefAccess<StatWorker, StatDef>("stat");

        public static void Postfix(StatWorker __instance, StatRequest req, bool applyPostProcess, ref float __result)
        {
            if (!(req.Thing is Pawn pawn) || !pawn.IsDupe())
            {
                return;
            }

            var stat = StatField(__instance);
            if (stat == StatDefOf.MoveSpeed)
            {
                __result *= DupeSettingsManager.Settings.MoveSpeedMultiplier;
            }
            else if (stat == StatDefOf.WorkSpeedGlobal)
            {
                __result *= DupeSettingsManager.Settings.WorkSpeedMultiplier;
            }
            else if (stat == StatDefOf.ShootingAccuracyPawn || stat == StatDefOf.MeleeHitChance)
            {
                __result *= DupeSettingsManager.Settings.CombatAccuracyMultiplier;
            }
            else if (stat == StatDefOf.MeleeDPS)
            {
                __result *= DupeSettingsManager.Settings.CombatDamageMultiplier;
            }
        }
    }
}
