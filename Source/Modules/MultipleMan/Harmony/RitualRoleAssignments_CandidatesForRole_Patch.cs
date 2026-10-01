using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(RitualRoleAssignments))]
    public static class RitualRoleAssignments_CandidatesForRole_Patch
    {
        private static IEnumerable<Pawn>? Filter(IEnumerable<Pawn>? source)
        {
            if (DupeSettingsManager.RitualParticipationAllowed)
            {
                return source;
            }

            return source?.Where(p => p == null || !p.IsDupe());
        }

        [HarmonyPatch(nameof(RitualRoleAssignments.CandidatesForRole), new Type[] { typeof(string), typeof(TargetInfo), typeof(bool), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void ForRoleId(ref IEnumerable<Pawn> __result)
        {
            __result = Filter(__result) ?? Enumerable.Empty<Pawn>();
        }

        [HarmonyPatch(nameof(RitualRoleAssignments.CandidatesForRole), new Type[] { typeof(RitualRole), typeof(TargetInfo), typeof(bool), typeof(bool), typeof(bool) })]
        [HarmonyPostfix]
        public static void ForRole(ref IEnumerable<Pawn> __result)
        {
            __result = Filter(__result) ?? Enumerable.Empty<Pawn>();
        }
    }
}
