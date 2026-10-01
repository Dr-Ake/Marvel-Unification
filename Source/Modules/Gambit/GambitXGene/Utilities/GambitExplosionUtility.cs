using System.Collections.Generic;
using GambitXGene.Comps;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GambitXGene.Utilities;

public static class GambitExplosionUtility
{
    private static readonly List<Thing> TmpIgnored = new();

    private static SoundDef? cardExplosionSound;
    private static SoundDef? touchExplosionSound;
    private static SoundDef? kineticExplosionSound;

    public static void DoCardExplosion(Pawn pawn, IntVec3 cell)
    {
        var settings = GambitMod.Settings;
        if (settings == null || pawn.Map == null)
        {
            return;
        }

        PrepareIgnored(pawn);

        GenExplosion.DoExplosion(
            cell,
            pawn.Map,
            settings.explosionRadius,
            DamageDefOf.Bomb,
            pawn,
            Mathf.RoundToInt(settings.cardDamage),
            -1f,
            GetCardSound(),
            ignoredThings: TmpIgnored,
            chanceToStartFire: 0.25f,
            damageFalloff: true);

        TmpIgnored.Clear();
    }

    public static void DoTouchChargeExplosion(Pawn pawn, IntVec3 cell, Building? primaryTarget)
    {
        var settings = GambitMod.Settings;
        if (settings == null || pawn.Map == null)
        {
            return;
        }

        PrepareIgnored(pawn);
        if (primaryTarget != null)
        {
            TmpIgnored.Add(primaryTarget);
        }

        var baseDamage = settings.touchChargeDamage;
        if (primaryTarget != null)
        {
            baseDamage = Mathf.Max(baseDamage, primaryTarget.MaxHitPoints);
        }

        GenExplosion.DoExplosion(
            cell,
            pawn.Map,
            settings.touchChargeRadius,
            DamageDefOf.Bomb,
            pawn,
            Mathf.RoundToInt(baseDamage),
            -1f,
            GetTouchSound(),
            ignoredThings: TmpIgnored,
            chanceToStartFire: 0.2f,
            damageFalloff: true);

        if (primaryTarget != null && !primaryTarget.Destroyed)
        {
            var finalDamage = Mathf.Max(primaryTarget.HitPoints, Mathf.RoundToInt(baseDamage * 1.25f));
            var dinfo = new DamageInfo(DamageDefOf.Bomb, finalDamage, 999f, instigator: pawn);
            primaryTarget.TakeDamage(dinfo);
        }

        TmpIgnored.Clear();
    }

    public static void TryDoKineticExplosion(Pawn pawn, Thing? intendedTarget, IntVec3 fallbackCell, HediffComp_GambitDeck deck)
    {
        var settings = GambitMod.Settings;
        if (settings == null || pawn.Map == null || !deck.ReadyForKinetic(intendedTarget))
        {
            return;
        }

        var center = intendedTarget?.Position ?? fallbackCell;
        PrepareIgnored(pawn);
        if (settings.kineticIgnoreFriendlyFire)
        {
            CollectFriendlyPawns(pawn, center, settings.kineticRadius);
        }

        GenExplosion.DoExplosion(
            center,
            pawn.Map,
            settings.kineticRadius,
            DamageDefOf.Bomb,
            pawn,
            Mathf.RoundToInt(settings.kineticDamage),
            settings.kineticArmorPen,
            GetKineticSound(),
            ignoredThings: TmpIgnored,
            chanceToStartFire: 0.05f,
            damageFalloff: false);

        TmpIgnored.Clear();
    }

    private static void PrepareIgnored(Pawn pawn)
    {
        TmpIgnored.Clear();
        TmpIgnored.Add(pawn);
    }

    private static void CollectFriendlyPawns(Pawn caster, IntVec3 center, float radius)
    {
        var map = caster.Map;
        if (map == null || caster.Faction == null)
        {
            return;
        }

        var cells = GenRadial.RadialCellsAround(center, radius, true);
        foreach (var cell in cells)
        {
            if (!cell.InBounds(map))
            {
                continue;
            }

            var things = map.thingGrid.ThingsListAtFast(cell);
            for (var i = 0; i < things.Count; i++)
            {
                if (things[i] is Pawn pawn && pawn.Faction == caster.Faction)
                {
                    TmpIgnored.Add(pawn);
                }
            }
        }
    }

    private static SoundDef GetCardSound() =>
        cardExplosionSound ??= DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_Bomb")
                             ?? SoundDefOf.Explosion_FirefoamPopper;

    private static SoundDef GetTouchSound() =>
        touchExplosionSound ??= DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_Mortar")
                             ?? GetCardSound();

    private static SoundDef GetKineticSound() =>
        kineticExplosionSound ??= DefDatabase<SoundDef>.GetNamedSilentFail("Explosion_Bomb")
                               ?? GetCardSound();
}
