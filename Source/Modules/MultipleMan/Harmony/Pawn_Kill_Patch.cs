using HarmonyLib;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Pawn_Kill_Patch
    {
        public static bool Prefix(Pawn __instance)
        {
            if (__instance.IsDupe())
            {
                if (DupeManager.Current.TryGetData(__instance, out var data))
                {
                    DupeManager.Current.ForceDespawn(data, DupeDespawnReason.Manual);
                }
                return false;
            }

            return true;
        }

        public static void Postfix(Pawn __instance)
        {
            if (!__instance.IsDupe())
            {
                DupeManager.Current.NotifyOriginalDeath(__instance);
            }
        }
    }
}
