using RimWorld;
using UnityEngine;
using Verse;

namespace MultipleManXGene
{
    public enum ResearchPermission
    {
        Disabled,
        Allowed,
        AllowedWithSpeedSlider
    }

    public class DupesSettings : ModSettings
    {
        private int maxActiveDupes = 5;
        private float dupeDurationHours = 24f;
        private bool vanishWhenOriginalSleeps = true;
        private bool promoteDupeOnDeath = true;
        private bool forceInjectorMode = false;
        private bool draftDupesWhenOriginalDrafted = false;
        private float workSpeedMultiplier = 1f;
        private float moveSpeedMultiplier = 1f;
        private float combatDamageMultiplier = 1f;
        private float combatAccuracyMultiplier = 1f;
        private bool skillXpGainEnabled = false;
        private ResearchPermission researchPermission = ResearchPermission.Disabled;
        private float healthScaling = 1f;
        private bool cannotBeDowned = false;
        private bool ignorePain = false;
        private float summonCooldownHours = 0f;
        private bool unlimitedDupes = false;
        private bool allowRitualParticipation = false;
        private bool allowNeeds = false;
        private bool allowMood = false;
        private bool allowSocialInteractions = false;
        private bool allowBedOwnership = false;
        private bool allowRipScannerUsage = true;
        private Vector2 settingsScrollPosition = Vector2.zero;
        private float settingsViewHeight = 1200f;

        public int MaxActiveDupes
        {
            get => maxActiveDupes;
            set => maxActiveDupes = Mathf.Clamp(value, 1, 999);
        }

        public bool UnlimitedDupes
        {
            get => unlimitedDupes;
            set => unlimitedDupes = value;
        }

        public float DupeDurationHours
        {
            get => dupeDurationHours;
            set => dupeDurationHours = Mathf.Max(0f, value);
        }

        public bool VanishWhenOriginalSleeps
        {
            get => vanishWhenOriginalSleeps;
            set => vanishWhenOriginalSleeps = value;
        }

        public bool PromoteDupeOnDeath
        {
            get => promoteDupeOnDeath;
            set => promoteDupeOnDeath = value;
        }

        public bool ForceInjectorMode
        {
            get => forceInjectorMode;
            set => forceInjectorMode = value;
        }

        public bool DraftDupesWhenOriginalDrafted
        {
            get => draftDupesWhenOriginalDrafted;
            set => draftDupesWhenOriginalDrafted = value;
        }

        public float WorkSpeedMultiplier
        {
            get => workSpeedMultiplier;
            set => workSpeedMultiplier = Mathf.Clamp(value, 0f, 10f);
        }

        public float MoveSpeedMultiplier
        {
            get => moveSpeedMultiplier;
            set => moveSpeedMultiplier = Mathf.Clamp(value, 0.5f, 5f);
        }

        public float CombatDamageMultiplier
        {
            get => combatDamageMultiplier;
            set => combatDamageMultiplier = Mathf.Clamp(value, 0f, 5f);
        }

        public float CombatAccuracyMultiplier
        {
            get => combatAccuracyMultiplier;
            set => combatAccuracyMultiplier = Mathf.Clamp(value, 0f, 3f);
        }

        public bool SkillXpGainEnabled
        {
            get => skillXpGainEnabled;
            set => skillXpGainEnabled = value;
        }

        public ResearchPermission ResearchPermission
        {
            get => researchPermission;
            set => researchPermission = value;
        }

        public float HealthScaling
        {
            get => healthScaling;
            set => healthScaling = Mathf.Clamp(value, 0.01f, 5f);
        }

        public bool CannotBeDowned
        {
            get => cannotBeDowned;
            set => cannotBeDowned = value;
        }

        public bool IgnorePain
        {
            get => ignorePain;
            set => ignorePain = value;
        }

        public float SummonCooldownHours
        {
            get => summonCooldownHours;
            set => summonCooldownHours = Mathf.Max(0f, value);
        }

        public bool AllowRitualParticipation
        {
            get => allowRitualParticipation;
            set => allowRitualParticipation = value;
        }

        public bool AllowNeeds
        {
            get => allowNeeds;
            set => allowNeeds = value;
        }

