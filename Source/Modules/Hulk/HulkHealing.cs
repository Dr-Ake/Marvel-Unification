using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Drake.Hulk
{
    public sealed class IncredibleHulkSettings : ModSettings
    {
        private static readonly string[] SeedExcludedHediffDefNames =
        {
            "MorningSickness"
        };

        public List<string> excludedHediffDefs = new List<string>();
        public bool enableGammaHealing = true;
        public bool healOutsideHulkForm;
        public bool cureDiseases = true;
        public bool removePermanentInjuries = true;
        public bool protectPregnancy = true;
        public bool regrowMissingParts = true;
        public bool autoResurrect = true;
        public bool enableGreenDoorReturn = true;
        public int greenDoorDelayHours = 6;
        public int greenDoorDoorOpenDelayHours = 1;
        public int greenDoorRecoveryComaHours = 2;
        public bool triggerRageOnHostileCarry = true;
        public bool enableForcedHulkRevert;
        public bool enableTimeRestrictedHulk;
        public int timeRestrictedHulkDurationSeconds = 90;
        public float moveSpeedMultiplier = 1f;
        public float carryCapacityMultiplier = 1f;
        public float meleeHitChanceMultiplier = 1f;
        public float meleeDamageMultiplier = 1f;
        public float meleeSpeedMultiplier = 1f;
        public float meleeDodgeMultiplier = 1f;
        public float toughnessMultiplier = 1f;
        public float regenerationMultiplier = 1f;
        public float clapRadiusMultiplier = 1f;
        public float clapPawnDamageMultiplier = 1f;
        public float clapStructureDamageMultiplier = 1f;
        public float leapRadiusMultiplier = 1f;
        public float leapPawnDamageMultiplier = 1f;
        public float leapStructureDamageMultiplier = 1f;
        public float boulderThrowRangeMultiplier = 1f;
        public float boulderImpactRadiusMultiplier = 1f;
        public float boulderImpactDamageMultiplier = 1f;
        public bool defaultExclusionsInitialized;

        private HashSet<string> excludedHediffDefsLookup;

        public IncredibleHulkSettings()
        {
            EnsureDefaultExcludedHediffs();
            EnsureValid();
        }

        public bool IsExcludedFromHealing(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }

            EnsureLookup();
            return excludedHediffDefsLookup.Contains(def.defName);
        }

        public void SetExcludedFromHealing(string defName, bool excluded)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return;
            }

            if (excludedHediffDefs == null)
            {
                excludedHediffDefs = new List<string>();
            }

            var changed = false;
            if (excluded)
            {
                if (!excludedHediffDefs.Contains(defName))
                {
                    excludedHediffDefs.Add(defName);
                    changed = true;
                }
            }
            else
            {
                changed = excludedHediffDefs.Remove(defName);
            }

            if (changed)
            {
                excludedHediffDefs.Sort(StringComparer.OrdinalIgnoreCase);
                excludedHediffDefsLookup = null;
            }
        }

        public void ClearExcludedHealingHediffs()
        {
            excludedHediffDefs?.Clear();
            excludedHediffDefsLookup = null;
        }

        public void ResetToDefaults()
        {
            enableGammaHealing = true;
            healOutsideHulkForm = false;
            cureDiseases = true;
            removePermanentInjuries = true;
            protectPregnancy = true;
            regrowMissingParts = true;
            autoResurrect = true;
            enableGreenDoorReturn = true;
            greenDoorDelayHours = 6;
            greenDoorDoorOpenDelayHours = 1;
            greenDoorRecoveryComaHours = 2;
            triggerRageOnHostileCarry = true;
            enableForcedHulkRevert = false;
            enableTimeRestrictedHulk = false;
            timeRestrictedHulkDurationSeconds = 90;
            moveSpeedMultiplier = 1f;
            carryCapacityMultiplier = 1f;
            meleeHitChanceMultiplier = 1f;
            meleeDamageMultiplier = 1f;
            meleeSpeedMultiplier = 1f;
            meleeDodgeMultiplier = 1f;
            toughnessMultiplier = 1f;
            regenerationMultiplier = 1f;
            clapRadiusMultiplier = 1f;
            clapPawnDamageMultiplier = 1f;
            clapStructureDamageMultiplier = 1f;
            leapRadiusMultiplier = 1f;
            leapPawnDamageMultiplier = 1f;
            leapStructureDamageMultiplier = 1f;
            boulderThrowRangeMultiplier = 1f;
            boulderImpactRadiusMultiplier = 1f;
            boulderImpactDamageMultiplier = 1f;
            excludedHediffDefs = BuildDefaultExcludedHediffDefNames();
            excludedHediffDefsLookup = null;
            defaultExclusionsInitialized = DefDatabase<HediffDef>.AllDefsListForReading?.Count > 0;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref excludedHediffDefs, "excludedHediffDefs", LookMode.Value);
            Scribe_Values.Look(ref enableGammaHealing, "enableGammaHealing", true);
            Scribe_Values.Look(ref healOutsideHulkForm, "healOutsideHulkForm", false);
            Scribe_Values.Look(ref cureDiseases, "cureDiseases", true);
            Scribe_Values.Look(ref removePermanentInjuries, "removePermanentInjuries", true);
            Scribe_Values.Look(ref protectPregnancy, "protectPregnancy", true);
            Scribe_Values.Look(ref regrowMissingParts, "regrowMissingParts", true);
            Scribe_Values.Look(ref autoResurrect, "autoResurrect", true);
            Scribe_Values.Look(ref enableGreenDoorReturn, "enableGreenDoorReturn", true);
            Scribe_Values.Look(ref greenDoorDelayHours, "greenDoorDelayHours", 6);
            Scribe_Values.Look(ref greenDoorDoorOpenDelayHours, "greenDoorDoorOpenDelayHours", 1);
            Scribe_Values.Look(ref greenDoorRecoveryComaHours, "greenDoorRecoveryComaHours", 2);
            Scribe_Values.Look(ref triggerRageOnHostileCarry, "triggerRageOnHostileCarry", true);
            Scribe_Values.Look(ref enableForcedHulkRevert, "enableForcedHulkRevert", false);
            Scribe_Values.Look(ref enableTimeRestrictedHulk, "enableTimeRestrictedHulk", false);
            Scribe_Values.Look(ref timeRestrictedHulkDurationSeconds, "timeRestrictedHulkDurationSeconds", 90);
            Scribe_Values.Look(ref moveSpeedMultiplier, "moveSpeedMultiplier", 1f);
            Scribe_Values.Look(ref carryCapacityMultiplier, "carryCapacityMultiplier", 1f);
            Scribe_Values.Look(ref meleeHitChanceMultiplier, "meleeHitChanceMultiplier", 1f);
            Scribe_Values.Look(ref meleeDamageMultiplier, "meleeDamageMultiplier", 1f);
            Scribe_Values.Look(ref meleeSpeedMultiplier, "meleeSpeedMultiplier", 1f);
            Scribe_Values.Look(ref meleeDodgeMultiplier, "meleeDodgeMultiplier", 1f);
            Scribe_Values.Look(ref toughnessMultiplier, "toughnessMultiplier", 1f);
            Scribe_Values.Look(ref regenerationMultiplier, "regenerationMultiplier", 1f);
            Scribe_Values.Look(ref clapRadiusMultiplier, "clapRadiusMultiplier", 1f);
            Scribe_Values.Look(ref clapPawnDamageMultiplier, "clapPawnDamageMultiplier", 1f);
            Scribe_Values.Look(ref clapStructureDamageMultiplier, "clapStructureDamageMultiplier", 1f);
            Scribe_Values.Look(ref leapRadiusMultiplier, "leapRadiusMultiplier", 1f);
            Scribe_Values.Look(ref leapPawnDamageMultiplier, "leapPawnDamageMultiplier", 1f);
            Scribe_Values.Look(ref leapStructureDamageMultiplier, "leapStructureDamageMultiplier", 1f);
            Scribe_Values.Look(ref boulderThrowRangeMultiplier, "boulderThrowRangeMultiplier", 1f);
            Scribe_Values.Look(ref boulderImpactRadiusMultiplier, "boulderImpactRadiusMultiplier", 1f);
            Scribe_Values.Look(ref boulderImpactDamageMultiplier, "boulderImpactDamageMultiplier", 1f);
            Scribe_Values.Look(ref defaultExclusionsInitialized, "defaultExclusionsInitialized", false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                EnsureDefaultExcludedHediffs();
                EnsureValid();
                excludedHediffDefsLookup = null;
            }
        }

        public void EnsureValid()
        {
            moveSpeedMultiplier = ClampRounded(moveSpeedMultiplier, 0.25f, 3f);
            carryCapacityMultiplier = ClampRounded(carryCapacityMultiplier, 0.25f, 3f);
            meleeHitChanceMultiplier = ClampRounded(meleeHitChanceMultiplier, 0.25f, 3f);
            meleeDamageMultiplier = ClampRounded(meleeDamageMultiplier, 0.25f, 3f);
            meleeSpeedMultiplier = ClampRounded(meleeSpeedMultiplier, 0.25f, 3f);
            meleeDodgeMultiplier = ClampRounded(meleeDodgeMultiplier, 0.25f, 3f);
            toughnessMultiplier = ClampRounded(toughnessMultiplier, 0.25f, 3f);
            regenerationMultiplier = ClampRounded(regenerationMultiplier, 0.25f, 3f);
            clapRadiusMultiplier = ClampRounded(clapRadiusMultiplier, 0.5f, 2f);
            clapPawnDamageMultiplier = ClampRounded(clapPawnDamageMultiplier, 0.25f, 3f);
            clapStructureDamageMultiplier = ClampRounded(clapStructureDamageMultiplier, 0.25f, 3f);
            leapRadiusMultiplier = ClampRounded(leapRadiusMultiplier, 0.5f, 2f);
            leapPawnDamageMultiplier = ClampRounded(leapPawnDamageMultiplier, 0.25f, 3f);
            leapStructureDamageMultiplier = ClampRounded(leapStructureDamageMultiplier, 0.25f, 3f);
            boulderThrowRangeMultiplier = ClampRounded(boulderThrowRangeMultiplier, 0.5f, 2f);
            boulderImpactRadiusMultiplier = ClampRounded(boulderImpactRadiusMultiplier, 0.5f, 2f);
            boulderImpactDamageMultiplier = ClampRounded(boulderImpactDamageMultiplier, 0.25f, 3f);
            greenDoorDelayHours = Mathf.Clamp(greenDoorDelayHours, 1, 168);
            greenDoorDoorOpenDelayHours = Mathf.Clamp(greenDoorDoorOpenDelayHours, 1, 24);
            greenDoorRecoveryComaHours = Mathf.Clamp(greenDoorRecoveryComaHours, 1, 24);
            timeRestrictedHulkDurationSeconds = Mathf.Clamp(timeRestrictedHulkDurationSeconds, 15, 300);
        }

        private void EnsureDefaultExcludedHediffs()
        {
            if (defaultExclusionsInitialized)
            {
                return;
            }

            if (excludedHediffDefs == null)
            {
                excludedHediffDefs = new List<string>();
            }

            foreach (var defName in BuildDefaultExcludedHediffDefNames())
            {
                if (!excludedHediffDefs.Contains(defName))
                {
                    excludedHediffDefs.Add(defName);
                }
            }

            excludedHediffDefs.Sort(StringComparer.OrdinalIgnoreCase);
            defaultExclusionsInitialized = DefDatabase<HediffDef>.AllDefsListForReading?.Count > 0;
        }

        private void EnsureLookup()
        {
            EnsureDefaultExcludedHediffs();
            EnsureValid();
            if (excludedHediffDefsLookup != null)
            {
                return;
            }

            if (excludedHediffDefs == null)
            {
                excludedHediffDefs = new List<string>();
            }

            excludedHediffDefs.RemoveAll(string.IsNullOrWhiteSpace);
            excludedHediffDefsLookup = new HashSet<string>(excludedHediffDefs, StringComparer.OrdinalIgnoreCase);
        }

        private List<string> BuildDefaultExcludedHediffDefNames()
        {
            var excluded = new HashSet<string>(SeedExcludedHediffDefNames, StringComparer.OrdinalIgnoreCase);
            var allHediffs = DefDatabase<HediffDef>.AllDefsListForReading;
            if (allHediffs != null)
            {
                foreach (var def in allHediffs)
                {
                    if (def == null || string.IsNullOrWhiteSpace(def.defName))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(HulkHealingUtility.GetDefaultExclusionReasonForDef(def, this)))
                    {
                        excluded.Add(def.defName);
                    }
                }
            }

            return excluded
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static float ClampRounded(float value, float min, float max)
        {
            return Mathf.Round(Mathf.Clamp(value, min, max) * 100f) / 100f;
        }
    }

    public sealed class IncredibleHulkMod : Mod
    {
        public static IncredibleHulkSettings Settings;

        private Vector2 scrollPosition = Vector2.zero;
        private float scrollHeight;

        public IncredibleHulkMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<IncredibleHulkSettings>();
            Settings.EnsureValid();
        }

        public override string SettingsCategory()
        {
            return "HulkSettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (scrollHeight <= 0f)
            {
                scrollHeight = inRect.height;
            }

            var contentWidth = inRect.width - 18f;
            var viewRect = new Rect(0f, 0f, contentWidth, scrollHeight);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect, true);

            var list = new Listing_Standard();
            var listingRect = new Rect(0f, 0f, contentWidth, 99999f);
            list.Begin(listingRect);
            var changed = false;

            DrawSectionHeader(list, "HulkSettingsSectionHealing".Translate());
            list.CheckboxLabeled("HulkSettingsEnableGammaHealing".Translate(), ref Settings.enableGammaHealing);
            list.CheckboxLabeled("HulkSettingsHealOutsideHulkForm".Translate(), ref Settings.healOutsideHulkForm);
            list.CheckboxLabeled("HulkSettingsCureDiseases".Translate(), ref Settings.cureDiseases);
            list.CheckboxLabeled("HulkSettingsRemovePermanentInjuries".Translate(), ref Settings.removePermanentInjuries);
            list.CheckboxLabeled("HulkSettingsRegrowMissingParts".Translate(), ref Settings.regrowMissingParts);
            list.CheckboxLabeled("HulkSettingsProtectPregnancy".Translate(), ref Settings.protectPregnancy);
            list.CheckboxLabeled("HulkSettingsAutoResurrect".Translate(), ref Settings.autoResurrect);
            if (Settings.autoResurrect)
            {
                list.CheckboxLabeled("HulkSettingsEnableGreenDoorReturn".Translate(), ref Settings.enableGreenDoorReturn);
                if (Settings.enableGreenDoorReturn)
                {
                    changed |= DrawIntSlider(list, "HulkSettingsGreenDoorDelayHours".Translate(), ref Settings.greenDoorDelayHours, 1, 168, hours => "HulkSettingsHoursFormat".Translate(hours).ToString());
                    changed |= DrawIntSlider(list, "HulkSettingsGreenDoorOpenDelayHours".Translate(), ref Settings.greenDoorDoorOpenDelayHours, 1, 24, hours => "HulkSettingsHoursFormat".Translate(hours).ToString());
                    changed |= DrawIntSlider(list, "HulkSettingsGreenDoorRecoveryComaHours".Translate(), ref Settings.greenDoorRecoveryComaHours, 1, 24, hours => "HulkSettingsHoursFormat".Translate(hours).ToString());
                    list.Label("HulkSettingsGreenDoorSummary".Translate());
                }
            }
            list.CheckboxLabeled("HulkSettingsTriggerHostileCarryRage".Translate(), ref Settings.triggerRageOnHostileCarry);

            if (Widgets.ButtonText(list.GetRect(30f), "HulkSettingsConfigureHealingFilter".Translate()))
            {
                Find.WindowStack.Add(new Dialog_HulkHealingFilter(Settings));
            }

            list.Label("HulkSettingsExcludedHediffs".Translate(Settings.excludedHediffDefs?.Count ?? 0));

            list.GapLine();
            DrawSectionHeader(list, "HulkSettingsSectionAppearance".Translate());
            DrawHulkAppearanceSection(list);

            if (Widgets.ButtonText(list.GetRect(30f), "HulkSettingsResetDefaults".Translate()))
            {
                Settings.ResetToDefaults();
                changed = true;
            }

            list.GapLine();
            DrawSectionHeader(list, "HulkSettingsSectionCombat".Translate());
            changed |= DrawMultiplierSlider(list, "HulkSettingsMoveSpeedMultiplier".Translate(), ref Settings.moveSpeedMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsCarryCapacityMultiplier".Translate(), ref Settings.carryCapacityMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsMeleeHitChanceMultiplier".Translate(), ref Settings.meleeHitChanceMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsMeleeDamageMultiplier".Translate(), ref Settings.meleeDamageMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsMeleeSpeedMultiplier".Translate(), ref Settings.meleeSpeedMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsMeleeDodgeMultiplier".Translate(), ref Settings.meleeDodgeMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsToughnessMultiplier".Translate(), ref Settings.toughnessMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsRegenerationMultiplier".Translate(), ref Settings.regenerationMultiplier, 0.25f, 3f);

            list.GapLine();
            DrawSectionHeader(list, "HulkSettingsSectionAbilities".Translate());
            changed |= DrawMultiplierSlider(list, "HulkSettingsClapRadiusMultiplier".Translate(), ref Settings.clapRadiusMultiplier, 0.5f, 2f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsClapPawnDamageMultiplier".Translate(), ref Settings.clapPawnDamageMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsClapStructureDamageMultiplier".Translate(), ref Settings.clapStructureDamageMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsLeapRadiusMultiplier".Translate(), ref Settings.leapRadiusMultiplier, 0.5f, 2f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsLeapPawnDamageMultiplier".Translate(), ref Settings.leapPawnDamageMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsLeapStructureDamageMultiplier".Translate(), ref Settings.leapStructureDamageMultiplier, 0.25f, 3f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsBoulderThrowRangeMultiplier".Translate(), ref Settings.boulderThrowRangeMultiplier, 0.5f, 2f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsBoulderImpactRadiusMultiplier".Translate(), ref Settings.boulderImpactRadiusMultiplier, 0.5f, 2f);
            changed |= DrawMultiplierSlider(list, "HulkSettingsBoulderImpactDamageMultiplier".Translate(), ref Settings.boulderImpactDamageMultiplier, 0.25f, 3f);

            list.GapLine();
            DrawSectionHeader(list, "HulkSettingsSectionTimers".Translate());
            list.CheckboxLabeled("HulkSettingsEnableForcedHulkRevert".Translate(), ref Settings.enableForcedHulkRevert);
            list.CheckboxLabeled("HulkSettingsEnableTimeRestrictedHulk".Translate(), ref Settings.enableTimeRestrictedHulk);
            if (Settings.enableTimeRestrictedHulk)
            {
                changed |= DrawIntSlider(list, "HulkSettingsTimeRestrictedHulkDuration".Translate(), ref Settings.timeRestrictedHulkDurationSeconds, 15, 300, seconds => "HulkSettingsSecondsFormat".Translate(seconds).ToString());
            }
            list.Label("HulkSettingsTimerSummary".Translate());

            list.GapLine();
            list.Label("HulkSettingsSummary".Translate());

            var contentHeight = list.CurHeight;
            list.End();
            Widgets.EndScrollView();

            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
            {
                scrollHeight = Mathf.Max(contentHeight + 24f, inRect.height + 1f);
            }

            if (GUI.changed || changed)
            {
                Settings.EnsureValid();
                HulkRuntimeTuning.ApplyCurrentSettings();
                Settings.Write();
            }
        }

        private static void DrawSectionHeader(Listing_Standard list, string label)
        {
            list.Label(label);
            list.Gap(3f);
        }

        private static void DrawHulkAppearanceSection(Listing_Standard list)
        {
            var hasActiveGame = Current.ProgramState == ProgramState.Playing;
            var eligiblePawns = hasActiveGame
                ? HulkUtility.GetEligibleColorCustomizationPawns()
                    .OrderBy(GetPawnDisplayLabel, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : new List<Pawn>();
            var customColorCount = eligiblePawns.Count(HulkUtility.HasCustomHulkSkinColor);
            var oldEnabled = GUI.enabled;

            GUI.enabled = eligiblePawns.Count > 0;
            if (Widgets.ButtonText(list.GetRect(30f), "HulkSettingsChooseHulkColor".Translate()))
            {
                Find.WindowStack.Add(new Dialog_HulkColorSelector());
            }

            GUI.enabled = eligiblePawns.Count > 0 && customColorCount > 0;
            if (Widgets.ButtonText(list.GetRect(30f), "HulkSettingsClearAllHulkColors".Translate()))
            {
                HulkUtility.ClearAllCustomHulkSkinColors();
            }

            GUI.enabled = oldEnabled;

            if (!hasActiveGame)
            {
                list.Label("HulkSettingsNoActiveGame".Translate());
                return;
            }

            if (eligiblePawns.Count == 0)
            {
                list.Label("HulkSettingsNoEligibleHulkColorPawns".Translate());
                return;
            }

            list.Label("HulkSettingsHulkColorSummary".Translate(eligiblePawns.Count, customColorCount));
        }

        private static bool DrawMultiplierSlider(Listing_Standard list, string label, ref float value, float min, float max)
        {
            var previous = value;
            list.Label($"{label}: {value:0.00}x");
            value = Mathf.Round(list.Slider(value, min, max) * 100f) / 100f;
            return Math.Abs(previous - value) > 0.0001f;
        }

        private static bool DrawIntSlider(Listing_Standard list, string label, ref int value, int min, int max, Func<int, string> formatter)
        {
            var previous = value;
            list.Label($"{label}: {formatter(value)}");
            value = Mathf.RoundToInt(list.Slider(value, min, max));
            return previous != value;
        }

        private static string GetPawnDisplayLabel(Pawn pawn)
        {
            return pawn?.Name?.ToStringShort ?? pawn?.LabelShortCap.ToString() ?? string.Empty;
        }
    }

    internal sealed class Dialog_HulkHealingFilter : Window
    {
        private readonly IncredibleHulkSettings settings;
        private readonly List<HediffDef> allHediffs;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = string.Empty;

        public override Vector2 InitialSize => new Vector2(980f, 720f);

        public Dialog_HulkHealingFilter(IncredibleHulkSettings settings)
        {
            this.settings = settings;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            allHediffs = DefDatabase<HediffDef>.AllDefsListForReading
                .Where(def => def != null && !string.IsNullOrWhiteSpace(def.defName))
                .OrderBy(GetModLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(GetDisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.defName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var searchLabelRect = new Rect(inRect.x, inRect.y, 90f, 24f);
            Widgets.Label(searchLabelRect, "HulkHealingFilterSearch".Translate());

            var searchRect = new Rect(searchLabelRect.xMax + 8f, inRect.y, inRect.width - 98f, 24f);
            searchText = Widgets.TextField(searchRect, searchText ?? string.Empty);

            var infoRect = new Rect(inRect.x, searchRect.yMax + 8f, inRect.width, 22f);
            Widgets.Label(infoRect, "HulkHealingFilterInfo".Translate());

            var buttonsRect = new Rect(inRect.x, infoRect.yMax + 8f, inRect.width, 30f);
            const float buttonWidth = 160f;
            if (Widgets.ButtonText(new Rect(buttonsRect.x, buttonsRect.y, buttonWidth, buttonsRect.height), "HulkHealingFilterAllowVisible".Translate()))
            {
                SetVisibleExcluded(false);
            }

            if (Widgets.ButtonText(new Rect(buttonsRect.x + buttonWidth + 10f, buttonsRect.y, buttonWidth, buttonsRect.height), "HulkHealingFilterExcludeVisible".Translate()))
            {
                SetVisibleExcluded(true);
            }

            if (Widgets.ButtonText(new Rect(buttonsRect.x + ((buttonWidth + 10f) * 2f), buttonsRect.y, buttonWidth, buttonsRect.height), "HulkHealingFilterResetAll".Translate()))
            {
                settings.ResetToDefaults();
                settings.Write();
            }

            var visibleHediffs = FilteredHediffs();
            var listRect = new Rect(inRect.x, buttonsRect.yMax + 10f, inRect.width, inRect.height - (buttonsRect.yMax - inRect.y) - 46f);
            const float rowHeight = 26f;
            var viewHeight = Mathf.Max(visibleHediffs.Count * rowHeight, listRect.height - 4f);
            var viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            var y = 0f;
            foreach (var def in visibleHediffs)
            {
                var row = new Rect(0f, y, viewRect.width, rowHeight);
                var defaultExclusionReason = HulkHealingUtility.GetAutomaticSkipReasonForDef(def);
                var allowHealing = !settings.IsExcludedFromHealing(def);
                var newAllowHealing = allowHealing;
                var label = $"{GetDisplayLabel(def)} ({def.defName}) [{GetModLabel(def)}]";
                if (!string.IsNullOrWhiteSpace(defaultExclusionReason))
                {
                    label = "HulkHealingFilterDefaultRow".Translate(label, defaultExclusionReason);
                }

                Widgets.CheckboxLabeled(row, label, ref newAllowHealing);
                if (newAllowHealing != allowHealing)
                {
                    settings.SetExcludedFromHealing(def.defName, !newAllowHealing);
                    settings.Write();
                }

                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private List<HediffDef> FilteredHediffs()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return allHediffs;
            }

            return allHediffs.Where(def =>
                    (def.defName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (def.label?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    GetModLabel(def).IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void SetVisibleExcluded(bool excluded)
        {
            foreach (var def in FilteredHediffs())
            {
                settings.SetExcludedFromHealing(def.defName, excluded);
            }

            settings.Write();
        }

        private static string GetDisplayLabel(HediffDef def)
        {
            return string.IsNullOrWhiteSpace(def?.label) ? def?.defName ?? "HulkHealingFilterInvalid".Translate().ToString() : def.label.CapitalizeFirst();
        }

        private static string GetModLabel(Def def)
        {
            return def?.modContentPack?.Name ?? "HulkHealingFilterCore".Translate().ToString();
        }
    }

    internal sealed class Dialog_HulkColorSelector : Window
    {
        private static readonly List<Color> PresetColors = new List<Color>
        {
            HulkUtility.HulkSkinColor,
            new Color(0.80f, 0.14f, 0.16f, 1f),
            new Color(0.22f, 0.44f, 0.95f, 1f),
            new Color(0.96f, 0.48f, 0.78f, 1f),
            new Color(0.78f, 0.72f, 0.22f, 1f),
            new Color(0.64f, 0.41f, 0.20f, 1f),
            new Color(0.56f, 0.27f, 0.80f, 1f),
            new Color(0.72f, 0.72f, 0.72f, 1f)
        };

        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(760f, 520f);

        public Dialog_HulkColorSelector()
        {
            doCloseX = false;
            doCloseButton = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Widgets.Label(titleRect, "HulkColorSelectorTitle".Translate());

            var infoRect = new Rect(inRect.x, titleRect.yMax + 6f, inRect.width, 24f);
            Widgets.Label(infoRect, "HulkColorSelectorInfo".Translate());

            var pawns = HulkUtility.GetEligibleColorCustomizationPawns()
                .OrderBy(GetPawnDisplayLabel, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var listRect = new Rect(inRect.x, infoRect.yMax + 10f, inRect.width, inRect.height - (infoRect.yMax - inRect.y) - 46f);
            if (pawns.Count == 0)
            {
                Widgets.Label(listRect, "HulkSettingsNoEligibleHulkColorPawns".Translate());
                return;
            }

            const float rowHeight = 42f;
            const float rowGap = 4f;
            var viewHeight = Mathf.Max(pawns.Count * (rowHeight + rowGap), listRect.height - 4f);
            var viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            var y = 0f;
            foreach (var pawn in pawns)
            {
                var row = new Rect(0f, y, viewRect.width, rowHeight);
                DrawPawnRow(row, pawn);
                y += rowHeight + rowGap;
            }

            Widgets.EndScrollView();
        }

        private static void DrawPawnRow(Rect row, Pawn pawn)
        {
            var resolvedColor = HulkUtility.GetResolvedHulkSkinColor(pawn);
            var hasCustomColor = HulkUtility.HasCustomHulkSkinColor(pawn);
            Widgets.DrawHighlightIfMouseover(row);

            var swatchRect = new Rect(row.x, row.y + 7f, 28f, 28f);
            Widgets.DrawBoxSolid(swatchRect.ContractedBy(1f), resolvedColor);
            Widgets.DrawBox(swatchRect);

            var labelRect = new Rect(swatchRect.xMax + 10f, row.y + 2f, Mathf.Max(0f, row.width - 230f), 20f);
            Widgets.Label(labelRect, GetPawnDisplayLabel(pawn));

            var statusRect = new Rect(labelRect.x, labelRect.yMax, labelRect.width, 18f);
            Widgets.Label(statusRect, hasCustomColor ? "HulkColorStatusCustom".Translate() : "HulkColorStatusDefault".Translate());

            const float buttonWidth = 90f;
            const float buttonGap = 6f;
            var defaultRect = new Rect(row.xMax - ((buttonWidth * 2f) + buttonGap), row.y + 6f, buttonWidth, 30f);
            var chooseRect = new Rect(row.xMax - buttonWidth, row.y + 6f, buttonWidth, 30f);

            var oldEnabled = GUI.enabled;
            GUI.enabled = hasCustomColor;
            if (Widgets.ButtonText(defaultRect, "HulkColorDefault".Translate()))
            {
                HulkUtility.ClearCustomHulkSkinColor(pawn);
            }

            GUI.enabled = oldEnabled;
            if (Widgets.ButtonText(chooseRect, "HulkColorChoose".Translate()))
            {
                Find.WindowStack.Add(new Dialog_HulkColorPresetPicker(pawn, PresetColors));
            }
        }

        private static string GetPawnDisplayLabel(Pawn pawn)
        {
            return pawn?.Name?.ToStringShort ?? pawn?.LabelShortCap.ToString() ?? string.Empty;
        }
    }

    internal sealed class Dialog_HulkColorPresetPicker : Window
    {
        private readonly Pawn pawn;
        private readonly List<Color> presetColors;

        public override Vector2 InitialSize => new Vector2(760f, 210f);

        public Dialog_HulkColorPresetPicker(Pawn pawn, List<Color> presetColors)
        {
            this.pawn = pawn;
            this.presetColors = presetColors ?? new List<Color>();
            doCloseX = false;
            doCloseButton = false;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 28f);
            Widgets.Label(titleRect, "HulkColorDialogTitle".Translate(GetPawnDisplayLabel(pawn)));

            var infoRect = new Rect(inRect.x, titleRect.yMax + 6f, inRect.width, 24f);
            Widgets.Label(infoRect, "HulkColorPresetInfo".Translate());

            var currentColor = HulkUtility.GetResolvedHulkSkinColor(pawn);
            var currentStatusRect = new Rect(inRect.x, infoRect.yMax + 6f, inRect.width, 22f);
            Widgets.Label(
                currentStatusRect,
                HulkUtility.HasCustomHulkSkinColor(pawn)
                    ? "HulkColorCurrentStatusCustom".Translate()
                    : "HulkColorCurrentStatusDefault".Translate());

            var swatchTop = currentStatusRect.yMax + 18f;
            const float swatchSize = 48f;
            const float swatchGap = 10f;
            var x = inRect.x;
            var selectedPresetIndex = GetSelectedPresetIndex(currentColor);

            for (var i = 0; i < presetColors.Count; i++)
            {
                var swatchRect = new Rect(x, swatchTop, swatchSize, swatchSize);
                DrawPresetButton(swatchRect, i, selectedPresetIndex == i);
                x += swatchSize + swatchGap;
            }
        }

        private void DrawPresetButton(Rect rect, int index, bool selected)
        {
            var oldColor = GUI.color;
            if (selected)
            {
                GUI.color = Color.white;
                Widgets.DrawBoxSolid(rect.ExpandedBy(2f), new Color(0.94f, 0.94f, 0.94f, 1f));
            }

            Widgets.DrawBoxSolid(rect.ContractedBy(2f), presetColors[index]);
            Widgets.DrawBox(rect);
            GUI.color = oldColor;

            if (Widgets.ButtonInvisible(rect))
            {
                if (index == 0)
                {
                    HulkUtility.ClearCustomHulkSkinColor(pawn);
                }
                else
                {
                    HulkUtility.SetCustomHulkSkinColor(pawn, presetColors[index]);
                }

                Close();
            }
        }

        private int GetSelectedPresetIndex(Color currentColor)
        {
            for (var i = 0; i < presetColors.Count; i++)
            {
                if (ColorsClose(presetColors[i], currentColor))
                {
                    if (i == 0 && HulkUtility.HasCustomHulkSkinColor(pawn))
                    {
                        return -1;
                    }

                    return i;
                }
            }

            return -1;
        }

        private static bool ColorsClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f &&
                   Mathf.Abs(a.g - b.g) < 0.02f &&
                   Mathf.Abs(a.b - b.b) < 0.02f &&
                   Mathf.Abs(a.a - b.a) < 0.02f;
        }

        private static string GetPawnDisplayLabel(Pawn pawn)
        {
            return pawn?.Name?.ToStringShort ?? pawn?.LabelShortCap.ToString() ?? string.Empty;
        }
    }

    public sealed class HediffCompProperties_HulkRegrowing : HediffCompProperties
    {
        public HediffCompProperties_HulkRegrowing()
        {
            compClass = typeof(HediffComp_HulkRegrowing);
        }
    }

    public sealed class HediffComp_HulkRegrowing : HediffComp
    {
        private bool canContinueOutsideHulkForm;
        private bool continuationRuleInitialized;

        public bool CanContinueOutsideHulkForm => continuationRuleInitialized ? canContinueOutsideHulkForm : true;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref canContinueOutsideHulkForm, "canContinueOutsideHulkForm", false);
            Scribe_Values.Look(ref continuationRuleInitialized, "continuationRuleInitialized", false);
        }

        public void MarkStartContext(bool startedInHulkForm)
        {
            canContinueOutsideHulkForm = startedInHulkForm;
            continuationRuleInitialized = true;
        }
    }

    internal static class HulkHealingUtility
    {
        private const int MissingPartRegrowthIntervalTicks = 250;
        private const float HulkRegrowthSpeedPerInterval = 0.01f;
        private const int MaxConcurrentRegrowingParts = 2;
        private static readonly IncredibleHulkSettings FallbackSettings = new IncredibleHulkSettings();

        public static IncredibleHulkSettings Settings => IncredibleHulkMod.Settings ?? FallbackSettings;

        public static string GetAutomaticSkipReasonForDef(HediffDef def)
        {
            return GetDefaultExclusionReasonForDef(def, Settings);
        }

        public static string GetDefaultExclusionReasonForDef(HediffDef def, IncredibleHulkSettings settings)
        {
            if (def == null)
            {
                return "HulkHealingSkipInvalid".Translate();
            }

            if (def == HulkDefOf.Drake_HulkIdentity ||
                def == HulkDefOf.Drake_HulkForm ||
                def == HulkDefOf.Drake_HulkRecoveryComa ||
                def == HulkDefOf.Drake_HulkRegrowing ||
                def == HulkDefOf.Drake_HulkAdjusting)
            {
                return "HulkHealingSkipInternal".Translate();
            }

            var hediffClass = def.hediffClass;
            if (hediffClass != null)
            {
                if (typeof(Hediff_MissingPart).IsAssignableFrom(hediffClass))
                {
                    return null;
                }

                if (typeof(Hediff_AddedPart).IsAssignableFrom(hediffClass))
                {
                    return "HulkHealingSkipAddedParts".Translate();
                }

                if ((settings?.protectPregnancy ?? true) && IsPregnancyLike(def))
                {
                    return "HulkHealingSkipPregnancy".Translate();
                }

                if (typeof(Hediff_Injury).IsAssignableFrom(hediffClass))
                {
                    return null;
                }
            }

            if (!def.isBad)
            {
                return "HulkHealingSkipNotHarmful".Translate();
            }

            if (!ShouldCureDefWithGammaHealing(def))
            {
                return "HulkHealingSkipNotHealable".Translate();
            }

            return null;
        }

        public static void ApplyRegenerationPulse(Pawn pawn, float healAmount)
        {
            if (pawn?.health?.hediffSet == null || !Settings.enableGammaHealing)
            {
                return;
            }

            var hediffs = pawn.health.hediffSet.hediffs.ToList();
            var toRemove = new List<Hediff>();

            foreach (var hediff in hediffs)
            {
                if (hediff == null || ShouldSkipForHealing(hediff))
                {
                    continue;
                }

                if (IsGammaSupportHediff(hediff.def))
                {
                    continue;
                }

                if (hediff is Hediff_MissingPart)
                {
                    continue;
                }

                if (hediff is Hediff_AddedPart addedPart)
                {
                    RestoreOrganicPart(pawn, addedPart.Part, addAdjustment: true);
                    continue;
                }

                if (IsPregnancyLike(hediff.def))
                {
                    toRemove.Add(hediff);
                    continue;
                }

                if (hediff is Hediff_Injury injury)
                {
                    if (injury.IsPermanent())
                    {
                        if (Settings.removePermanentInjuries)
                        {
                            toRemove.Add(injury);
                        }

                        continue;
                    }

                    if (injury.Severity > 0f && injury.CanHealNaturally())
                    {
                        injury.Heal(healAmount);
                    }

                    continue;
                }

                if (!Settings.cureDiseases || !ShouldCureWithGammaHealing(hediff))
                {
                    continue;
                }

                if (hediff.Severity > 0f)
                {
                    hediff.Severity = Mathf.Max(0f, hediff.Severity - healAmount);
                }

                toRemove.Add(hediff);
            }

            foreach (var hediff in toRemove.Distinct().Where(entry => pawn.health.hediffSet.hediffs.Contains(entry)).ToList())
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void ApplyEmergencyRecovery(Pawn pawn, float healAmount = 12f, int pulses = 12, int regrowthAttempts = 2)
        {
            if (pawn == null)
            {
                return;
            }

            ClearActiveRegrowthState(pawn);

            for (var i = 0; i < Math.Max(1, pulses); i++)
            {
                ApplyRegenerationPulse(pawn, healAmount);
            }

            if (!Settings.regrowMissingParts)
            {
                return;
            }

            for (var i = 0; i < Math.Max(0, regrowthAttempts); i++)
            {
                if (!TryRegrowMissingPart(pawn))
                {
                    break;
                }
            }
        }

        public static void TickMissingPartRegrowth(Pawn pawn, float rateMultiplier, bool allowNewStarts)
        {
            if (!Settings.regrowMissingParts || pawn?.health?.hediffSet == null || !HulkUtility.HasGammaIdentity(pawn))
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(MissingPartRegrowthIntervalTicks))
            {
                return;
            }

            var regrowingDef = HulkDefOf.Drake_HulkRegrowing;
            var regrowingHediffs = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == regrowingDef)
                .ToList();

            foreach (var regrow in regrowingHediffs.ToList())
            {
                if (!CanProgressRegrowth(pawn, regrow))
                {
                    continue;
                }

                regrow.Severity += HulkRegrowthSpeedPerInterval * Mathf.Max(0.25f, rateMultiplier) * Settings.regenerationMultiplier;
                if (regrow.Severity < 1f)
                {
                    continue;
                }

                var partToRestore = pawn.health.hediffSet.GetMissingPartsCommonAncestors()
                    .Select(missing => missing.Part)
                    .FirstOrDefault(part => part?.parent == regrow.Part) ?? regrow.Part;
                pawn.health.RemoveHediff(regrow);
                RestoreOrganicPart(pawn, partToRestore, addAdjustment: true);
            }

            if (!allowNewStarts)
            {
                return;
            }

            var regrowingCount = pawn.health.hediffSet.hediffs.Count(hediff => hediff?.def == regrowingDef);
            if (regrowingCount >= MaxConcurrentRegrowingParts)
            {
                return;
            }

            var startedInHulkForm = HulkUtility.IsTransformed(pawn);
            foreach (var missing in pawn.health.hediffSet.GetMissingPartsCommonAncestors().ToList())
            {
                if (missing?.Part == null || Settings.IsExcludedFromHealing(missing.def))
                {
                    continue;
                }

                var anchorPart = missing.Part.parent ?? missing.Part;
                if (anchorPart == null)
                {
                    continue;
                }

                if (pawn.health.hediffSet.hediffs.Any(hediff => hediff?.def == regrowingDef && hediff.Part == anchorPart))
                {
                    continue;
                }

                var regrow = pawn.health.AddHediff(regrowingDef, anchorPart);
                regrow.TryGetComp<HediffComp_HulkRegrowing>()?.MarkStartContext(startedInHulkForm);
                regrow.Severity = Mathf.Max(regrow.Severity, 0.01f);
                regrowingCount++;
                if (regrowingCount >= MaxConcurrentRegrowingParts)
                {
                    break;
                }
            }
        }

        public static bool TryRegrowMissingPart(Pawn pawn)
        {
            if (!Settings.regrowMissingParts || pawn?.health?.hediffSet == null)
            {
                return false;
            }

            var missingPart = GetNextRegrowableMissingPart(pawn)?.Part;

            if (missingPart == null)
            {
                return false;
            }

            return RestoreOrganicPart(pawn, missingPart, addAdjustment: false);
        }

        public static bool ShouldSkipForHealing(Hediff hediff)
        {
            if (hediff?.def == null)
            {
                return true;
            }

            if (Settings.IsExcludedFromHealing(hediff.def))
            {
                return true;
            }

            return false;
        }

        public static bool ShouldCureWithGammaHealing(Hediff hediff)
        {
            if (hediff?.def == null)
            {
                return false;
            }

            if (ShouldSkipForHealing(hediff))
            {
                return false;
            }

            if (ShouldCureDefWithGammaHealing(hediff.def))
            {
                return true;
            }

            return hediff.TryGetComp<HediffComp_Immunizable>() != null;
        }

        private static Hediff_MissingPart GetNextRegrowableMissingPart(Pawn pawn)
        {
            return pawn?.health?.hediffSet?.GetMissingPartsCommonAncestors()
                .Where(missing => missing?.Part != null)
                .Where(missing => !Settings.IsExcludedFromHealing(missing.def))
                .OrderBy(missing => missing.Part.coverageAbsWithChildren)
                .FirstOrDefault();
        }

        private static bool ShouldCureDefWithGammaHealing(HediffDef def)
        {
            if (def == null || !def.isBad)
            {
                return false;
            }

            if (def.isInfection ||
                def.chronic ||
                def.tendable ||
                def.makesSickThought ||
                def.everCurableByItem ||
                def.lethalSeverity > 0f ||
                def.removeOnRedressChanceByDaysCurve != null ||
                def.chemicalNeed != null)
            {
                return true;
            }

            return def.comps != null &&
                   def.comps.Any(comp => comp?.compClass != null && typeof(HediffComp_Immunizable).IsAssignableFrom(comp.compClass));
        }

        private static bool IsPregnancyLike(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.pregnant)
            {
                return true;
            }

            if (ContainsIgnoreCase(def.defName, "Pregnan") ||
                ContainsIgnoreCase(def.defName, "MorningSickness") ||
                ContainsIgnoreCase(def.label, "pregnan"))
            {
                return true;
            }

            return ContainsIgnoreCase(def.hediffClass?.Name, "Pregnan");
        }

        private static bool IsMissingPartDef(HediffDef def)
        {
            return def?.hediffClass != null && typeof(Hediff_MissingPart).IsAssignableFrom(def.hediffClass);
        }

        private static bool IsGammaSupportHediff(HediffDef def)
        {
            return def == HulkDefOf.Drake_HulkIdentity ||
                   def == HulkDefOf.Drake_HulkForm ||
                   def == HulkDefOf.Drake_HulkRecoveryComa ||
                   def == HulkDefOf.Drake_HulkRegrowing ||
                   def == HulkDefOf.Drake_HulkAdjusting;
        }

        private static bool CanProgressRegrowth(Pawn pawn, Hediff regrow)
        {
            if (pawn == null || regrow == null)
            {
                return false;
            }

            if (HulkUtility.IsTransformed(pawn) || Settings.healOutsideHulkForm)
            {
                return true;
            }

            return regrow.TryGetComp<HediffComp_HulkRegrowing>()?.CanContinueOutsideHulkForm ?? true;
        }

        private static bool RestoreOrganicPart(Pawn pawn, BodyPartRecord part, bool addAdjustment)
        {
            if (pawn?.health == null || part == null)
            {
                return false;
            }

            try
            {
                pawn.health.RestorePart(part);
            }
            catch
            {
                return false;
            }

            RemoveRelatedRegrowthHediffs(pawn, part);
            if (addAdjustment)
            {
                pawn.health.AddHediff(HulkDefOf.Drake_HulkAdjusting, part);
            }

            return true;
        }

        private static void RemoveRelatedRegrowthHediffs(Pawn pawn, BodyPartRecord restoredPart)
        {
            if (pawn?.health?.hediffSet == null || restoredPart == null)
            {
                return;
            }

            var hediffs = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == HulkDefOf.Drake_HulkRegrowing &&
                                 (hediff.Part == restoredPart || hediff.Part == restoredPart.parent))
                .ToList();
            foreach (var hediff in hediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static void ClearActiveRegrowthState(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            var hediffs = pawn.health.hediffSet.hediffs
                .Where(hediff => hediff?.def == HulkDefOf.Drake_HulkRegrowing || hediff?.def == HulkDefOf.Drake_HulkAdjusting)
                .ToList();
            foreach (var hediff in hediffs)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static bool ContainsIgnoreCase(string source, string fragment)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
