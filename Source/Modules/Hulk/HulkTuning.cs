using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace Drake.Hulk
{
    [StaticConstructorOnStartup]
    internal static class HulkRuntimeBootstrap
    {
        static HulkRuntimeBootstrap()
        {
            LongEventHandler.ExecuteWhenFinished(HulkRuntimeTuning.ApplyCurrentSettings);
        }
    }

    internal static class HulkRuntimeTuning
    {
        private const float BaseClapRadius = 6.9f;
        private const int BaseClapStructureDamage = 800;
        private const int BaseClapPawnDamage = 220;
        private const float BaseLeapRadius = 4.2f;
        private const int BaseLeapStructureDamage = 340;
        private const int BaseLeapPawnDamage = 80;
        private const int BaseBoulderThrowRange = 38;

        private static bool initialized;
        private static readonly FieldInfo ProjectileDamageAmountBaseField = typeof(ProjectileProperties).GetField("damageAmountBase", BindingFlags.Instance | BindingFlags.NonPublic);
        private static List<HulkStageBaseline> stageBaselines;
        private static float baseHealPerPulse;
        private static float baseBoulderExplosionRadius;
        private static int baseBoulderDamage;

        public static void ApplyCurrentSettings()
        {
            EnsureInitialized();
            var settings = HulkHealingUtility.Settings;
            settings.EnsureValid();

            ApplyFormTuning(settings);
            ApplyBoulderTuning(settings);
            NotifyAllGammaPawns(settings);
        }

        public static float GetClapRadius()
        {
            EnsureInitialized();
            return Mathf.Max(0.5f, BaseClapRadius * HulkHealingUtility.Settings.clapRadiusMultiplier);
        }

        public static int GetClapStructureDamage()
        {
            EnsureInitialized();
            return Mathf.Max(1, Mathf.RoundToInt(BaseClapStructureDamage * HulkHealingUtility.Settings.clapStructureDamageMultiplier));
        }

        public static int GetClapPawnDamage()
        {
            EnsureInitialized();
            return Mathf.Max(1, Mathf.RoundToInt(BaseClapPawnDamage * HulkHealingUtility.Settings.clapPawnDamageMultiplier));
        }

        public static float GetLeapRadius()
        {
            EnsureInitialized();
            return Mathf.Max(0.5f, BaseLeapRadius * HulkHealingUtility.Settings.leapRadiusMultiplier);
        }

        public static int GetLeapStructureDamage()
        {
            EnsureInitialized();
            return Mathf.Max(1, Mathf.RoundToInt(BaseLeapStructureDamage * HulkHealingUtility.Settings.leapStructureDamageMultiplier));
        }

        public static int GetLeapPawnDamage()
        {
            EnsureInitialized();
            return Mathf.Max(1, Mathf.RoundToInt(BaseLeapPawnDamage * HulkHealingUtility.Settings.leapPawnDamageMultiplier));
        }

        public static int GetBoulderThrowRange()
        {
            EnsureInitialized();
            return Mathf.Max(1, Mathf.RoundToInt(BaseBoulderThrowRange * HulkHealingUtility.Settings.boulderThrowRangeMultiplier));
        }

        public static float GetBoulderExplosionRadius()
        {
            EnsureInitialized();
            return HulkDefOf.Drake_HulkBoulderProjectile?.projectile?.explosionRadius ?? baseBoulderExplosionRadius;
        }

        public static int GetBoulderDamage()
        {
            EnsureInitialized();
            return GetProjectileDamageAmount(HulkDefOf.Drake_HulkBoulderProjectile?.projectile, baseBoulderDamage);
        }

        public static SoundDef GetSharedImpactSound()
        {
            EnsureInitialized();
            var projectile = HulkDefOf.Drake_HulkBoulderProjectile?.projectile;
            return projectile?.soundExplode ??
                   projectile?.damageDef?.soundExplosion ??
                   projectile?.soundImpact;
        }

        public static string GetSharedImpactSoundDefName()
        {
            return GetSharedImpactSound()?.defName ?? string.Empty;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            stageBaselines = (HulkDefOf.Drake_HulkForm?.stages ?? new List<HediffStage>())
                .Select(CaptureStageBaseline)
                .ToList();
            baseHealPerPulse = HulkDefOf.Drake_HulkForm?.comps?.OfType<HediffCompProperties_HulkForm>().FirstOrDefault()?.healPerPulse ?? 3f;
            baseBoulderExplosionRadius = HulkDefOf.Drake_HulkBoulderProjectile?.projectile?.explosionRadius ?? 4.6f;
            baseBoulderDamage = GetProjectileDamageAmount(HulkDefOf.Drake_HulkBoulderProjectile?.projectile, 520);
            initialized = true;
        }

        private static void ApplyFormTuning(IncredibleHulkSettings settings)
        {
            var stages = HulkDefOf.Drake_HulkForm?.stages;
            if (stages == null || stageBaselines == null)
            {
                return;
            }

            for (var index = 0; index < stages.Count && index < stageBaselines.Count; index++)
            {
                var stage = stages[index];
                var baseline = stageBaselines[index];

                SetStatOffset(stage, StatDefOf.MoveSpeed, baseline.moveSpeed * settings.moveSpeedMultiplier);
                SetStatOffset(stage, StatDefOf.CarryingCapacity, baseline.carryCapacity * settings.carryCapacityMultiplier);
                SetStatOffset(stage, StatDefOf.MeleeHitChance, baseline.meleeHitChance * settings.meleeHitChanceMultiplier);
                SetStatOffset(stage, StatDefOf.MeleeDamageFactor, baseline.meleeDamageFactor * settings.meleeDamageMultiplier);
                SetStatOffset(stage, StatDefOf.MeleeCooldownFactor, ScaleFactorOffset(baseline.meleeCooldownFactor, settings.meleeSpeedMultiplier, 0.05f, 8f));
                SetStatOffset(stage, StatDefOf.MeleeDodgeChance, baseline.meleeDodgeChance * settings.meleeDodgeMultiplier);
                SetStatOffset(stage, StatDefOf.ArmorRating_Blunt, baseline.armorBlunt * settings.toughnessMultiplier);
                SetStatOffset(stage, StatDefOf.ArmorRating_Sharp, baseline.armorSharp * settings.toughnessMultiplier);
                SetStatOffset(stage, StatDefOf.ArmorRating_Heat, baseline.armorHeat * settings.toughnessMultiplier);
                SetStatOffset(stage, StatDefOf.IncomingDamageFactor, ScaleFactorOffset(baseline.incomingDamageFactor, settings.toughnessMultiplier, 0.01f, 10f));
                SetStatOffset(stage, StatDefOf.PainShockThreshold, baseline.painShockThreshold * settings.toughnessMultiplier);
            }

            var formCompProps = HulkDefOf.Drake_HulkForm?.comps?.OfType<HediffCompProperties_HulkForm>().FirstOrDefault();
            if (formCompProps != null)
            {
                formCompProps.healPerPulse = Mathf.Max(0.1f, baseHealPerPulse * settings.regenerationMultiplier);
            }
        }

        private static void ApplyBoulderTuning(IncredibleHulkSettings settings)
        {
            var projectileProps = HulkDefOf.Drake_HulkBoulderProjectile?.projectile;
            if (projectileProps == null)
            {
                return;
            }

            SetProjectileDamageAmount(projectileProps, Mathf.Max(1, Mathf.RoundToInt(baseBoulderDamage * settings.boulderImpactDamageMultiplier)));
            projectileProps.explosionRadius = Mathf.Max(0.5f, baseBoulderExplosionRadius * settings.boulderImpactRadiusMultiplier);
        }

        private static void NotifyAllGammaPawns(IncredibleHulkSettings settings)
        {
            if (Current.Game == null)
            {
                return;
            }

            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawnsSpawned)
                {
                    var identityComp = GetIdentityComp(pawn);
                    if (identityComp == null)
                    {
                        continue;
                    }

                    if (!settings.enableTimeRestrictedHulk)
                    {
                        identityComp.ClearManualFormTimer();
                    }

                    var form = HulkUtility.GetFormHediff(pawn);
                    if (form != null)
                    {
                        try
                        {
                            pawn.health.Notify_HediffChanged(form);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private static HulkStageBaseline CaptureStageBaseline(HediffStage stage)
        {
            return new HulkStageBaseline
            {
                moveSpeed = GetStatOffset(stage, StatDefOf.MoveSpeed),
                carryCapacity = GetStatOffset(stage, StatDefOf.CarryingCapacity),
                meleeHitChance = GetStatOffset(stage, StatDefOf.MeleeHitChance),
                meleeDamageFactor = GetStatOffset(stage, StatDefOf.MeleeDamageFactor),
                meleeCooldownFactor = GetStatOffset(stage, StatDefOf.MeleeCooldownFactor),
                meleeDodgeChance = GetStatOffset(stage, StatDefOf.MeleeDodgeChance),
                armorBlunt = GetStatOffset(stage, StatDefOf.ArmorRating_Blunt),
                armorSharp = GetStatOffset(stage, StatDefOf.ArmorRating_Sharp),
                armorHeat = GetStatOffset(stage, StatDefOf.ArmorRating_Heat),
                incomingDamageFactor = GetStatOffset(stage, StatDefOf.IncomingDamageFactor),
                painShockThreshold = GetStatOffset(stage, StatDefOf.PainShockThreshold)
            };
        }

        private static float GetStatOffset(HediffStage stage, StatDef stat)
        {
            return stage?.statOffsets?.FirstOrDefault(modifier => modifier?.stat == stat)?.value ?? 0f;
        }

        private static void SetStatOffset(HediffStage stage, StatDef stat, float value)
        {
            if (stage == null || stat == null)
            {
                return;
            }

            if (stage.statOffsets == null)
            {
                stage.statOffsets = new List<StatModifier>();
            }

            var modifier = stage.statOffsets.FirstOrDefault(entry => entry?.stat == stat);
            if (modifier == null)
            {
                modifier = new StatModifier { stat = stat };
                stage.statOffsets.Add(modifier);
            }

            modifier.value = value;
        }

        private static float ScaleFactorOffset(float baseOffset, float multiplier, float minEffectiveFactor, float maxEffectiveFactor)
        {
            var baseEffectiveFactor = 1f + baseOffset;
            var scaledEffectiveFactor = Mathf.Clamp(baseEffectiveFactor / Mathf.Max(0.01f, multiplier), minEffectiveFactor, maxEffectiveFactor);
            return scaledEffectiveFactor - 1f;
        }

        private static int GetProjectileDamageAmount(ProjectileProperties projectileProps, int fallbackValue)
        {
            if (projectileProps == null || ProjectileDamageAmountBaseField == null)
            {
                return fallbackValue;
            }

            try
            {
                return (int)ProjectileDamageAmountBaseField.GetValue(projectileProps);
            }
            catch
            {
                return fallbackValue;
            }
        }

        private static void SetProjectileDamageAmount(ProjectileProperties projectileProps, int value)
        {
            if (projectileProps == null || ProjectileDamageAmountBaseField == null)
            {
                return;
            }

            ProjectileDamageAmountBaseField.SetValue(projectileProps, value);
        }

        private static HediffComp_HulkIdentity GetIdentityComp(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetFirstHediffOfDef(HulkDefOf.Drake_HulkIdentity)?.TryGetComp<HediffComp_HulkIdentity>();
        }

        private sealed class HulkStageBaseline
        {
            public float moveSpeed;
            public float carryCapacity;
            public float meleeHitChance;
            public float meleeDamageFactor;
            public float meleeCooldownFactor;
            public float meleeDodgeChance;
            public float armorBlunt;
            public float armorSharp;
            public float armorHeat;
            public float incomingDamageFactor;
            public float painShockThreshold;
        }
    }

    internal static class HulkTimerFormatting
    {
        public static string FormatTicks(int ticks)
        {
            var totalSeconds = Mathf.Max(0, Mathf.CeilToInt(ticks / 60f));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:00}";
        }
    }

    internal sealed class HulkForcedTimerGizmo : Gizmo
    {
        private readonly float fillPercent;
        private readonly string label;
        private readonly string duration;

        public HulkForcedTimerGizmo(float fillPercent, string label, string duration)
        {
            this.fillPercent = Mathf.Clamp01(fillPercent);
            this.label = label ?? string.Empty;
            this.duration = duration ?? string.Empty;
            Order = -90f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            var outerRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(outerRect);
            var innerRect = outerRect.ContractedBy(6f);
            var labelRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 20f);
            var barRect = new Rect(innerRect.x, innerRect.y + 24f, innerRect.width, 20f);
            var durationRect = new Rect(innerRect.x, innerRect.y + 48f, innerRect.width, 18f);
            var oldAnchor = Text.Anchor;
            var oldFont = Text.Font;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, label);
            Widgets.FillableBar(barRect, fillPercent);
            Widgets.DrawBox(barRect);
            Widgets.Label(durationRect, duration);
            Text.Anchor = oldAnchor;
            Text.Font = oldFont;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
