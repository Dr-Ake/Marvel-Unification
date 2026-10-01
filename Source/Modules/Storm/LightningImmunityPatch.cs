using HarmonyLib;
using RimWorld;
using Verse;
using System;

namespace Rimworld_Storm
{
    public static class LightningImmunityPatch
    {
        // Thread-static flag to indicate we are currently processing a lightning strike
        [ThreadStatic]
        public static bool isStriking;

        [HarmonyPatch(typeof(WeatherEvent_LightningStrike), "FireEvent")]
        public static class LightningStrike_FireEvent_Patch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                isStriking = true;
            }

            [HarmonyPostfix]
            public static void Postfix()
            {
                isStriking = false;
            }
        }

        [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
        public static class Pawn_PreApplyDamage_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
            {
                absorbed = false;

                // Only intervene if we are in the middle of a lightning strike
                if (isStriking)
                {
                    // Check for Storm Gene
                    if (__instance.health != null && __instance.health.hediffSet.HasHediff(HediffDef.Named("Storm_Gene")))
                    {
                        // Lightning deals Bomb and Flame damage. 
                        // We block these specifically during a lightning strike event.
                        if (dinfo.Def == DamageDefOf.Bomb || dinfo.Def == DamageDefOf.Flame)
                        {
                            absorbed = true;
                            return false; // Skip damage
                        }
                    }
                }

                return true;
            }
        }
    }
}