        public bool AllowMood
        {
            get => allowMood;
            set => allowMood = value;
        }

        public bool AllowSocialInteractions
        {
            get => allowSocialInteractions;
            set => allowSocialInteractions = value;
        }

        public bool AllowBedOwnership
        {
            get => allowBedOwnership;
            set => allowBedOwnership = value;
        }

        public bool AllowRipScannerUsage
        {
            get => allowRipScannerUsage;
            set => allowRipScannerUsage = value;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxActiveDupes, nameof(maxActiveDupes), 5);
            Scribe_Values.Look(ref dupeDurationHours, nameof(dupeDurationHours), 24f);
            Scribe_Values.Look(ref vanishWhenOriginalSleeps, nameof(vanishWhenOriginalSleeps), true);
            Scribe_Values.Look(ref promoteDupeOnDeath, nameof(promoteDupeOnDeath), true);
            Scribe_Values.Look(ref forceInjectorMode, nameof(forceInjectorMode), false);
            Scribe_Values.Look(ref draftDupesWhenOriginalDrafted, nameof(draftDupesWhenOriginalDrafted), false);
            Scribe_Values.Look(ref workSpeedMultiplier, nameof(workSpeedMultiplier), 1f);
            Scribe_Values.Look(ref moveSpeedMultiplier, nameof(moveSpeedMultiplier), 1f);
            Scribe_Values.Look(ref combatDamageMultiplier, nameof(combatDamageMultiplier), 1f);
            Scribe_Values.Look(ref combatAccuracyMultiplier, nameof(combatAccuracyMultiplier), 1f);
            Scribe_Values.Look(ref skillXpGainEnabled, nameof(skillXpGainEnabled));
            Scribe_Values.Look(ref researchPermission, nameof(researchPermission), ResearchPermission.Disabled);
            Scribe_Values.Look(ref healthScaling, nameof(healthScaling), 1f);
            Scribe_Values.Look(ref cannotBeDowned, nameof(cannotBeDowned));
            Scribe_Values.Look(ref ignorePain, nameof(ignorePain));
            Scribe_Values.Look(ref summonCooldownHours, nameof(summonCooldownHours));
            Scribe_Values.Look(ref unlimitedDupes, nameof(unlimitedDupes), false);
            Scribe_Values.Look(ref allowRitualParticipation, nameof(allowRitualParticipation), false);
            Scribe_Values.Look(ref allowNeeds, nameof(allowNeeds), false);
            Scribe_Values.Look(ref allowMood, nameof(allowMood), false);
            Scribe_Values.Look(ref allowSocialInteractions, nameof(allowSocialInteractions), false);
            Scribe_Values.Look(ref allowBedOwnership, nameof(allowBedOwnership), false);
            Scribe_Values.Look(ref allowRipScannerUsage, nameof(allowRipScannerUsage), true);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            string Info(string label, string explanation) => $"{label} [{explanation}]";

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, settingsViewHeight);
            Widgets.BeginScrollView(inRect, ref settingsScrollPosition, viewRect);

            var listing = new Listing_Standard { ColumnWidth = viewRect.width };
            listing.Begin(viewRect);
            void Section(string title)
            {
                listing.GapLine();
                listing.Label(title);
                listing.Gap(4f);
            }

            listing.Label("Limits");
            listing.Gap(4f);
            listing.CheckboxLabeled(Info("Unlimited dupes", "Ignore the max dupe limit"), ref unlimitedDupes);
            if (!unlimitedDupes)
            {
                listing.Label(Info("Max active dupes", "Limits simultaneous dupes") + ": " + MaxActiveDupes);
                MaxActiveDupes = Mathf.RoundToInt(listing.Slider(MaxActiveDupes, 1, 100));
            }

            listing.Label(Info("Dupe duration", "Time before dupes expire (0 = permanent)") + ": " + DupeDurationHours.ToString("F1") + "h");
            DupeDurationHours = Mathf.Round(listing.Slider(DupeDurationHours, 0f, 96f) * 10f) / 10f;
            listing.Label(Info("Summon cooldown", "Delay between summons (hours)") + ": " + SummonCooldownHours.ToString("F1"));
            SummonCooldownHours = Mathf.Round(listing.Slider(SummonCooldownHours, 0f, 24f) * 10f) / 10f;

