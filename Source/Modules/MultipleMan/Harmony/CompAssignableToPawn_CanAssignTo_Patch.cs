using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(CompAssignableToPawn), nameof(CompAssignableToPawn.CanAssignTo))]
    public static class CompAssignableToPawn_CanAssignTo_Patch
    {
        public static void Postfix(CompAssignableToPawn __instance, Pawn pawn, ref AcceptanceReport __result)
        {
            if (!__result.Accepted || pawn == null)
            {
                return;
            }

            if (pawn.IsDupe())
            {
                var bed = __instance.parent as Building_Bed;
                if (!DupeSettingsManager.BedOwnershipAllowed && bed != null && !bed.Medical)
                {
                    __result = (AcceptanceReport)"Dupes cannot claim beds";
                }
            }
        }
    }
}
