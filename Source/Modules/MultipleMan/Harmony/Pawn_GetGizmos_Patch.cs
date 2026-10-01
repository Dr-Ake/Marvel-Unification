using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (var gizmo in values)
            {
                yield return gizmo;
            }

            var summon = DupeUtility.BuildSummonGizmo(__instance);
            if (summon != null)
            {
                yield return summon;
            }

            var dismiss = DupeUtility.BuildDismissGizmo(__instance);
            if (dismiss != null)
            {
                yield return dismiss;
            }

            var selfDismiss = DupeUtility.BuildSelfDismissGizmo(__instance);
            if (selfDismiss != null)
            {
                yield return selfDismiss;
            }
        }
    }
}
