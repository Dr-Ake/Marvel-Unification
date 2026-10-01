using HarmonyLib;
using Verse;
using Verse.AI;

namespace NightcrawlerTeleportation
{
    [HarmonyPatch(typeof(Pawn_PathFollower), "TryEnterNextPathCell")]
    public static class Pawn_PathFollower_TeleportPatch
    {
        public static bool Prefix(Pawn_PathFollower __instance, LocalTargetInfo ___destination, PawnPath ___curPath, Pawn ___pawn)
        {
            var pawn = ___pawn;
            if (pawn == null || !__instance.Moving)
            {
                return true;
            }

            if (___curPath == null || ___curPath.NodesLeftCount == 0)
            {
                return true;
            }

            if (!TeleportUtility.TryGetActiveTeleportComp(pawn, out var comp))
            {
                return true;
            }

            var destinationCell = TeleportUtility.ResolveDestination(___destination, ___curPath);
            if (!TeleportUtility.CanTeleportTo(pawn, destinationCell, comp))
            {
                return true;
            }

            TeleportUtility.ExecuteTeleport(pawn, destinationCell, __instance, comp);
            return false;
        }
    }
}
