using HarmonyLib;
using RimWorld;
using Verse;

namespace DeadpoolsHealingFactor
{
    [HarmonyPatch(typeof(KidnapAIUtility), "ReachableWoundedGuest")]
    public static class Patch_KidnapAIUtility_ReachableWoundedGuest
    {
        public static void Postfix(ref Pawn __result)
        {
            if (DeadpoolsHealingFactorMod.settings?.preventHealingFactorKidnap == true
                && __result?.health?.hediffSet?.HasHediff(DPDefOf.DP_HealingFactor) == true)
            {
                __result = null;
            }
        }
    }
}
