using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace Drake.Hulk
{
    [DefOf]
    public static class HulkDefOf
    {
        static HulkDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HulkDefOf));
        }

        public static TraitDef Drake_BruceBanner;
        public static HediffDef Drake_HulkIdentity;
        public static HediffDef Drake_HulkForm;
        public static HediffDef Drake_HulkRecoveryComa;
        public static HediffDef Drake_HulkRegrowing;
        public static HediffDef Drake_HulkAdjusting;
        public static ThingDef Drake_HulkGammaLeapFlyer;
        public static ThingDef Drake_HulkBoulderProjectile;
        public static ThingDef Drake_HulkGreenDoor;
        public static ThingDef Drake_HulkSerumInjector;
        public static RecipeDef Drake_AdministerHulkSerum;
        public static RecipeDef Drake_MakeHulkSerumInjector;
    }

    public static class HulkUtility
    {
        public const string HulkSuite = "hulk-smoke";
        public static readonly Color HulkSkinColor = new Color(0.16f, 0.74f, 0.18f, 1f);
        private static SoundDef TransformSound => HulkPresentation.ResolveSound("Pawn_Chimera_SpeedupRoar", "Ability_TerrorRoar", "Explosion_Thump");
        private static SoundDef RevertSound => HulkPresentation.ResolveSound("Foam_Impact", "BulletImpact_Ground");
        private static SoundDef HeavyImpactSound => HulkPresentation.ResolveSound("ThumpCannon_Impact", "Explosion_GiantBomb", "Explosion_Thump");
        private static SoundDef GammaLeapJumpSound => HulkPresentation.ResolveSound("JumpPackLaunch", "JumpMechLaunch", "Longjump_Jump");
        private static SoundDef GammaLeapLandSound => HeavyImpactSound;
        private static SoundDef ClapSound => HulkRuntimeTuning.GetSharedImpactSound() ?? HeavyImpactSound;
        private static SoundDef BoulderThrowSound => HulkPresentation.ResolveSound("JumpMechLaunch", "JumpPackLaunch", "Longjump_Jump");
        private static EffecterDef GammaLeapFlightEffect => HulkPresentation.ResolveEffecter("JumpMechFlightEffect", "JumpFlightEffect");
        private static FleckDef ExplosionFlashFleck => DefDatabase<FleckDef>.GetNamedSilentFail("ExplosionFlash");

        public static bool HasBannerTrait(Pawn pawn)
        {
            return pawn?.story?.traits?.HasTrait(HulkDefOf.Drake_BruceBanner) == true;
        }

        public static bool HasGammaIdentity(Pawn pawn)
        {
            return HasBannerTrait(pawn) || GetIdentityHediff(pawn) != null;
        }

        public static void EnsureBannerState(Pawn pawn)
        {
            if (pawn?.story == null || pawn.health == null)
            {
                return;
            }

            if (!HasBannerTrait(pawn))
            {
                pawn.story.traits.GainTrait(new Trait(HulkDefOf.Drake_BruceBanner));
            }

            var identity = GetIdentityHediff(pawn);
            if (identity == null)
            {
                identity = pawn.health.AddHediff(HulkDefOf.Drake_HulkIdentity);
            }

            GetIdentityComp(pawn)?.CaptureBaselineAppearance();
        }

        public static void GrantGammaIdentity(Pawn pawn)
        {
            EnsureBannerState(pawn);
            if (IsTransformed(pawn))
            {
                EnsureHulkAppearance(pawn);
            }
        }

        public static Hediff GetIdentityHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkIdentity);
        }

        public static Hediff GetFormHediff(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkForm);
        }

        public static bool IsTransformed(Pawn pawn)
        {
            return GetFormHediff(pawn) != null;
        }

        public static void Transform(Pawn pawn)
        {
            Transform(pawn, true);
        }

        public static void TransformForEmergency(Pawn pawn)
        {
            Transform(pawn, false);
        }

        public static void Revert(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            var identityComp = GetIdentityComp(pawn);
            if (identityComp?.HasActiveRage == true)
            {
                if (pawn.Map != null)
                {
                    Messages.Message("HulkMessageTooEnragedToRevert".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                return;
            }

            var form = GetFormHediff(pawn);
            if (form != null)
            {
                pawn.health.RemoveHediff(form);
            }

            identityComp?.ClearManualFormTimer();
            identityComp?.RestoreBannerAppearance();
            HulkPresentation.PlayAt(RevertSound, pawn);
        }

        public static float GetRageLevel(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0.35f;
            }

            var injurySeverity = TotalInjurySeverity(pawn);
            var pain = Mathf.Clamp01(pawn.health.hediffSet.PainTotal);
            var healthLoss = 1f - Mathf.Clamp01(pawn.health.summaryHealth.SummaryHealthPercent);
            var baseRage = 0.28f + pain * 0.35f + healthLoss * 1.15f + injurySeverity / 28f;
            var forcedFloor = GetIdentityComp(pawn)?.ForcedRageSeverityFloor ?? 0f;
            return Mathf.Clamp(Mathf.Max(baseRage, forcedFloor), 0.28f, 1f);
        }

        public static bool TryLeapNear(Pawn pawn, int radius = 14)
        {
            if (pawn?.Map == null)
            {
                return false;
            }

            if (!CellFinder.TryRandomClosewalkCellNear(
                    pawn.Position,
                    pawn.Map,
                    Math.Max(1, radius),
                    out var target,
                    cell => CanLeapTo(pawn, cell)))
            {
                return false;
            }

            return TryLeapTo(pawn, target);
        }

        public static void BeginLeapTargeting(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return;
            }

            Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = false,
                    canTargetBuildings = false,
                    canTargetItems = false,
                    canTargetPlants = false,
                    canTargetFires = false,
                    canTargetSelf = false
                },
                target =>
                {
                    if (!TryLeapTo(pawn, target.Cell))
                    {
                        Messages.Message("HulkMessageChooseStandableDestination".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                },
                pawn,
                null,
                null,
                false);
        }

        public static bool TryLeapTo(Pawn pawn, IntVec3 target)
        {
            if (!CanLeapTo(pawn, target))
            {
                return false;
            }

            var map = pawn.Map;
            pawn.pather?.StopDead();
            pawn.jobs?.StopAll();
            FleckMaker.ThrowDustPuffThick(pawn.DrawPos, map, 2.8f, Color.gray);
            HulkPresentation.PlayAt(GammaLeapJumpSound, pawn);

            var flyer = PawnFlyer.MakeFlyer(
                HulkDefOf.Drake_HulkGammaLeapFlyer,
                pawn,
                target,
                GammaLeapFlightEffect,
                null,
                false,
                null,
                null,
                new LocalTargetInfo(target));

            if (flyer == null)
            {
                return false;
            }

            GenSpawn.Spawn(flyer, target, map);
            return true;
        }

        public static bool PerformClap(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned || !IsTransformed(pawn))
            {
                return false;
            }

            var radius = HulkRuntimeTuning.GetClapRadius();
            var structureDamage = HulkRuntimeTuning.GetClapStructureDamage();
            var pawnDamage = HulkRuntimeTuning.GetClapPawnDamage();
            var map = pawn.Map;
            var destroyedAnything = false;
            foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                foreach (var thing in cell.GetThingList(map).ToList())
                {
                    if (thing == pawn ||
                        thing.Destroyed ||
                        thing is Mote ||
                        thing.def.category == ThingCategory.Filth)
                    {
                        continue;
                    }

                    if (thing is Pawn victim)
                    {
                        victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, pawnDamage, 999f, -1f, pawn, null, null));
                        if (!victim.Destroyed)
                        {
                            victim.stances?.stunner?.StunFor(90, pawn, addBattleLog: false, showMote: true);
                        }

                        destroyedAnything = true;
                        continue;
                    }

                    if (thing.def.destroyable || thing.def.useHitPoints)
                    {
                        thing.TakeDamage(new DamageInfo(DamageDefOf.Bomb, structureDamage, 999f, -1f, pawn, null, null));
                        if (!thing.Destroyed && thing.def.destroyable)
                        {
                            thing.Destroy(DestroyMode.KillFinalize);
                        }

                        destroyedAnything = true;
                    }
                }

                ThrowShockwaveCellFleck(cell, map, pawn.Position, radius, HulkSkinColor, 1.5f);
            }

            FleckMaker.ThrowDustPuffThick(pawn.DrawPos, map, 7f, HulkSkinColor);
            if (ExplosionFlashFleck != null)
            {
                FleckMaker.Static(pawn.DrawPos, map, ExplosionFlashFleck, 3.4f);
            }
            HulkPresentation.PlayAtCell(ClapSound, pawn.Position, map);
            MoteMaker.ThrowText(pawn.DrawPos, map, "HulkMoteClap".Translate(), Color.white);
            return destroyedAnything;
        }

        public static void BeginBoulderTargeting(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return;
            }

            Find.Targeter.BeginTargeting(
                new TargetingParameters
                {
                    canTargetLocations = true,
                    canTargetPawns = true,
                    canTargetBuildings = true,
                    canTargetItems = true,
                    canTargetPlants = true,
                    canTargetFires = false,
                    canTargetSelf = false
                },
                target =>
                {
                    if (!TryThrowBoulderAt(pawn, target))
                    {
                        Messages.Message("HulkMessageChooseThrowTarget".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                },
                pawn,
                null,
                null,
                false);
        }

        public static bool TryThrowBoulderTo(Pawn pawn, IntVec3 targetCell)
        {
            return TryThrowBoulderAt(pawn, new LocalTargetInfo(targetCell));
        }

        public static bool TryThrowBoulderAt(Pawn pawn, LocalTargetInfo target)
        {
            if (pawn?.Map == null || !pawn.Spawned || !IsTransformed(pawn))
            {
                return false;
            }

            var targetCell = target.Cell;
            var maxRange = HulkRuntimeTuning.GetBoulderThrowRange();
            if (!targetCell.IsValid ||
                !targetCell.InBounds(pawn.Map) ||
                targetCell == pawn.Position ||
                pawn.Position.DistanceToSquared(targetCell) > maxRange * maxRange)
            {
                return false;
            }

            var projectile = ThingMaker.MakeThing(HulkDefOf.Drake_HulkBoulderProjectile) as Projectile;
            if (projectile == null)
            {
                return false;
            }

            var map = pawn.Map;
            pawn.pather?.StopDead();
            FleckMaker.ThrowDustPuffThick(pawn.DrawPos, map, 2.1f, Color.gray);
            HulkPresentation.PlayAt(BoulderThrowSound, pawn);
            GenSpawn.Spawn(projectile, pawn.Position, map);
            projectile.Launch(
                pawn,
                target,
                target,
                ProjectileHitFlags.All,
                false,
                null);
            return true;
        }

        public static void ResolveGammaLeapLanding(Pawn pawn)
        {
            if (pawn?.Map == null || !pawn.Spawned)
            {
                return;
            }

            var radius = HulkRuntimeTuning.GetLeapRadius();
            var structureDamage = HulkRuntimeTuning.GetLeapStructureDamage();
            var pawnDamage = HulkRuntimeTuning.GetLeapPawnDamage();
            var map = pawn.Map;
            foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, radius, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                ThrowShockwaveCellFleck(cell, map, pawn.Position, radius, HulkSkinColor, 1.2f);
                foreach (var thing in cell.GetThingList(map).ToList())
                {
                    if (thing == pawn || thing.Destroyed || thing is Mote || thing.def.category == ThingCategory.Filth)
                    {
                        continue;
                    }

                    if (thing is Pawn victim)
                    {
                        victim.TakeDamage(new DamageInfo(DamageDefOf.Blunt, pawnDamage, 30f, -1f, pawn, null, null));
                        if (!victim.Destroyed)
                        {
                            victim.stances?.stunner?.StunFor(45, pawn, addBattleLog: false, showMote: true);
                        }
                        continue;
                    }

                    if (thing.def.destroyable || thing.def.useHitPoints)
                    {
                        thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, structureDamage, 80f, -1f, pawn, null, null));
                    }
                }
            }

            FleckMaker.ThrowDustPuffThick(pawn.DrawPos, map, 4.5f, HulkSkinColor);
            if (ExplosionFlashFleck != null)
            {
                FleckMaker.Static(pawn.DrawPos, map, ExplosionFlashFleck, 2.1f);
            }
            HulkPresentation.PlayAtCell(GammaLeapLandSound, pawn.Position, map);
            MoteMaker.ThrowText(pawn.DrawPos, map, "HulkMoteSlam".Translate(), Color.white);
        }

        public static void ApplyRegenerationPulse(Pawn pawn, float healAmount)
        {
            HulkHealingUtility.ApplyRegenerationPulse(pawn, healAmount);
        }

        public static void ApplyEmergencyRecovery(Pawn pawn)
        {
            HulkHealingUtility.ApplyEmergencyRecovery(pawn);
        }

        public static bool TryRegrowMissingPart(Pawn pawn)
        {
            return HulkHealingUtility.TryRegrowMissingPart(pawn);
        }

        public static float TotalInjurySeverity(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.hediffs?.OfType<Hediff_Injury>().Sum(entry => entry.Severity) ?? 0f;
        }

        public static string GetAutomaticSkipReasonForDef(HediffDef def)
        {
            return HulkHealingUtility.GetAutomaticSkipReasonForDef(def);
        }

        public static bool IsExcludedFromHealing(HediffDef def)
        {
            return HulkHealingUtility.Settings.IsExcludedFromHealing(def);
        }

        public static bool HasActiveRage(Pawn pawn)
        {
            return HulkImmortalityManager.HasActiveRage(pawn);
        }

        public static bool ForceHostileCarryRage(Pawn victim, Pawn carrier)
        {
            return HulkImmortalityManager.TriggerHostileCarryRage(victim, carrier);
        }

        public static void SetLastAggressor(Pawn pawn, Thing aggressor)
        {
            HulkImmortalityManager.RecordAggressor(pawn, aggressor);
        }

        public static Thing GetRevengeTarget(Pawn pawn)
        {
            return HulkImmortalityManager.GetRevengeTarget(pawn);
        }

        public static float GetFormSeverity(Pawn pawn)
        {
            return GetFormHediff(pawn)?.Severity ?? 0f;
        }

        public static void SetExcludedFromHealing(string defName, bool excluded)
        {
            HulkHealingUtility.Settings.SetExcludedFromHealing(defName, excluded);
        }

        public static void ResetHealingSettingsToDefaults()
        {
            ResetSettingsToDefaults();
        }

        public static void ResetSettingsToDefaults()
        {
            HulkHealingUtility.Settings.ResetToDefaults();
            ApplyCurrentSettings();
        }

        public static void ApplyCurrentSettings()
        {
            HulkRuntimeTuning.ApplyCurrentSettings();
        }

        public static IEnumerable<string> GetGizmoLabels(Pawn pawn)
        {
            return pawn.GetGizmos().OfType<Command>().Select(command => command.defaultLabel).Where(label => !string.IsNullOrWhiteSpace(label));
        }

        public static float GetClapRadius()
        {
            return HulkRuntimeTuning.GetClapRadius();
        }

        public static int GetClapPawnDamage()
        {
            return HulkRuntimeTuning.GetClapPawnDamage();
        }

        public static float GetLeapRadius()
        {
            return HulkRuntimeTuning.GetLeapRadius();
        }

        public static int GetLeapPawnDamage()
        {
            return HulkRuntimeTuning.GetLeapPawnDamage();
        }

        public static int GetBoulderThrowRange()
        {
            return HulkRuntimeTuning.GetBoulderThrowRange();
        }

        public static float GetBoulderExplosionRadius()
        {
            return HulkRuntimeTuning.GetBoulderExplosionRadius();
        }

        public static int GetBoulderDamage()
        {
            return HulkRuntimeTuning.GetBoulderDamage();
        }

        public static string GetSharedImpactSoundDefName()
        {
            return HulkRuntimeTuning.GetSharedImpactSoundDefName();
        }

        public static int GetForcedRageTicksRemaining(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.RageTicksRemaining ?? 0;
        }

        public static int GetForcedRageMaxTicks(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.MaxForcedRageTicks ?? 0;
        }

        public static bool HasForcedTimerBar(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.ShouldShowForcedTimerBar == true;
        }

        public static int GetManualFormTicksRemaining(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.ManualFormTicksRemaining ?? 0;
        }

        public static List<Pawn> GetEligibleColorCustomizationPawns()
        {
            var pawns = new List<Pawn>();
            if (Current.ProgramState != ProgramState.Playing)
            {
                return pawns;
            }

            foreach (var map in Find.Maps)
            {
                if (map?.IsPlayerHome != true || map.mapPawns == null)
                {
                    continue;
                }

                foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
                {
                    if (pawn == null || pawn.Dead || !HasGammaIdentity(pawn) || pawns.Contains(pawn))
                    {
                        continue;
                    }

                    pawns.Add(pawn);
                }
            }

            return pawns;
        }

        public static void ClearManualFormTimer(Pawn pawn)
        {
            GetIdentityComp(pawn)?.ClearManualFormTimer();
        }

        public static Color GetResolvedHulkSkinColor(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.ResolvedHulkSkinColor ?? HulkSkinColor;
        }

        public static bool HasCustomHulkSkinColor(Pawn pawn)
        {
            return GetIdentityComp(pawn)?.HasCustomHulkColor == true;
        }

        public static void SetCustomHulkSkinColor(Pawn pawn, Color color)
        {
            if (pawn == null || !HasGammaIdentity(pawn))
            {
                return;
            }

            EnsureBannerState(pawn);
            GetIdentityComp(pawn)?.SetCustomHulkColor(color);
        }

        public static void ClearCustomHulkSkinColor(Pawn pawn)
        {
            if (pawn == null || !HasGammaIdentity(pawn))
            {
                return;
            }

            GetIdentityComp(pawn)?.ClearCustomHulkColor();
        }

        public static void ClearAllCustomHulkSkinColors()
        {
            foreach (var pawn in GetEligibleColorCustomizationPawns())
            {
                ClearCustomHulkSkinColor(pawn);
            }
        }

        public static bool HasHulkAppearance(Pawn pawn)
        {
            if (pawn?.story == null)
            {
                return false;
            }

            if (pawn.story.bodyType != BodyTypeDefOf.Hulk)
            {
                return false;
            }

            if (!pawn.story.skinColorOverride.HasValue)
            {
                return false;
            }

            return ColorsClose(pawn.story.skinColorOverride.Value, GetResolvedHulkSkinColor(pawn));
        }

        public static void EnsureHulkAppearance(Pawn pawn)
        {
            if (!HasHulkAppearance(pawn))
            {
                GetIdentityComp(pawn)?.ApplyHulkAppearance();
            }
        }

        public static void RefreshPawnVisuals(Pawn pawn)
        {
            if (pawn?.Drawer?.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }

            PortraitsCache.SetDirty(pawn);
        }

        private static bool CanLeapTo(Pawn pawn, IntVec3 target)
        {
            return pawn != null &&
                   pawn.Map != null &&
                   pawn.Spawned &&
                   target.IsValid &&
                   target.InBounds(pawn.Map) &&
                   target.Standable(pawn.Map) &&
                   target != pawn.Position;
        }

        private static HediffComp_HulkIdentity GetIdentityComp(Pawn pawn)
        {
            return GetIdentityHediff(pawn)?.TryGetComp<HediffComp_HulkIdentity>();
        }

        private static void ThrowShockwaveCellFleck(IntVec3 cell, Map map, IntVec3 center, float radius, Color color, float scale)
        {
            if ((cell - center).LengthHorizontalSquared > radius * radius)
            {
                return;
            }

            FleckMaker.ThrowDustPuff(cell.ToVector3Shifted(), map, scale);
            if (ExplosionFlashFleck != null && cell.DistanceTo(center) >= radius - 1.25f)
            {
                FleckMaker.ThrowExplosionCell(cell, map, ExplosionFlashFleck, color);
            }
        }

        private static bool ColorsClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f &&
                   Mathf.Abs(a.g - b.g) < 0.02f &&
                   Mathf.Abs(a.b - b.b) < 0.02f &&
                   Mathf.Abs(a.a - b.a) < 0.02f;
        }

        private static void Transform(Pawn pawn, bool startManualTimer)
        {
            EnsureBannerState(pawn);
            if (pawn == null || IsTransformed(pawn))
            {
                return;
            }

            var identityComp = GetIdentityComp(pawn);
            identityComp?.ApplyHulkAppearance();
            var hediff = HediffMaker.MakeHediff(HulkDefOf.Drake_HulkForm, pawn);
            hediff.Severity = Mathf.Clamp(GetRageLevel(pawn), 0.30f, 1f);
            pawn.health.AddHediff(hediff);

            if (startManualTimer && HulkHealingUtility.Settings.enableTimeRestrictedHulk)
            {
                identityComp?.StartManualFormTimer(HulkHealingUtility.Settings.timeRestrictedHulkDurationSeconds * 60);
            }
            else if (!startManualTimer)
            {
                identityComp?.ClearManualFormTimer();
            }

            HulkPresentation.PlayAt(TransformSound, pawn);
            if (pawn.Map != null)
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "HulkMoteTransform".Translate(), HulkSkinColor);
            }
        }
    }

    [StaticConstructorOnStartup]
    internal static class HulkPresentation
    {
        public static readonly Texture2D TransformIcon = LoadIcon(
            new[] { "UI/Icons/Genes/Gene_BodyHulk", "UI/Abilities/GhoulFrenzy" },
            new Color(0.11f, 0.43f, 0.10f, 1f),
            new Color(0.50f, 0.92f, 0.36f, 1f));

        public static readonly Texture2D RevertIcon = LoadIcon(
            new[] { "UI/Icons/Genes/Gene_BodyStandard", "UI/Icons/Genes/Gene_KindInstinct" },
            new Color(0.18f, 0.18f, 0.18f, 1f),
            new Color(0.85f, 0.85f, 0.85f, 1f));

        public static readonly Texture2D GammaLeapIcon = LoadIcon(
            new[] { "UI/Abilities/Longjump", "UI/Abilities/MechLongJump" },
            new Color(0.07f, 0.32f, 0.05f, 1f),
            new Color(0.70f, 0.98f, 0.48f, 1f));

        public static readonly Texture2D HulkClapIcon = LoadIcon(
            new[] { "UI/Abilities/FireBurst", "UI/Abilities/VoidTerror" },
            new Color(0.16f, 0.27f, 0.12f, 1f),
            new Color(0.92f, 0.98f, 0.78f, 1f));

        public static readonly Texture2D BoulderThrowIcon = LoadIcon(
            new[] { "UI/Abilities/TransmuteSteel", "UI/Abilities/PiercingSpine" },
            new Color(0.26f, 0.20f, 0.14f, 1f),
            new Color(0.68f, 0.86f, 0.61f, 1f));

        public static SoundDef ResolveSound(params string[] defNames)
        {
            foreach (var defName in defNames)
            {
                var sound = DefDatabase<SoundDef>.GetNamedSilentFail(defName);
                if (sound != null)
                {
                    return sound;
                }
            }

            return null;
        }

        public static EffecterDef ResolveEffecter(params string[] defNames)
        {
            foreach (var defName in defNames)
            {
                var effecter = DefDatabase<EffecterDef>.GetNamedSilentFail(defName);
                if (effecter != null)
                {
                    return effecter;
                }
            }

            return null;
        }

        public static void PlayAt(SoundDef sound, Thing thing)
        {
            if (sound != null && thing?.MapHeld != null)
            {
                Verse.Sound.SoundStarter.PlayOneShot(sound, Verse.Sound.SoundInfo.InMap(new TargetInfo(thing.PositionHeld, thing.MapHeld)));
            }
        }

        public static void PlayAtCell(SoundDef sound, IntVec3 cell, Map map)
        {
            if (sound != null && map != null && cell.IsValid)
            {
                Verse.Sound.SoundStarter.PlayOneShot(sound, Verse.Sound.SoundInfo.InMap(new TargetInfo(cell, map)));
            }
        }

        private static Texture2D LoadIcon(IEnumerable<string> paths, Color background, Color accent)
        {
            foreach (var path in paths)
            {
                var texture = ContentFinder<Texture2D>.Get(path, reportFailure: false);
                if (texture != null)
                {
                    return texture;
                }
            }

            return CreateFallbackIcon(background, accent);
        }

        private static Texture2D CreateFallbackIcon(Color background, Color accent)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x < 4 || y < 4 || x >= size - 4 || y >= size - 4;
                    var cross = Mathf.Abs(x - y) < 4 || Mathf.Abs((size - 1 - x) - y) < 4;
                    pixels[y * size + x] = border ? Color.black : cross ? accent : background;
                }
            }

            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }
    }

    public sealed class HediffCompProperties_HulkIdentity : HediffCompProperties
    {
        public HediffCompProperties_HulkIdentity()
        {
            compClass = typeof(HediffComp_HulkIdentity);
        }
    }

    public sealed class HediffComp_HulkIdentity : HediffComp
    {
        private BodyTypeDef originalBodyType;
        private bool appearanceCaptured;
        private bool hadOriginalSkinOverride;
        private bool hasCustomHulkColor;
        private Thing lastAggressor;
        private int lastAggressorTick = -999999;
        private Thing currentRageTarget;
        private int rageTicksRemaining;
        private int maxForcedRageTicks;
        private int manualFormTicksRemaining;
        private int maxManualFormTicks;
        private float forcedRageSeverityFloor;
        private float originalSkinR;
        private float originalSkinG;
        private float originalSkinB;
        private float originalSkinA;
        private float customHulkColorR = HulkUtility.HulkSkinColor.r;
        private float customHulkColorG = HulkUtility.HulkSkinColor.g;
        private float customHulkColorB = HulkUtility.HulkSkinColor.b;
        private float customHulkColorA = HulkUtility.HulkSkinColor.a;

        public Thing CurrentRageTarget => currentRageTarget;
        public bool HasActiveRage => rageTicksRemaining > 0;
        public bool HasCustomHulkColor => hasCustomHulkColor;
        public int RageTicksRemaining => rageTicksRemaining;
        public int MaxForcedRageTicks => maxForcedRageTicks;
        public int ManualFormTicksRemaining => maxManualFormTicks > 0 ? manualFormTicksRemaining : 0;
        public bool ShouldShowForcedTimerBar => Pawn != null && HulkUtility.IsTransformed(Pawn) && HulkHealingUtility.Settings.enableForcedHulkRevert && HasActiveRage && maxForcedRageTicks > 0;
        public bool HasManualTimerInfo => Pawn != null && HulkUtility.IsTransformed(Pawn) && HulkHealingUtility.Settings.enableTimeRestrictedHulk && maxManualFormTicks > 0;
        public float ForcedRageFillPercent => maxForcedRageTicks > 0 ? Mathf.Clamp01(rageTicksRemaining / (float)maxForcedRageTicks) : 0f;
        public float ForcedRageSeverityFloor => HasActiveRage ? forcedRageSeverityFloor : 0f;
        public Color ResolvedHulkSkinColor => hasCustomHulkColor
            ? new Color(customHulkColorR, customHulkColorG, customHulkColorB, customHulkColorA)
            : HulkUtility.HulkSkinColor;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Defs.Look(ref originalBodyType, "originalBodyType");
            Scribe_Values.Look(ref appearanceCaptured, "appearanceCaptured");
            Scribe_Values.Look(ref hadOriginalSkinOverride, "hadOriginalSkinOverride");
            Scribe_Values.Look(ref hasCustomHulkColor, "hasCustomHulkColor", false);
            Scribe_References.Look(ref lastAggressor, "lastAggressor");
            Scribe_Values.Look(ref lastAggressorTick, "lastAggressorTick", -999999);
            Scribe_References.Look(ref currentRageTarget, "currentRageTarget");
            Scribe_Values.Look(ref rageTicksRemaining, "rageTicksRemaining", 0);
            Scribe_Values.Look(ref maxForcedRageTicks, "maxForcedRageTicks", 0);
            Scribe_Values.Look(ref manualFormTicksRemaining, "manualFormTicksRemaining", 0);
            Scribe_Values.Look(ref maxManualFormTicks, "maxManualFormTicks", 0);
            Scribe_Values.Look(ref forcedRageSeverityFloor, "forcedRageSeverityFloor", 0f);
            Scribe_Values.Look(ref originalSkinR, "originalSkinR", 0f);
            Scribe_Values.Look(ref originalSkinG, "originalSkinG", 0f);
            Scribe_Values.Look(ref originalSkinB, "originalSkinB", 0f);
            Scribe_Values.Look(ref originalSkinA, "originalSkinA", 1f);
            Scribe_Values.Look(ref customHulkColorR, "customHulkColorR", HulkUtility.HulkSkinColor.r);
            Scribe_Values.Look(ref customHulkColorG, "customHulkColorG", HulkUtility.HulkSkinColor.g);
            Scribe_Values.Look(ref customHulkColorB, "customHulkColorB", HulkUtility.HulkSkinColor.b);
            Scribe_Values.Look(ref customHulkColorA, "customHulkColorA", HulkUtility.HulkSkinColor.a);
        }

        public void RecordAggressor(Thing aggressor)
        {
            if (aggressor == null || aggressor == Pawn)
            {
                return;
            }

            lastAggressor = aggressor;
            lastAggressorTick = Find.TickManager?.TicksGame ?? 0;
        }

        public Thing ConsumeFreshAggressor(int maxAgeTicks)
        {
            var aggressor = lastAggressor;
            var age = (Find.TickManager?.TicksGame ?? 0) - lastAggressorTick;
            if (aggressor == null || age > maxAgeTicks)
            {
                aggressor = null;
            }

            lastAggressor = null;
            lastAggressorTick = -999999;
            return aggressor;
        }

        public void BeginRage(Thing target, int ticks, float severityFloor)
        {
            var safeTicks = Math.Max(0, ticks);
            currentRageTarget = target != Pawn && target != null && !target.Destroyed ? target : null;
            rageTicksRemaining = Math.Max(rageTicksRemaining, safeTicks);
            maxForcedRageTicks = Math.Max(maxForcedRageTicks, rageTicksRemaining);
            forcedRageSeverityFloor = Mathf.Max(forcedRageSeverityFloor, severityFloor);
        }

        public void TickRage()
        {
            if (rageTicksRemaining <= 0)
            {
                currentRageTarget = null;
                maxForcedRageTicks = 0;
                forcedRageSeverityFloor = 0f;
                return;
            }

            rageTicksRemaining--;
            if (currentRageTarget != null && currentRageTarget.Destroyed)
            {
                currentRageTarget = null;
            }

            if (rageTicksRemaining <= 0)
            {
                currentRageTarget = null;
                maxForcedRageTicks = 0;
                forcedRageSeverityFloor = 0f;
            }
        }

        public void StartManualFormTimer(int ticks)
        {
            if (ticks <= 0)
            {
                ClearManualFormTimer();
                return;
            }

            manualFormTicksRemaining = ticks;
            maxManualFormTicks = ticks;
        }

        public void ClearManualFormTimer()
        {
            manualFormTicksRemaining = 0;
            maxManualFormTicks = 0;
        }

        public bool TickManualFormTimer(bool allowExpiry)
        {
            if (maxManualFormTicks <= 0)
            {
                return false;
            }

            if (manualFormTicksRemaining > 0)
            {
                manualFormTicksRemaining--;
            }

            return allowExpiry && manualFormTicksRemaining <= 0;
        }

        public void ClearFormTimerStateIfInactive()
        {
            if (!HulkUtility.IsTransformed(Pawn))
            {
                ClearManualFormTimer();
            }
        }

        public void CaptureBaselineAppearance()
        {
            if (appearanceCaptured || Pawn?.story == null)
            {
                return;
            }

            originalBodyType = Pawn.story.bodyType;
            var skin = Pawn.story.SkinColor;
            originalSkinR = skin.r;
            originalSkinG = skin.g;
            originalSkinB = skin.b;
            originalSkinA = skin.a;
            hadOriginalSkinOverride = Pawn.story.skinColorOverride.HasValue;
            appearanceCaptured = true;
        }

        public void ApplyHulkAppearance()
        {
            if (Pawn?.story == null)
            {
                return;
            }

            CaptureBaselineAppearance();
            Pawn.story.bodyType = BodyTypeDefOf.Hulk;
            Pawn.story.skinColorOverride = ResolvedHulkSkinColor;
            HulkUtility.RefreshPawnVisuals(Pawn);
        }

        public void SetCustomHulkColor(Color color)
        {
            hasCustomHulkColor = true;
            customHulkColorR = Mathf.Clamp01(color.r);
            customHulkColorG = Mathf.Clamp01(color.g);
            customHulkColorB = Mathf.Clamp01(color.b);
            customHulkColorA = 1f;
            if (Pawn != null && HulkUtility.IsTransformed(Pawn))
            {
                ApplyHulkAppearance();
            }
        }

        public void ClearCustomHulkColor()
        {
            hasCustomHulkColor = false;
            customHulkColorR = HulkUtility.HulkSkinColor.r;
            customHulkColorG = HulkUtility.HulkSkinColor.g;
            customHulkColorB = HulkUtility.HulkSkinColor.b;
            customHulkColorA = HulkUtility.HulkSkinColor.a;
            if (Pawn != null && HulkUtility.IsTransformed(Pawn))
            {
                ApplyHulkAppearance();
            }
        }

        public void RestoreBannerAppearance()
        {
            if (Pawn?.story == null)
            {
                return;
            }

            if (appearanceCaptured)
            {
                Pawn.story.bodyType = originalBodyType ?? BodyTypeDefOf.Male;
                Pawn.story.skinColorOverride = hadOriginalSkinOverride
                    ? new Color(originalSkinR, originalSkinG, originalSkinB, originalSkinA)
                    : (Color?)null;
            }
            else
            {
                Pawn.story.skinColorOverride = null;
            }

            HulkUtility.RefreshPawnVisuals(Pawn);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn == null || !Pawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            if (HulkUtility.IsTransformed(Pawn))
            {
                if (ShouldShowForcedTimerBar)
                {
                    yield return new HulkForcedTimerGizmo(
                        ForcedRageFillPercent,
                        "HulkForcedTimerLabel".Translate(),
                        "HulkForcedTimerDuration".Translate(HulkTimerFormatting.FormatTicks(rageTicksRemaining)));
                }

                yield return new Command_Action
                {
                    defaultLabel = "HulkCommandRevertLabel".Translate(),
                    defaultDesc = "HulkCommandRevertDesc".Translate(),
                    icon = HulkPresentation.RevertIcon,
                    action = () => HulkUtility.Revert(Pawn)
                };

                yield return new Command_Action
                {
                    defaultLabel = "HulkCommandLeapLabel".Translate(),
                    defaultDesc = "HulkCommandLeapDesc".Translate(),
                    icon = HulkPresentation.GammaLeapIcon,
                    action = () => HulkUtility.BeginLeapTargeting(Pawn)
                };

                yield return new Command_Action
                {
                    defaultLabel = "HulkCommandClapLabel".Translate(),
                    defaultDesc = "HulkCommandClapDesc".Translate(),
                    icon = HulkPresentation.HulkClapIcon,
                    action = () =>
                    {
                        if (!HulkUtility.PerformClap(Pawn))
                        {
                            Messages.Message("HulkMessageNothingToSmash".Translate(), MessageTypeDefOf.RejectInput, false);
                        }
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "HulkCommandBoulderLabel".Translate(),
                    defaultDesc = "HulkCommandBoulderDesc".Translate(),
                    icon = HulkPresentation.BoulderThrowIcon,
                    action = () => HulkUtility.BeginBoulderTargeting(Pawn)
                };
            }
            else
            {
                yield return new Command_Action
                {
                    defaultLabel = "HulkCommandTransformLabel".Translate(),
                    defaultDesc = "HulkCommandTransformDesc".Translate(),
                    icon = HulkPresentation.TransformIcon,
                    action = () => HulkUtility.Transform(Pawn)
                };
            }
        }
    }

    public sealed class HediffCompProperties_HulkForm : HediffCompProperties
    {
        public int ticksPerPulse = 45;
        public float healPerPulse = 4f;

        public HediffCompProperties_HulkForm()
        {
            compClass = typeof(HediffComp_HulkForm);
        }
    }

    public sealed class HediffComp_HulkForm : HediffComp
    {
        public HediffCompProperties_HulkForm Props => (HediffCompProperties_HulkForm)props;

        public override string CompLabelInBracketsExtra
        {
            get
            {
                var identityComp = GetIdentityComp();
                return identityComp?.HasManualTimerInfo == true
                    ? HulkTimerFormatting.FormatTicks(identityComp.ManualFormTicksRemaining)
                    : null;
            }
        }

        public override string CompTipStringExtra
        {
            get
            {
                var identityComp = GetIdentityComp();
                return identityComp?.HasManualTimerInfo == true
                    ? "HulkManualTimerInspect".Translate(HulkTimerFormatting.FormatTicks(identityComp.ManualFormTicksRemaining))
                    : null;
            }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (Pawn == null)
            {
                return;
            }

            HulkUtility.EnsureHulkAppearance(Pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            GetIdentityComp()?.ClearManualFormTimer();
        }

        private HediffComp_HulkIdentity GetIdentityComp()
        {
            return Pawn?.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkIdentity)?.TryGetComp<HediffComp_HulkIdentity>();
        }
    }

    public sealed class HulkGammaLeapFlyer : PawnFlyer
    {
        protected override void RespawnPawn()
        {
            var pawn = FlyingPawn;
            base.RespawnPawn();
            HulkUtility.ResolveGammaLeapLanding(pawn);
        }
    }

    public static class HulkDebugActions
    {
        private static Pawn lastGreenDoorDebugPawn;

        [DebugAction("Incredible Hulk", "Spawn Bruce Banner", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnBruceBanner()
        {
            if (Find.CurrentMap == null)
            {
                return;
            }

            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer, null);
            HulkUtility.EnsureBannerState(pawn);
            var cell = CellFinder.RandomClosewalkCellNear(Find.CurrentMap.Center, Find.CurrentMap, 8, entry => entry.Standable(Find.CurrentMap));
            GenSpawn.Spawn(pawn, cell, Find.CurrentMap);
            Find.Selector.Select(pawn);
        }

        [DebugAction("Incredible Hulk", "Transform Selected Pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TransformSelectedPawn()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("HulkDebugSelectPawnFirst".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            HulkUtility.Transform(pawn);
        }

        [DebugAction("Incredible Hulk", "Revert Selected Pawn", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void RevertSelectedPawn()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("HulkDebugSelectPawnFirst".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            HulkUtility.Revert(pawn);
        }

        [DebugAction("Incredible Hulk", "Start Green Door Test (Selected Pawn)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void StartGreenDoorTest()
        {
            if (!TryGetSelectedLivingGammaPawn(out var pawn))
            {
                return;
            }

            if (!HulkHealingUtility.Settings.autoResurrect)
            {
                Messages.Message("HulkDebugGreenDoorAutoResurrectDisabled".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!HulkHealingUtility.Settings.enableGreenDoorReturn)
            {
                Messages.Message("HulkDebugGreenDoorReturnDisabled".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (HulkGreenDoorManager.HasScheduledReturn(pawn))
            {
                Messages.Message("HulkDebugGreenDoorTestAlreadyPending".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return;
            }

            lastGreenDoorDebugPawn = pawn;
            HulkImmortalityManager.SuppressImmediateResurrectionForNextDeath(pawn);
            pawn.Kill(null, null);

            var corpse = pawn.Corpse;
            if (!pawn.Dead || corpse == null)
            {
                Messages.Message("HulkDebugGreenDoorTestCorpseMissing".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return;
            }

            corpse.Destroy(DestroyMode.Vanish);
            if (!HulkGreenDoorManager.HasScheduledReturn(pawn))
            {
                Messages.Message("HulkDebugGreenDoorTestScheduleFailed".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("HulkDebugGreenDoorTestStarted".Translate(pawn.LabelShortCap), MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Incredible Hulk", "Force Green Door Manifest Now (Selected Pawn)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ForceGreenDoorManifestNow()
        {
            if (!TryGetGreenDoorTargetPawn(out var pawn))
            {
                return;
            }

            if (!HulkGreenDoorManager.HasScheduledReturn(pawn))
            {
                Messages.Message("HulkDebugGreenDoorForcePendingMissing".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (!HulkGreenDoorManager.TryForceManifestNow(pawn))
            {
                Messages.Message("HulkDebugGreenDoorForceFailed".Translate(pawn.LabelShortCap), MessageTypeDefOf.RejectInput, false);
                return;
            }

            Messages.Message("HulkDebugGreenDoorForceSuccess".Translate(pawn.LabelShortCap), MessageTypeDefOf.TaskCompletion, false);
        }

        [DebugAction("Incredible Hulk", "Inspect Green Door State (Selected Pawn)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void InspectGreenDoorState()
        {
            if (!TryGetGreenDoorTargetPawn(out var pawn))
            {
                return;
            }

            var summary = HulkGreenDoorManager.GetDebugStateSummary(pawn);
            Log.Message($"[Drake.Hulk] {summary}");
            Messages.Message(summary, MessageTypeDefOf.TaskCompletion, false);
        }

        private static Thing GetSelectedDebugThing()
        {
            return Find.Selector?.SingleSelectedThing;
        }

        private static Pawn GetSelectedPawnReference()
        {
            switch (GetSelectedDebugThing())
            {
                case Pawn pawn:
                    return pawn;
                case Corpse corpse:
                    return corpse.InnerPawn;
                default:
                    return null;
            }
        }

        private static bool TryGetSelectedLivingGammaPawn(out Pawn pawn)
        {
            pawn = GetSelectedPawnReference();
            if (pawn == null || pawn.Dead || !HulkUtility.HasGammaIdentity(pawn))
            {
                Messages.Message("HulkDebugGreenDoorNeedsLivingGammaPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        private static bool TryGetGreenDoorTargetPawn(out Pawn pawn)
        {
            pawn = GetSelectedPawnReference() ?? lastGreenDoorDebugPawn;
            if (pawn == null)
            {
                Messages.Message("HulkDebugGreenDoorNoTarget".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (!HulkUtility.HasGammaIdentity(pawn) && !HulkGreenDoorManager.HasScheduledReturn(pawn))
            {
                Messages.Message("HulkDebugGreenDoorNeedsLivingGammaPawn".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        public static IReadOnlyList<string> GetActionNames()
        {
            return typeof(HulkDebugActions)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .SelectMany(method => method.GetCustomAttributes(typeof(DebugActionAttribute), false).Cast<DebugActionAttribute>())
                .Select(attribute => attribute.name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .OrderBy(name => name)
                .ToList();
        }
    }
}
