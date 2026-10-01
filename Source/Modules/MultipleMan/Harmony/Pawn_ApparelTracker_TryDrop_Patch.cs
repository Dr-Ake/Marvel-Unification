using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch]
    public static class Pawn_ApparelTracker_TryDrop_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_ApparelTracker), "TryDrop", new[]
            {
                typeof(Apparel),
                typeof(Apparel).MakeByRefType(),
                typeof(IntVec3),
                typeof(bool)
            });
        }

        public static bool Prefix(Pawn_ApparelTracker __instance, Apparel ap, ref bool __result)
        {
            var pawn = __instance.pawn;
            if (pawn != null && DupeManager.Current.TryGetData(pawn, out var data) && data.IsPhantom(ap))
            {
                ap.Destroy(DestroyMode.Vanish);
                data.NotifyThingRemoved(ap);
                __result = true;
                return false;
            }

            return true;
        }
    }
}
