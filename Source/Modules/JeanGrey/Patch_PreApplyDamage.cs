using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace JeanGreyMod
{
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class Patch_PreApplyDamage
    {
        static bool Prefix(Pawn __instance, ref DamageInfo dinfo, ref bool absorbed)
        {
            if (__instance.health == null || __instance.health.hediffSet == null) return true;

            // Check for Hediff
            var phoenixHediff = __instance.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("JG_PhoenixForce"));
            if (phoenixHediff != null)
            {
                // Retain the original shield rule for weapon-sourced and explosive damage.
                bool isProjectile = dinfo.Weapon != null; 
                
                if (isProjectile || dinfo.Def.isExplosive) 
                {
                    // Calculate Block Chance
                    // Flat 90% chance regardless of severity.
                    float blockChance = 0.9f; 
                    
                    if (Rand.Value < blockChance)
                    {
                        absorbed = true; // Tell the engine the damage was absorbed
                        
                        // Visuals
                        MoteMaker.ThrowText(__instance.DrawPos, __instance.Map, "Telekinetic Shield", Color.magenta);
                        if (EffecterDefOf.Deflect_Metal != null)
                        {
                             EffecterDefOf.Deflect_Metal.Spawn().Trigger(__instance, dinfo.Instigator ?? __instance);
                        }
                        
                        return false; // Skip the original method
                    }
                }
            }
            return true; // Execute original method
        }
    }
}
