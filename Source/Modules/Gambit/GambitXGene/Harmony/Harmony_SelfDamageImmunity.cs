using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GambitXGene.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.PreApplyDamage))]
public static class Harmony_SelfDamageImmunity
{
    private static readonly FieldInfo? PawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");

    public static bool Prefix(Pawn_HealthTracker __instance, DamageInfo dinfo, ref bool absorbed)
    {
        var targetPawn = PawnField?.GetValue(__instance) as Pawn;
        var instigatorPawn = dinfo.Instigator as Pawn;
        if (instigatorPawn == null || targetPawn == null)
        {
            return true;
        }

        if (instigatorPawn != targetPawn)
        {
            return true;
        }

        if (instigatorPawn.health?.hediffSet == null ||
            !instigatorPawn.health.hediffSet.HasHediff(GambitDefOf.Gambit_DeckHediff))
        {
            return true;
        }

        if (dinfo.Def != DamageDefOf.Bomb && dinfo.Def != DamageDefOf.Flame)
        {
            return true;
        }

        absorbed = true;
        return false;
    }
}