            Section("Summon behavior");
            listing.CheckboxLabeled(Info("Vanish when original sleeps", "Despawns all dupes when master rests"), ref vanishWhenOriginalSleeps);
            listing.CheckboxLabeled(Info("Promote dupe when original dies", "If off, dupes never replace the master on death"), ref promoteDupeOnDeath);
            listing.CheckboxLabeled(Info("Draft dupes with original", "If the original is drafted, new dupes start drafted"), ref draftDupesWhenOriginalDrafted);

            Section("Gene / injector");
            listing.CheckboxLabeled(Info("Force injector mode", "Use the injector/hediff even when Biotech is active"), ref forceInjectorMode);

            Section("Multipliers");
            listing.Label(Info("Work speed multiplier", "Scales job completion speed") + ": " + WorkSpeedMultiplier.ToString("F2") + "x");
            WorkSpeedMultiplier = listing.Slider(WorkSpeedMultiplier, 0f, 10f);

            listing.Label(Info("Move speed multiplier", "Scales movement speed") + ": " + MoveSpeedMultiplier.ToString("F2") + "x");
            MoveSpeedMultiplier = listing.Slider(MoveSpeedMultiplier, 0.5f, 5f);

            listing.Label(Info("Combat damage multiplier", "Scales melee/ranged damage") + ": " + CombatDamageMultiplier.ToString("F2") + "x");
            CombatDamageMultiplier = listing.Slider(CombatDamageMultiplier, 0f, 5f);

            listing.Label(Info("Combat accuracy multiplier", "Scales hit chance") + ": " + CombatAccuracyMultiplier.ToString("F2") + "x");
            CombatAccuracyMultiplier = listing.Slider(CombatAccuracyMultiplier, 0f, 3f);

            listing.CheckboxLabeled(Info("Allow skill XP gain", "Let dupes learn while active"), ref skillXpGainEnabled);

            Section("Research");
            listing.Label(Info("Research permission", "Controls whether dupes can research") + ": " + researchPermission);
            if (listing.ButtonText("Cycle"))
            {
                researchPermission = (ResearchPermission)(((int)researchPermission + 1) % 3);
            }

            Section("Health");
            listing.Label(Info("Health scaling", "Scales dupe HP pool") + ": " + HealthScaling.ToString("F2") + "x");
            HealthScaling = listing.Slider(HealthScaling, 0.01f, 5f);

            listing.CheckboxLabeled(Info("Cannot be downed (vanish instead)", "Dupes disappear instead of falling"), ref cannotBeDowned);
            listing.CheckboxLabeled(Info("Ignore pain", "Blocks pain debuffs"), ref ignorePain);

            Section("Needs and social");
            listing.CheckboxLabeled(Info("Allow needs", "If off, dupes have no needs (food/rest/etc)"), ref allowNeeds);
            listing.CheckboxLabeled(Info("Allow mood", "If off, dupes have no mood/thoughts/mental breaks"), ref allowMood);
            listing.CheckboxLabeled(Info("Allow social/romance", "If off, dupes won't start social/romantic interactions"), ref allowSocialInteractions);

            Section("Permissions");
            listing.CheckboxLabeled(Info("Allow dupes in rituals", "If off, dupes will be excluded from ritual role lists"), ref allowRitualParticipation);
            listing.CheckboxLabeled(Info("Allow bed ownership", "If off, dupes cannot claim non-medical beds"), ref allowBedOwnership);
            listing.CheckboxLabeled(Info("Allow rip scanner use", "If off, dupes cannot be inserted into rip scanners for high subcores"), ref allowRipScannerUsage);

            var contentHeight = listing.CurHeight + 20f;
            listing.End();
            if (contentHeight > settingsViewHeight)
            {
                settingsViewHeight = contentHeight;
            }
            if (settingsViewHeight <= inRect.height)
            {
                settingsViewHeight = inRect.height + 1f;
            }

            var maxScroll = Mathf.Max(0f, settingsViewHeight - inRect.height);
            settingsScrollPosition.y = Mathf.Clamp(settingsScrollPosition.y, 0f, maxScroll);

            Widgets.EndScrollView();
        }
    }
}
