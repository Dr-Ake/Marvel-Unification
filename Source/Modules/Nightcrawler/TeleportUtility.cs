using System;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace NightcrawlerTeleportation
{
    public static class TeleportUtility
    {
        private static readonly Action<Pawn_PathFollower>? NotifyTeleportedInternal =
            AccessTools.Method(typeof(Pawn_PathFollower), "Notify_Teleported_Int") is { } method
                ? AccessTools.MethodDelegate<Action<Pawn_PathFollower>>(method)
                : null;

        private static readonly Action<Pawn_PathFollower>? PatherArrivedInternal =
            AccessTools.Method(typeof(Pawn_PathFollower), "PatherArrived") is { } arrivedMethod
                ? AccessTools.MethodDelegate<Action<Pawn_PathFollower>>(arrivedMethod)
                : null;

        public static bool TryGetActiveTeleportComp(Pawn pawn, out HediffComp_TeleportToggle comp)
        {
            comp = null!;
            if (!TryGetTeleportComp(pawn, out var found))
            {
                return false;
            }

            comp = found;

            if (!comp.TeleportEnabled)
            {
                return false;
            }

            if (pawn.Map == null || pawn.Dead || pawn.Downed)
            {
                return false;
            }

            if (comp.Props.requireConscious && pawn.health?.capacities != null)
            {
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                {
                    return false;
                }
            }

            if (!comp.Props.allowWhileDrafted && pawn.Drafted)
            {
                return false;
            }

            return true;
        }

        public static bool TryGetTeleportComp(Pawn pawn, out HediffComp_TeleportToggle comp)
        {
            comp = null!;

            if (pawn?.health?.hediffSet == null)
            {
                return false;
            }

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is not HediffWithComps withComps)
                {
                    continue;
                }

                var toggle = withComps.TryGetComp<HediffComp_TeleportToggle>();
                if (toggle == null)
                {
                    continue;
                }

                comp = toggle;
                return true;
            }

            return false;
        }

        public static IntVec3 ResolveDestination(LocalTargetInfo destination, PawnPath? path)
        {
            if (path != null && path.NodesLeftCount > 0)
            {
                var lastNode = path.LastNode;
                if (lastNode.IsValid)
                {
                    return lastNode;
                }
            }

            if (destination.Cell.IsValid)
            {
                return destination.Cell;
            }

            return IntVec3.Invalid;
        }

        public static bool CanTeleportTo(Pawn pawn, IntVec3 cell, HediffComp_TeleportToggle comp)
        {
            if (!cell.IsValid)
            {
                return false;
            }

            var map = pawn.Map;
            if (map == null)
            {
                return false;
            }

            if (!cell.InBounds(map) || !cell.Standable(map))
            {
                return false;
            }

            if (comp.Props.checkFog && cell.Fogged(map))
            {
                return false;
            }

            if (cell.GetFirstPawn(map) is { } other && other != pawn)
            {
                return false;
            }

            return true;
        }

        public static void ExecuteTeleport(Pawn pawn, IntVec3 destination, Pawn_PathFollower follower, HediffComp_TeleportToggle comp)
        {
            var map = pawn.Map;
            var origin = pawn.Position;
            if (map == null)
            {
                return;
            }

            follower.StopDead();

            pawn.Position = destination;
            pawn.Notify_Teleported(endCurrentJob: false, resetTweenedPos: true);

            NotifyTeleportedInternal?.Invoke(follower);
            follower.ResetToCurrentPosition();

            if (pawn.jobs?.curDriver != null)
            {
                PatherArrivedInternal?.Invoke(follower);
            }

            comp.OnTeleported(pawn, origin, destination);
        }
    }
}
