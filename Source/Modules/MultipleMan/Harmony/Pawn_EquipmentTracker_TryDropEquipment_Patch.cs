using System.Reflection;
using HarmonyLib;
using Verse;

namespace MultipleManXGene
{
    [HarmonyPatch]
    public static class Pawn_EquipmentTracker_TryDropEquipment_Patch
    {
        private static readonly AccessTools.FieldRef<Pawn_EquipmentTracker, Pawn> PawnField =
            AccessTools.FieldRefAccess<Pawn_EquipmentTracker, Pawn>("pawn");

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Pawn_EquipmentTracker), "TryDropEquipment", new[]
            {
                typeof(ThingWithComps),
                typeof(ThingWithComps).MakeByRefType(),
                typeof(IntVec3),
                typeof(bool)
            });
        }

        public static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref bool __result)
        {
            var pawn = PawnField(__instance);
            if (pawn != null && DupeManager.Current.TryGetData(pawn, out var data) && data.IsPhantom(eq))
            {
                eq.Destroy(DestroyMode.Vanish);
                data.NotifyThingRemoved(eq);
                __result = true;
                return false;
            }

            return true;
        }
    }
}
