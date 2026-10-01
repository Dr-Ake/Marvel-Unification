using GambitXGene.Comps;
using GambitXGene.Utilities;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace GambitXGene.HarmonyPatches;

[HarmonyPatch(typeof(Verb_MeleeAttackDamage), "ApplyMeleeDamageToTarget")]
public static class Harmony_MeleeExplosions
{
    public static void Postfix(Verb_MeleeAttackDamage __instance, LocalTargetInfo target)
    {
        var pawn = __instance.CasterPawn;
        if (pawn == null || pawn.Map == null || target.Cell == IntVec3.Invalid)
        {
            return;
        }

        var deck = pawn.health.hediffSet.GetFirstHediffOfDef(GambitDefOf.Gambit_DeckHediff)?.TryGetComp<HediffComp_GambitDeck>();
        if (deck == null || !deck.KineticMeleeEnabled)
        {
            return;
        }

        GambitExplosionUtility.TryDoKineticExplosion(pawn, target.Thing, target.Cell, deck);
    }
}
