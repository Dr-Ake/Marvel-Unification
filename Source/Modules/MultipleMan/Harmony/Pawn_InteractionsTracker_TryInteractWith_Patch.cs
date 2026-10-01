using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn_InteractionsTracker), nameof(Pawn_InteractionsTracker.TryInteractWith))]
    public static class Pawn_InteractionsTracker_TryInteractWith_Patch
    {
        private static readonly AccessTools.FieldRef<Pawn_InteractionsTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_InteractionsTracker, Pawn>("pawn");

        public static bool Prefix(Pawn_InteractionsTracker __instance, Pawn recipient, InteractionDef intDef, ref bool __result)
        {
            var pawn = __instance != null ? PawnField(__instance) : null;
            if (!DupeSettingsManager.SocialAllowed && ((pawn != null && pawn.IsDupe()) || (recipient != null && recipient.IsDupe())))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
