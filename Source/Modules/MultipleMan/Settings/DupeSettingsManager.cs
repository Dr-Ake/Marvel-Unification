using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public static class DupeSettingsManager
    {
        public static DupesSettings Settings => DupesMod.Instance.Settings;

        public static float DurationInTicks
        {
            get
            {
                if (Settings.DupeDurationHours <= 0f)
                {
                    return -1f;
                }

                return Settings.DupeDurationHours * GenDate.TicksPerHour;
            }
        }

        public static bool ResearchAllowed => Settings.ResearchPermission != ResearchPermission.Disabled;

        public static bool UnlimitedDupes => Settings.UnlimitedDupes;

        public static bool RitualParticipationAllowed => Settings.AllowRitualParticipation;

        public static bool NeedsAllowed => Settings.AllowNeeds;

        public static bool MoodAllowed => Settings.AllowMood;

        public static bool SocialAllowed => Settings.AllowSocialInteractions;

        public static bool BedOwnershipAllowed => Settings.AllowBedOwnership;

        public static bool RipScannerUsageAllowed => Settings.AllowRipScannerUsage;

        public static bool PromoteDupeOnDeath => Settings.PromoteDupeOnDeath;

        public static bool ForceInjectorMode => Settings.ForceInjectorMode;

        public static bool UseGeneMode => ModsConfig.BiotechActive && !Settings.ForceInjectorMode;

        public static bool UseInjectorMode => !UseGeneMode;

        public static bool DraftDupesWhenOriginalDrafted => Settings.DraftDupesWhenOriginalDrafted;
    }
}
