using HarmonyLib;
using RimWorld;
using Verse;

namespace Rimworld_Storm
{
    [HarmonyPatch(typeof(Pawn), "PreApplyDamage")]
    public static class DamageImmunityPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn __instance, ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;

            // Check for Storm Gene
            if (__instance.health != null && __instance.health.hediffSet.HasHediff(HediffDef.Named("Storm_Gene")))
            {
                // Immunity to Tornado Scratch
                if (dinfo.Def == DamageDefOf.TornadoScratch)
                {
                    absorbed = true; // Visually indicate absorption
                    return false; // Skip original method
                }
            }

            return true;
        }
    }
}
