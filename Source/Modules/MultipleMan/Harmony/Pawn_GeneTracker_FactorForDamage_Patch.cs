using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), nameof(Pawn_GeneTracker.FactorForDamage))]
    public static class Pawn_GeneTracker_FactorForDamage_Patch
    {
        public static bool Prefix(Pawn_GeneTracker __instance, DamageInfo dinfo, ref float __result)
        {
            if (__instance == null || !ModsConfig.BiotechActive)
            {
                __result = 1f;
                return false;
            }

            var pawn = __instance.pawn;
            if (pawn == null || !pawn.IsDupe())
            {
                return true;
            }

            if (dinfo.Def == null)
            {
                __result = 1f;
                return false;
            }

            __result = GetSafeDamageFactor(__instance, dinfo.Def);
            return false;
        }

        private static float GetSafeDamageFactor(Pawn_GeneTracker tracker, DamageDef damageDef)
        {
            if (tracker == null || damageDef == null)
            {
                return 1f;
            }

            var genes = tracker?.GenesListForReading;
            if (genes == null || genes.Count == 0)
            {
                return 1f;
            }

            float factor = 1f;
            try
            {
                for (int i = 0; i < genes.Count; i++)
                {
                    var gene = genes[i];
                    if (gene?.Active != true)
                    {
                        continue;
                    }

                    var def = gene.def;
                    var defFactors = def?.damageFactors;
                    if (defFactors == null || defFactors.Count == 0)
                    {
                        continue;
                    }

                    for (int j = 0; j < defFactors.Count; j++)
                    {
                        var entry = defFactors[j];
                        if (entry?.damageDef != damageDef)
                        {
                            continue;
                        }

                        factor *= entry.factor;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce($"[MultipleManXGene] Failed to evaluate damage factors for dupe {tracker?.pawn?.LabelShort ?? "(unknown)"}: {ex}", Gen.HashCombineInt(tracker?.GetHashCode() ?? 0, 1311835));
                return 1f;
            }

            return factor;
        }
    }
}
