using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Rimworld_Storm
{
    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class Patch_Verb_LaunchProjectile_TryCastShot
    {
        public static bool Prefix(Verb_LaunchProjectile __instance, ref bool __result)
        {
            if (__instance.CasterPawn == null) return true;
            
            Thing target = __instance.CurrentTarget.Thing;
            if (target is Pawn targetPawn)
            {
                 var hediff = targetPawn.health?.hediffSet?.GetFirstHediffOfDef(HediffDef.Named("Storm_Gene"));
                 if (hediff != null)
                 {
                     // Check if a storm is active via our component OR if wind is naturally high
                     bool stormActive = false;
                     if (targetPawn.Map != null)
                     {
                         var comp = targetPawn.Map.GetComponent<StormGodComponent>();
                         if (comp != null && comp.isStormActive)
                         {
                             stormActive = true;
                         }
                         else if (targetPawn.Map.weatherManager.CurWeatherPerceived.windSpeedFactor > 1.0f)
                         {
                             stormActive = true;
                         }
                     }

                     if (stormActive)
                     {
                         // Use settings for chance
                         if (Rand.Value < StormSettings.deflectionChance)
                         {
                             MoteMaker.ThrowText(targetPawn.DrawPos, targetPawn.Map, "Wind Deflect!", 3.65f);
                             __result = false; // Miss
                             return false; // Skip original method
                         }
                     }
                 }
            }
            return true;
        }
    }
}
