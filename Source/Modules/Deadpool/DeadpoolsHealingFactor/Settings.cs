using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace DeadpoolsHealingFactor
{
    public class DeadpoolsHealingFactorSettings : ModSettings
    {
        private static readonly string[] DefaultAdamantiumProtectedBodyPartDefNames = new[]
        {
            "Head",
            "Skull",
            "Jaw",
            "Neck",
            "Torso",
            "Shoulder",
            "Arm",
            "Hand",
            "Finger",
            "Clavicle",
            "Sternum",
            "Humerus",
            "Radius",
            "Ribcage",
            "Spine",
            "Pelvis",
            "Leg",
            "Foot",
            "Toe",
            "Femur",
            "Tibia"
        };

        public List<string> excludedHediffDefs = new List<string>();
        public List<string> adamantiumProtectedBodyPartDefs = new List<string>();
        public bool enableHealing = true;
        public bool enableRegrowth = true;
        public bool boostMood = true;
        public bool forcePsychopath = true;
        public bool autoResurrection = true;
        public bool cureDiseases = true;
        public bool removeScars = true;
        public bool preventHealingFactorKidnap = true;
        public bool enableClawSound = true;
        public float baseHealAmount = 0.5f;
        public int ticksBetweenHeals = 250;
        public float regrowSpeed = 0.01f;
        public int maxRegrowingParts = 2;
        public int infusionDurationTicks = 30000;
        public int chamberFuelRequired = 500;
        public float clawDamage = 18f;
        public float clawArmorPen = 0.35f;
        public float clawCooldown = 1.5f;
        public bool preferRimForgeAdamantium = true;
        public bool adamantiumProtectedBodyPartDefsInitialized;
        private HashSet<string> excludedHediffDefsLookup;
        private HashSet<string> adamantiumProtectedBodyPartDefsLookup;

        public bool IsExcludedFromHealing(HediffDef def)
        {
            if (def == null)
            {
                return false;
            }
            EnsureExcludedLookup();
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

            bool changed = false;
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

        public bool IsAdamantiumProtectedBodyPart(BodyPartDef def)
        {
            if (def == null)
            {
                return false;
            }
            EnsureAdamantiumProtectedBodyPartLookup();
            return adamantiumProtectedBodyPartDefsLookup.Contains(def.defName);
        }

        public void SetAdamantiumProtectedBodyPart(string defName, bool protect)
        {
            if (string.IsNullOrWhiteSpace(defName))
            {
                return;
            }

            if (adamantiumProtectedBodyPartDefs == null)
            {
                adamantiumProtectedBodyPartDefs = new List<string>();
            }

            bool changed = false;
            if (protect)
            {
                if (!adamantiumProtectedBodyPartDefs.Contains(defName))
                {
                    adamantiumProtectedBodyPartDefs.Add(defName);
                    changed = true;
                }
            }
            else
            {
                changed = adamantiumProtectedBodyPartDefs.Remove(defName);
            }

            if (changed)
            {
                adamantiumProtectedBodyPartDefs.Sort(StringComparer.OrdinalIgnoreCase);
                adamantiumProtectedBodyPartDefsLookup = null;
                adamantiumProtectedBodyPartDefsInitialized = true;
            }
        }

        public void ResetAdamantiumProtectedBodyPartsToDefault()
        {
            adamantiumProtectedBodyPartDefs = DefaultAdamantiumProtectedBodyPartDefNames
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            adamantiumProtectedBodyPartDefsLookup = null;
            adamantiumProtectedBodyPartDefsInitialized = true;
        }

        private void EnsureExcludedLookup()
        {
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

        private void EnsureAdamantiumProtectedBodyPartLookup()
        {
            if (adamantiumProtectedBodyPartDefsLookup != null)
            {
                return;
            }

            if (adamantiumProtectedBodyPartDefs == null)
            {
                adamantiumProtectedBodyPartDefs = new List<string>();
            }

            if (!adamantiumProtectedBodyPartDefsInitialized && adamantiumProtectedBodyPartDefs.Count == 0)
            {
                ResetAdamantiumProtectedBodyPartsToDefault();
            }

            adamantiumProtectedBodyPartDefs.RemoveAll(string.IsNullOrWhiteSpace);
            adamantiumProtectedBodyPartDefsLookup = new HashSet<string>(adamantiumProtectedBodyPartDefs, StringComparer.OrdinalIgnoreCase);
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref excludedHediffDefs, "excludedHediffDefs", LookMode.Value);
            Scribe_Collections.Look(ref adamantiumProtectedBodyPartDefs, "adamantiumProtectedBodyPartDefs", LookMode.Value);
            Scribe_Values.Look(ref enableHealing, "enableHealing", true);
            Scribe_Values.Look(ref enableRegrowth, "enableRegrowth", true);
            Scribe_Values.Look(ref boostMood, "boostMood", true);
            Scribe_Values.Look(ref forcePsychopath, "forcePsychopath", true);
            Scribe_Values.Look(ref autoResurrection, "autoResurrection", true);
            Scribe_Values.Look(ref cureDiseases, "cureDiseases", true);
            Scribe_Values.Look(ref removeScars, "removeScars", true);
            Scribe_Values.Look(ref preventHealingFactorKidnap, "preventHealingFactorKidnap", true);
            Scribe_Values.Look(ref enableClawSound, "enableClawSound", true);
            Scribe_Values.Look(ref baseHealAmount, "baseHealAmount", 0.5f);
            Scribe_Values.Look(ref ticksBetweenHeals, "ticksBetweenHeals", 250);
            Scribe_Values.Look(ref regrowSpeed, "regrowSpeed", 0.01f);
            Scribe_Values.Look(ref maxRegrowingParts, "maxRegrowingParts", 2);
            Scribe_Values.Look(ref infusionDurationTicks, "infusionDurationTicks", 30000);
            Scribe_Values.Look(ref chamberFuelRequired, "chamberFuelRequired", 500);
            Scribe_Values.Look(ref clawDamage, "clawDamage", 18f);
            Scribe_Values.Look(ref clawArmorPen, "clawArmorPen", 0.35f);
            Scribe_Values.Look(ref clawCooldown, "clawCooldown", 1.5f);
            Scribe_Values.Look(ref preferRimForgeAdamantium, "preferRimForgeAdamantium", true);
            Scribe_Values.Look(ref adamantiumProtectedBodyPartDefsInitialized, "adamantiumProtectedBodyPartDefsInitialized", false);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (excludedHediffDefs == null)
                {
                    excludedHediffDefs = new List<string>();
                }
                excludedHediffDefs.RemoveAll(string.IsNullOrWhiteSpace);
                excludedHediffDefs.Sort(StringComparer.OrdinalIgnoreCase);
                excludedHediffDefsLookup = null;
                if (adamantiumProtectedBodyPartDefs == null)
                {
                    adamantiumProtectedBodyPartDefs = new List<string>();
                }
                if (!adamantiumProtectedBodyPartDefsInitialized && adamantiumProtectedBodyPartDefs.Count == 0)
                {
                    ResetAdamantiumProtectedBodyPartsToDefault();
                }
                adamantiumProtectedBodyPartDefs.RemoveAll(string.IsNullOrWhiteSpace);
                adamantiumProtectedBodyPartDefs.Sort(StringComparer.OrdinalIgnoreCase);
                adamantiumProtectedBodyPartDefsLookup = null;
                LongEventHandler.ExecuteWhenFinished(DPClawSettingsUtility.Apply);
            }
            base.ExposeData();
        }
    }

    public class DeadpoolsHealingFactorMod : Mod
    {
        public static DeadpoolsHealingFactorSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private float scrollHeight;

        public DeadpoolsHealingFactorMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<DeadpoolsHealingFactorSettings>();
            LongEventHandler.ExecuteWhenFinished(DPClawSettingsUtility.Apply);
        }

        public override string SettingsCategory() => "Deadpool's Healing Factor";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (scrollHeight <= 0f)
            {
                scrollHeight = inRect.height;
            }

            float contentWidth = inRect.width - 18f;
            Rect outRect = inRect;
            Rect viewRect = new Rect(0f, 0f, contentWidth, scrollHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
            Listing_Standard list = new Listing_Standard();
            Rect listingRect = new Rect(0f, 0f, contentWidth, 99999f);
            list.Begin(listingRect);
            list.CheckboxLabeled("Enable Healing", ref settings.enableHealing);
            list.CheckboxLabeled("Enable Regrowth", ref settings.enableRegrowth);
            list.CheckboxLabeled("Boost Mood", ref settings.boostMood);
            list.CheckboxLabeled("Force Psychopath", ref settings.forcePsychopath);
            list.CheckboxLabeled("Auto-Resurrection", ref settings.autoResurrection);
            list.CheckboxLabeled("Cure Diseases", ref settings.cureDiseases);
            list.CheckboxLabeled("Remove Scars/Perm Injuries", ref settings.removeScars);
            list.CheckboxLabeled("Prevent kidnapping of Healing Factor pawns", ref settings.preventHealingFactorKidnap);
            if (Widgets.ButtonText(list.GetRect(30f), "Configure Hediff Heal Filter"))
            {
                Find.WindowStack.Add(new Dialog_HediffHealFilter(settings));
            }
            list.Label($"Excluded Hediffs: {settings.excludedHediffDefs?.Count ?? 0}");

            list.Label($"Ticks Between Heals: {settings.ticksBetweenHeals}");
            settings.ticksBetweenHeals = (int)list.Slider(settings.ticksBetweenHeals, 60, 600);

            list.Label($"Base Heal Amount: {settings.baseHealAmount:F2}");
            settings.baseHealAmount = list.Slider(settings.baseHealAmount, 0f, 2f);

            list.Label($"Regrow Speed: {settings.regrowSpeed:F2}");
            settings.regrowSpeed = list.Slider(settings.regrowSpeed, 0f, 0.1f);

            list.Label($"Max Regrowing Parts: {settings.maxRegrowingParts}");
            settings.maxRegrowingParts = (int)list.Slider(settings.maxRegrowingParts, 0, 5);

            list.GapLine();
            list.CheckboxLabeled("Play snikt sound when claws extend", ref settings.enableClawSound);

            list.Label($"Claw Damage: {settings.clawDamage:F1}");
            settings.clawDamage = DPClawSettingsUtility.ClampDamage(list.Slider(settings.clawDamage, 0f, 100f));
            float armorPenPercent = settings.clawArmorPen * 100f;
            list.Label($"Claw Armor Penetration: {armorPenPercent:F0}%");
            armorPenPercent = list.Slider(armorPenPercent, 0f, 100f);
            settings.clawArmorPen = DPClawSettingsUtility.ClampArmorPenetration(armorPenPercent / 100f);

            list.Label($"Claw Attack Cooldown (seconds): {settings.clawCooldown:F2}");
            settings.clawCooldown = DPClawSettingsUtility.ClampCooldown(list.Slider(settings.clawCooldown, 0.1f, 5f));

            list.GapLine();

            list.Label($"Adamantium Infusion Duration (ticks): {settings.infusionDurationTicks}");
            settings.infusionDurationTicks = (int)list.Slider(settings.infusionDurationTicks, 6000, 120000);

            list.Label($"Adamantium Chamber Fuel Required: {settings.chamberFuelRequired}");
            settings.chamberFuelRequired = (int)list.Slider(settings.chamberFuelRequired, 100, 2000);
            if (Widgets.ButtonText(list.GetRect(30f), "Configure Adamantium Protection"))
            {
                Find.WindowStack.Add(new Dialog_AdamantiumBodyPartFilter(settings));
            }
            list.Label($"Adamantium Protected Body Parts: {settings.adamantiumProtectedBodyPartDefs?.Count ?? 0}");

            list.CheckboxLabeled("Use RimForge Adamantium for infusion fuel (requires RimForge)", ref settings.preferRimForgeAdamantium);
            if (!DPRimForgeIntegration.RimForgeDetected)
            {
                list.Label("RimForge not detected. This option will fall back to Plasteel.");
            }

            float contentHeight = list.CurHeight;
            list.End();
            Widgets.EndScrollView();

            if (Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint)
            {
                scrollHeight = Mathf.Max(contentHeight + 24f, outRect.height + 1f);
            }

            if (GUI.changed)
            {
                settings.Write();
            }
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            LongEventHandler.ExecuteWhenFinished(DPClawSettingsUtility.Apply);
            LongEventHandler.ExecuteWhenFinished(DPRimForgeIntegration.HandleSettingsChanged);
        }
    }

    public class Dialog_HediffHealFilter : Window
    {
        private readonly DeadpoolsHealingFactorSettings settings;
        private readonly List<HediffDef> allHediffs;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = string.Empty;

        public override Vector2 InitialSize => new Vector2(980f, 720f);

        public Dialog_HediffHealFilter(DeadpoolsHealingFactorSettings settings)
        {
            this.settings = settings;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            allHediffs = DefDatabase<HediffDef>.AllDefsListForReading
                .Where(def => def != null && !string.IsNullOrWhiteSpace(def.defName))
                .OrderBy(GetModLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.label ?? def.defName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.defName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect searchLabelRect = new Rect(inRect.x, inRect.y, 90f, 24f);
            Widgets.Label(searchLabelRect, "Search");

            Rect searchRect = new Rect(searchLabelRect.xMax + 8f, inRect.y, inRect.width - 98f, 24f);
            searchText = Widgets.TextField(searchRect, searchText ?? string.Empty);

            Rect infoRect = new Rect(inRect.x, searchRect.yMax + 8f, inRect.width, 22f);
            Widgets.Label(infoRect, "Checked = allowed. Unchecked = manually excluded. Gray unchecked = automatically skipped by the healing code.");

            Rect buttonsRect = new Rect(inRect.x, infoRect.yMax + 8f, inRect.width, 30f);
            float buttonWidth = 160f;
            if (Widgets.ButtonText(new Rect(buttonsRect.x, buttonsRect.y, buttonWidth, buttonsRect.height), "Allow Visible"))
            {
                SetVisibleExcluded(false);
            }
            if (Widgets.ButtonText(new Rect(buttonsRect.x + buttonWidth + 10f, buttonsRect.y, buttonWidth, buttonsRect.height), "Exclude Visible"))
            {
                SetVisibleExcluded(true);
            }
            if (Widgets.ButtonText(new Rect(buttonsRect.x + ((buttonWidth + 10f) * 2f), buttonsRect.y, buttonWidth, buttonsRect.height), "Reset All"))
            {
                settings.ClearExcludedHealingHediffs();
                settings.Write();
            }

            List<HediffDef> visibleHediffs = FilteredHediffs();
            Rect listRect = new Rect(inRect.x, buttonsRect.yMax + 10f, inRect.width, inRect.height - (buttonsRect.yMax - inRect.y) - 46f);
            float rowHeight = 26f;
            float viewHeight = Mathf.Max(visibleHediffs.Count * rowHeight, listRect.height - 4f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (HediffDef def in visibleHediffs)
            {
                Rect row = new Rect(0f, y, viewRect.width, rowHeight);
                string autoSkipReason = DPHealingUtility.GetAutomaticSkipReasonForDef(def);
                bool autoSkipped = !string.IsNullOrWhiteSpace(autoSkipReason);
                string label = $"{GetDisplayLabel(def)} ({def.defName}) [{GetModLabel(def)}]";

                if (autoSkipped)
                {
                    bool displayChecked = false;
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = false;
                    Widgets.CheckboxLabeled(row, $"{label} - auto skipped: {autoSkipReason}", ref displayChecked);
                    GUI.enabled = oldEnabled;
                }
                else
                {
                    bool allowHealing = !settings.IsExcludedFromHealing(def);
                    bool newAllowHealing = allowHealing;
                    Widgets.CheckboxLabeled(row, label, ref newAllowHealing);
                    if (newAllowHealing != allowHealing)
                    {
                        settings.SetExcludedFromHealing(def.defName, !newAllowHealing);
                        settings.Write();
                    }
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

            string search = searchText.Trim();
            return allHediffs.Where(def =>
                def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || GetDisplayLabel(def).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || GetModLabel(def).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void SetVisibleExcluded(bool excluded)
        {
            foreach (HediffDef def in FilteredHediffs())
            {
                if (!string.IsNullOrWhiteSpace(DPHealingUtility.GetAutomaticSkipReasonForDef(def)))
                {
                    continue;
                }
                settings.SetExcludedFromHealing(def.defName, excluded);
            }
            settings.Write();
        }

        private static string GetDisplayLabel(HediffDef def)
        {
            return string.IsNullOrWhiteSpace(def?.label) ? def?.defName ?? "Unnamed" : def.label;
        }

        private static string GetModLabel(HediffDef def)
        {
            if (def?.modContentPack == null)
            {
                return "Core";
            }

            return !string.IsNullOrWhiteSpace(def.modContentPack.PackageId)
                ? def.modContentPack.PackageId
                : def.modContentPack.Name ?? "Unknown Mod";
        }
    }

    public class Dialog_AdamantiumBodyPartFilter : Window
    {
        private readonly DeadpoolsHealingFactorSettings settings;
        private readonly List<BodyPartDef> allBodyParts;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = string.Empty;

        public override Vector2 InitialSize => new Vector2(980f, 720f);

        public Dialog_AdamantiumBodyPartFilter(DeadpoolsHealingFactorSettings settings)
        {
            this.settings = settings;
            doCloseX = true;
            doCloseButton = true;
            absorbInputAroundWindow = true;
            allBodyParts = DefDatabase<BodyPartDef>.AllDefsListForReading
                .Where(def => def != null && !string.IsNullOrWhiteSpace(def.defName))
                .OrderBy(GetModLabel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.label ?? def.defName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(def => def.defName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect searchLabelRect = new Rect(inRect.x, inRect.y, 90f, 24f);
            Widgets.Label(searchLabelRect, "Search");

            Rect searchRect = new Rect(searchLabelRect.xMax + 8f, inRect.y, inRect.width - 98f, 24f);
            searchText = Widgets.TextField(searchRect, searchText ?? string.Empty);

            Rect infoRect = new Rect(inRect.x, searchRect.yMax + 8f, inRect.width, 36f);
            Widgets.Label(infoRect, "Checked = adamantium protects this body part. Outside child parts inherit protection from checked ancestors; internal organs do not unless checked directly.");

            Rect buttonsRect = new Rect(inRect.x, infoRect.yMax + 8f, inRect.width, 30f);
            float buttonWidth = 170f;
            if (Widgets.ButtonText(new Rect(buttonsRect.x, buttonsRect.y, buttonWidth, buttonsRect.height), "Protect Visible"))
            {
                SetVisibleProtected(true);
            }
            if (Widgets.ButtonText(new Rect(buttonsRect.x + buttonWidth + 10f, buttonsRect.y, buttonWidth, buttonsRect.height), "Unprotect Visible"))
            {
                SetVisibleProtected(false);
            }
            if (Widgets.ButtonText(new Rect(buttonsRect.x + ((buttonWidth + 10f) * 2f), buttonsRect.y, buttonWidth, buttonsRect.height), "Reset Default"))
            {
                settings.ResetAdamantiumProtectedBodyPartsToDefault();
                settings.Write();
            }

            List<BodyPartDef> visibleBodyParts = FilteredBodyParts();
            Rect listRect = new Rect(inRect.x, buttonsRect.yMax + 10f, inRect.width, inRect.height - (buttonsRect.yMax - inRect.y) - 46f);
            float rowHeight = 26f;
            float viewHeight = Mathf.Max(visibleBodyParts.Count * rowHeight, listRect.height - 4f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

            float y = 0f;
            foreach (BodyPartDef def in visibleBodyParts)
            {
                Rect row = new Rect(0f, y, viewRect.width, rowHeight);
                bool protect = settings.IsAdamantiumProtectedBodyPart(def);
                bool newProtect = protect;
                string label = $"{GetDisplayLabel(def)} ({def.defName}) [{GetModLabel(def)}]";
                Widgets.CheckboxLabeled(row, label, ref newProtect);
                if (newProtect != protect)
                {
                    settings.SetAdamantiumProtectedBodyPart(def.defName, newProtect);
                    settings.Write();
                }
                y += rowHeight;
            }
            Widgets.EndScrollView();
        }

        private List<BodyPartDef> FilteredBodyParts()
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return allBodyParts;
            }

            string search = searchText.Trim();
            return allBodyParts.Where(def =>
                def.defName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || GetDisplayLabel(def).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || GetModLabel(def).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void SetVisibleProtected(bool protect)
        {
            foreach (BodyPartDef def in FilteredBodyParts())
            {
                settings.SetAdamantiumProtectedBodyPart(def.defName, protect);
            }
            settings.Write();
        }

        private static string GetDisplayLabel(BodyPartDef def)
        {
            return string.IsNullOrWhiteSpace(def?.label) ? def?.defName ?? "Unnamed" : def.label;
        }

        private static string GetModLabel(BodyPartDef def)
        {
            if (def?.modContentPack == null)
            {
                return "Core";
            }

            return !string.IsNullOrWhiteSpace(def.modContentPack.PackageId)
                ? def.modContentPack.PackageId
                : def.modContentPack.Name ?? "Unknown Mod";
        }
    }
}
