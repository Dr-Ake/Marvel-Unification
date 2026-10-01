using System.Linq;
using RimWorld;
using Verse;

namespace CyclopsGene
{
    [DefOf]
    public static class CyclopsDefOf
    {
        public static HediffDef Cyclops_Gene;
        public static HediffDef Cyclops_CommandPresence;
        public static ThingDef Cyclops_ControlVisor;
        public static ThingDef Cyclops_OpticBlastEffect;

        static CyclopsDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CyclopsDefOf));
        }
    }

    public static class CyclopsUtility
    {
        public static HediffComp_OpticController GetController(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || CyclopsDefOf.Cyclops_Gene == null)
            {
                return null;
            }

            Hediff gene = pawn.health.hediffSet.GetFirstHediffOfDef(CyclopsDefOf.Cyclops_Gene);
            return (gene as HediffWithComps)?.GetComp<HediffComp_OpticController>();
        }

        public static bool HasCyclopsGene(Pawn pawn)
        {
            return GetController(pawn) != null;
        }

        public static bool HasControlVisor(Pawn pawn)
        {
            return pawn?.apparel?.WornApparel.Any(apparel => apparel.def == CyclopsDefOf.Cyclops_ControlVisor) == true;
        }

        public static bool HasVisorAnywhere(Pawn pawn)
        {
            if (HasControlVisor(pawn))
            {
                return true;
            }

            if (pawn?.inventory?.innerContainer == null)
            {
                return false;
            }

            return pawn.inventory.innerContainer.Any(thing => thing.def == CyclopsDefOf.Cyclops_ControlVisor);
        }

        public static bool EnsureControlVisor(Pawn pawn)
        {
            if (pawn?.RaceProps?.Humanlike != true || CyclopsDefOf.Cyclops_ControlVisor == null)
            {
                return false;
            }

            if (HasVisorAnywhere(pawn))
            {
                return true;
            }

            Apparel visor = ThingMaker.MakeThing(CyclopsDefOf.Cyclops_ControlVisor) as Apparel;
            if (visor == null)
            {
                return false;
            }

            if (pawn.apparel != null && pawn.apparel.CanWearWithoutDroppingAnything(visor.def)
                && ApparelUtility.HasPartsToWear(pawn, visor.def))
            {
                pawn.apparel.Wear(visor, dropReplacedApparel: false);
                return true;
            }

            if (pawn.inventory?.innerContainer != null && pawn.inventory.innerContainer.TryAdd(visor))
            {
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("CyclopsVisorPlacedInInventory".Translate(pawn.Named("PAWN")), pawn,
                        MessageTypeDefOf.NeutralEvent);
                }
                return true;
            }

            if (pawn.Spawned && GenPlace.TryPlaceThing(visor, pawn.Position, pawn.Map, ThingPlaceMode.Near))
            {
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message("CyclopsVisorPlacedNearby".Translate(pawn.Named("PAWN")), visor,
                        MessageTypeDefOf.NeutralEvent);
                }
                return true;
            }

            visor.Destroy();
            return false;
        }
    }
}
