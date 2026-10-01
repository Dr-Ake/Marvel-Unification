using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace CyclopsGene
{
    public class CyclopsGeneSettings : ModSettings
    {
        public const int CurrentSettingsVersion = 2;
        public const float WarmupDefault = 1f;
        public const int CooldownDefault = 120;
        public const int DamageDefault = 25;
        public const float BasicRangeDefault = 25f;
        public const int RapidDamageDefault = 8;
        public const int RapidPulseCountDefault = 4;
        public const float RapidWarmupDefault = 0.2f;
        public const int RapidCooldownDefault = 180;
        public const float RapidRangeDefault = 20f;
        public const int FocusedDamageDefault = 8;
        public const float FocusedWarmupDefault = 1.2f;
        public const int FocusedDurationDefault = 105;
        public const int FocusedCooldownDefault = 720;
        public const float FocusedRangeDefault = 35f;
        public const int ConeDamageDefault = 150;
        public const float ConeWarmupDefault = 2.5f;
        public const float ConeRangeDefault = 18f;
        public const float ConeAngleDefault = 50f;
        public const int ConeCooldownDefault = 2700;
        public const float StrainRecoveryDefault = 3.5f;
        public const float LeadershipRadiusDefault = 8f;

        public float warmupTime = WarmupDefault;
        public int cooldownTicks = CooldownDefault;
        public int damageAmount = DamageDefault;
        public float basicRange = BasicRangeDefault;
        public int rapidDamage = RapidDamageDefault;
        public int rapidPulseCount = RapidPulseCountDefault;
        public float rapidWarmupTime = RapidWarmupDefault;
        public int rapidCooldownTicks = RapidCooldownDefault;
        public float rapidRange = RapidRangeDefault;
        public int focusedDamage = FocusedDamageDefault;
        public float focusedWarmupTime = FocusedWarmupDefault;
        public int focusedDurationTicks = FocusedDurationDefault;
        public int focusedCooldownTicks = FocusedCooldownDefault;
        public float focusedRange = FocusedRangeDefault;
        public int coneDamage = ConeDamageDefault;
        public float coneWarmupTime = ConeWarmupDefault;
        public float coneRange = ConeRangeDefault;
        public float coneAngle = ConeAngleDefault;
        public int coneCooldownTicks = ConeCooldownDefault;
        public float strainRecoveryPerSecond = StrainRecoveryDefault;
        public bool leadershipEnabled = true;
        public float leadershipRadius = LeadershipRadiusDefault;
        public bool visorDependencyEnabled;
        public int settingsVersion = CurrentSettingsVersion;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref warmupTime, "warmupTime", WarmupDefault);
            Scribe_Values.Look(ref cooldownTicks, "cooldownTicks", CooldownDefault);
            Scribe_Values.Look(ref damageAmount, "damageAmount", DamageDefault);
            Scribe_Values.Look(ref basicRange, "basicRange", BasicRangeDefault);
            Scribe_Values.Look(ref rapidDamage, "rapidDamage", RapidDamageDefault);
            Scribe_Values.Look(ref rapidPulseCount, "rapidPulseCount", RapidPulseCountDefault);
            Scribe_Values.Look(ref rapidWarmupTime, "rapidWarmupTime", RapidWarmupDefault);
            Scribe_Values.Look(ref rapidCooldownTicks, "rapidCooldownTicks", RapidCooldownDefault);
            Scribe_Values.Look(ref rapidRange, "rapidRange", RapidRangeDefault);
            Scribe_Values.Look(ref focusedDamage, "focusedDamage", FocusedDamageDefault);
            Scribe_Values.Look(ref focusedWarmupTime, "focusedWarmupTime", FocusedWarmupDefault);
            Scribe_Values.Look(ref focusedDurationTicks, "focusedDurationTicks", FocusedDurationDefault);
            Scribe_Values.Look(ref focusedCooldownTicks, "focusedCooldownTicks", FocusedCooldownDefault);
            Scribe_Values.Look(ref focusedRange, "focusedRange", FocusedRangeDefault);
            Scribe_Values.Look(ref coneDamage, "coneDamage", ConeDamageDefault);
            Scribe_Values.Look(ref coneWarmupTime, "coneWarmupTime", ConeWarmupDefault);
            Scribe_Values.Look(ref coneRange, "coneRange", ConeRangeDefault);
            Scribe_Values.Look(ref coneAngle, "coneAngle", ConeAngleDefault);
            Scribe_Values.Look(ref coneCooldownTicks, "coneCooldownTicks", ConeCooldownDefault);
            Scribe_Values.Look(ref strainRecoveryPerSecond, "strainRecoveryPerSecond", StrainRecoveryDefault);
            Scribe_Values.Look(ref leadershipEnabled, "leadershipEnabled", true);
            Scribe_Values.Look(ref leadershipRadius, "leadershipRadius", LeadershipRadiusDefault);
            Scribe_Values.Look(ref visorDependencyEnabled, "visorDependencyEnabled", false);
            Scribe_Values.Look(ref settingsVersion, "settingsVersion", 0);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && settingsVersion < CurrentSettingsVersion)
            {
                if (coneDamage == 42)
                {
                    coneDamage = ConeDamageDefault;
                }

                settingsVersion = CurrentSettingsVersion;
            }
        }

        public void ResetToDefaults()
        {
            warmupTime = WarmupDefault;
            cooldownTicks = CooldownDefault;
            damageAmount = DamageDefault;
            basicRange = BasicRangeDefault;
            rapidDamage = RapidDamageDefault;
            rapidPulseCount = RapidPulseCountDefault;
            rapidWarmupTime = RapidWarmupDefault;
            rapidCooldownTicks = RapidCooldownDefault;
            rapidRange = RapidRangeDefault;
            focusedDamage = FocusedDamageDefault;
            focusedWarmupTime = FocusedWarmupDefault;
            focusedDurationTicks = FocusedDurationDefault;
            focusedCooldownTicks = FocusedCooldownDefault;
            focusedRange = FocusedRangeDefault;
            coneDamage = ConeDamageDefault;
            coneWarmupTime = ConeWarmupDefault;
            coneRange = ConeRangeDefault;
            coneAngle = ConeAngleDefault;
            coneCooldownTicks = ConeCooldownDefault;
            strainRecoveryPerSecond = StrainRecoveryDefault;
            leadershipEnabled = true;
            leadershipRadius = LeadershipRadiusDefault;
            visorDependencyEnabled = false;
            settingsVersion = CurrentSettingsVersion;
        }
    }

    public class CyclopsGeneMod : Mod
    {
        public static CyclopsGeneSettings Settings;

        private Vector2 scrollPosition;

        public CyclopsGeneMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CyclopsGeneSettings>();
            ApplySettings();
            LongEventHandler.ExecuteWhenFinished(ApplySettings);
        }

        public override string SettingsCategory()
        {
            return "Cyclops X-Gene";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect outerRect = inRect.ContractedBy(4f);
            Rect viewRect = new Rect(0f, 0f, outerRect.width - 18f, 1500f);
            Widgets.BeginScrollView(outerRect, ref scrollPosition, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            listing.Label("CyclopsSettingsBasicHeader".Translate());
            listing.GapLine();
            DrawFloatSlider(listing, "CyclopsSettingsWarmup".Translate(Settings.warmupTime.ToString("0.0")),
                ref Settings.warmupTime, 0f, 5f, 0.1f);
            DrawIntSlider(listing, "CyclopsSettingsCooldown".Translate(TicksToSeconds(Settings.cooldownTicks)),
                ref Settings.cooldownTicks, 0, 600);
            DrawIntSlider(listing, "CyclopsSettingsDamage".Translate(Settings.damageAmount),
                ref Settings.damageAmount, 1, 500);
            DrawFloatSlider(listing, "CyclopsSettingsBasicRange".Translate(Settings.basicRange.ToString("0")),
                ref Settings.basicRange, 1f, 100f, 1f);

            listing.Gap();
            listing.Label("CyclopsSettingsAdvancedHeader".Translate());
            listing.GapLine();
            DrawIntSlider(listing, "CyclopsSettingsRapidDamage".Translate(Settings.rapidDamage),
                ref Settings.rapidDamage, 1, 250);
            DrawIntSlider(listing, "CyclopsSettingsRapidPulses".Translate(Settings.rapidPulseCount),
                ref Settings.rapidPulseCount, 1, 30);
            DrawFloatSlider(listing, "CyclopsSettingsRapidWarmup".Translate(Settings.rapidWarmupTime.ToString("0.0")),
                ref Settings.rapidWarmupTime, 0f, 10f, 0.1f);
            DrawIntSlider(listing, "CyclopsSettingsRapidCooldown".Translate(TicksToSeconds(Settings.rapidCooldownTicks)),
                ref Settings.rapidCooldownTicks, 0, 3600);
            DrawFloatSlider(listing, "CyclopsSettingsRapidRange".Translate(Settings.rapidRange.ToString("0")),
                ref Settings.rapidRange, 1f, 100f, 1f);
            DrawIntSlider(listing, "CyclopsSettingsFocusedDamage".Translate(Settings.focusedDamage),
                ref Settings.focusedDamage, 1, 250);
            DrawFloatSlider(listing, "CyclopsSettingsFocusedWarmup".Translate(Settings.focusedWarmupTime.ToString("0.0")),
                ref Settings.focusedWarmupTime, 0f, 10f, 0.1f);
            DrawIntSlider(listing, "CyclopsSettingsFocusedDuration".Translate(TicksToSeconds(Settings.focusedDurationTicks)),
                ref Settings.focusedDurationTicks, 15, 1200);
            DrawIntSlider(listing, "CyclopsSettingsFocusedCooldown".Translate(TicksToSeconds(Settings.focusedCooldownTicks)),
                ref Settings.focusedCooldownTicks, 0, 7200);
            DrawFloatSlider(listing, "CyclopsSettingsFocusedRange".Translate(Settings.focusedRange.ToString("0")),
                ref Settings.focusedRange, 1f, 150f, 1f);
            DrawIntSlider(listing, "CyclopsSettingsConeDamage".Translate(Settings.coneDamage),
                ref Settings.coneDamage, 5, 1000);
            DrawFloatSlider(listing, "CyclopsSettingsConeWarmup".Translate(Settings.coneWarmupTime.ToString("0.0")),
                ref Settings.coneWarmupTime, 0f, 10f, 0.1f);
            DrawFloatSlider(listing, "CyclopsSettingsConeRange".Translate(Settings.coneRange.ToString("0")),
                ref Settings.coneRange, 5f, 100f, 1f);
            DrawFloatSlider(listing, "CyclopsSettingsConeAngle".Translate(Settings.coneAngle.ToString("0")),
                ref Settings.coneAngle, 10f, 180f, 1f);
            DrawIntSlider(listing, "CyclopsSettingsConeCooldown".Translate(TicksToSeconds(Settings.coneCooldownTicks)),
                ref Settings.coneCooldownTicks, 0, 14400);

            listing.Gap();
            listing.Label("CyclopsSettingsUtilityHeader".Translate());
            listing.GapLine();
            DrawFloatSlider(listing,
                "CyclopsSettingsRecovery".Translate(Settings.strainRecoveryPerSecond.ToString("0.0")),
                ref Settings.strainRecoveryPerSecond, 0.1f, 50f, 0.1f);
            listing.CheckboxLabeled("CyclopsSettingsLeadership".Translate(), ref Settings.leadershipEnabled,
                "CyclopsSettingsLeadershipTooltip".Translate());
            DrawFloatSlider(listing, "CyclopsSettingsLeadershipRadius".Translate(Settings.leadershipRadius.ToString("0")),
                ref Settings.leadershipRadius, 1f, 100f, 1f);
            listing.CheckboxLabeled("CyclopsSettingsVisorDependency".Translate(), ref Settings.visorDependencyEnabled,
                "CyclopsSettingsVisorDependencyTooltip".Translate());

            listing.GapLine();
            if (listing.ButtonText("CyclopsResetToDefaults".Translate()))
            {
                Settings.ResetToDefaults();
            }

            listing.End();
            Widgets.EndScrollView();
            ApplySettings();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            ApplySettings();
        }

        public static void ApplySettings()
        {
            if (Settings == null)
            {
                return;
            }

            ClampSettings();
            ApplyAbilitySettings("Cyclops_OpticBlast", Settings.cooldownTicks, Settings.warmupTime,
                Settings.basicRange + 0.9f);
            ApplyAbilitySettings("Cyclops_OpticVolley", Settings.rapidCooldownTicks, Settings.rapidWarmupTime,
                Settings.rapidRange + 0.9f);
            ApplyAbilitySettings("Cyclops_FocusedBeam", Settings.focusedCooldownTicks, Settings.focusedWarmupTime,
                Settings.focusedRange + 0.9f);
            ApplyAbilitySettings("Cyclops_FullApertureBlast", Settings.coneCooldownTicks, Settings.coneWarmupTime,
                Settings.coneRange + 0.9f);
            ApplyLegacyProjectileDamage();
        }

        private static void ClampSettings()
        {
            Settings.warmupTime = Mathf.Clamp(Settings.warmupTime, 0f, 5f);
            Settings.cooldownTicks = Mathf.Clamp(Settings.cooldownTicks, 0, 600);
            Settings.damageAmount = Mathf.Clamp(Settings.damageAmount, 1, 500);
            Settings.basicRange = Mathf.Clamp(Settings.basicRange, 1f, 100f);
            Settings.rapidDamage = Mathf.Clamp(Settings.rapidDamage, 1, 250);
            Settings.rapidPulseCount = Mathf.Clamp(Settings.rapidPulseCount, 1, 30);
            Settings.rapidWarmupTime = Mathf.Clamp(Settings.rapidWarmupTime, 0f, 10f);
            Settings.rapidCooldownTicks = Mathf.Clamp(Settings.rapidCooldownTicks, 0, 3600);
            Settings.rapidRange = Mathf.Clamp(Settings.rapidRange, 1f, 100f);
            Settings.focusedDamage = Mathf.Clamp(Settings.focusedDamage, 1, 250);
            Settings.focusedWarmupTime = Mathf.Clamp(Settings.focusedWarmupTime, 0f, 10f);
            Settings.focusedDurationTicks = Mathf.Clamp(Settings.focusedDurationTicks, 15, 1200);
            Settings.focusedCooldownTicks = Mathf.Clamp(Settings.focusedCooldownTicks, 0, 7200);
            Settings.focusedRange = Mathf.Clamp(Settings.focusedRange, 1f, 150f);
            Settings.coneDamage = Mathf.Clamp(Settings.coneDamage, 5, 1000);
            Settings.coneWarmupTime = Mathf.Clamp(Settings.coneWarmupTime, 0f, 10f);
            Settings.coneRange = Mathf.Clamp(Settings.coneRange, 5f, 100f);
            Settings.coneAngle = Mathf.Clamp(Settings.coneAngle, 10f, 180f);
            Settings.coneCooldownTicks = Mathf.Clamp(Settings.coneCooldownTicks, 0, 14400);
            Settings.strainRecoveryPerSecond = Mathf.Clamp(Settings.strainRecoveryPerSecond, 0.1f, 50f);
            Settings.leadershipRadius = Mathf.Clamp(Settings.leadershipRadius, 1f, 100f);
        }

        private static void ApplyAbilitySettings(string defName, int cooldownTicks, float warmup, float range)
        {
            AbilityDef abilityDef = DefDatabase<AbilityDef>.GetNamedSilentFail(defName);
            if (abilityDef == null)
            {
                return;
            }

            abilityDef.cooldownTicksRange = new IntRange(cooldownTicks, cooldownTicks);
            if (abilityDef.verbProperties != null)
            {
                abilityDef.verbProperties.warmupTime = warmup;
                abilityDef.verbProperties.range = range;
            }
        }

        private static void ApplyLegacyProjectileDamage()
        {
            ThingDef projectileDef = DefDatabase<ThingDef>.GetNamedSilentFail("Bullet_OpticBlast");
            ProjectileProperties projectileProps = projectileDef?.projectile;
            if (projectileProps == null)
            {
                return;
            }

            FieldInfo damageField = typeof(ProjectileProperties).GetField("damageAmountBase",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (damageField != null)
            {
                damageField.SetValue(projectileProps, Settings.damageAmount);
                return;
            }

            PropertyInfo damageProperty = typeof(ProjectileProperties).GetProperty("damageAmountBase",
                BindingFlags.Instance | BindingFlags.Public);
            if (damageProperty != null && damageProperty.CanWrite)
            {
                damageProperty.SetValue(projectileProps, Settings.damageAmount, null);
            }
        }

        private static void DrawIntSlider(Listing_Standard listing, string label, ref int value, int min, int max)
        {
            listing.Label(label);
            value = Mathf.RoundToInt(listing.Slider(value, min, max));
            listing.Gap(4f);
        }

        private static void DrawFloatSlider(Listing_Standard listing, string label, ref float value, float min, float max,
            float step)
        {
            listing.Label(label);
            value = Mathf.Round(listing.Slider(value, min, max) / step) * step;
            listing.Gap(4f);
        }

        private static string TicksToSeconds(int ticks)
        {
            return (ticks / 60f).ToString("0.#");
        }
    }
}
