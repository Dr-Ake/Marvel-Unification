using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Building_SubcoreScanner))]
    public static class Building_SubcoreScanner_CanAcceptPawn_Patch
    {
        private static bool ShouldBlockDupe(Building_SubcoreScanner scanner, Pawn pawn)
        {
            if (scanner == null || pawn == null)
            {
                return false;
            }

            // Ripscanners set DestroyOccupantBrain to true; don't block non-lethal scanners.
            return scanner.DestroyOccupantBrain && pawn.IsDupe() && !DupeSettingsManager.RipScannerUsageAllowed;
        }

        [HarmonyPatch(nameof(Building_SubcoreScanner.CanAcceptPawn))]
        [HarmonyPostfix]
        public static void CanAcceptPawnPostfix(Building_SubcoreScanner __instance, Pawn selPawn, ref AcceptanceReport __result)
        {
            // Only block if it was otherwise accepted.
            // If the original method or another mod already rejected it, we don't need to intervene.
            if (__result.Accepted && ShouldBlockDupe(__instance, selPawn))
            {
                __result = "Dupes cannot be used in rip scanners (mod settings).";
            }
        }

        [HarmonyPatch(nameof(Building_SubcoreScanner.TryAcceptPawn))]
        [HarmonyPrefix]
        public static bool TryAcceptPawnPrefix(Building_SubcoreScanner __instance, Pawn pawn)
        {
            // Kept as Prefix to prevent the side-effects of entering the scanner if blocked.
            if (ShouldBlockDupe(__instance, pawn))
            {
                Messages.Message("Dupes cannot be used in rip scanners (mod settings).", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }

            return true;
        }
    }
}
