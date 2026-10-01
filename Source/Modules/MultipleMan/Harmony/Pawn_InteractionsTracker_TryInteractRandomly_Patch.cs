using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), "TryInteractRandomly")]
    public static class Pawn_InteractionsTracker_TryInteractRandomly_Patch
    {
        private static readonly AccessTools.FieldRef<Pawn_InteractionsTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_InteractionsTracker, Pawn>("pawn");

        public static bool Prefix(Pawn_InteractionsTracker __instance)
        {
            var pawn = __instance != null ? PawnField(__instance) : null;
            if (pawn != null && pawn.IsDupe() && !DupeSettingsManager.SocialAllowed)
            {
                return false;
            }

            return true;
        }
    }
}
