using Verse;
using HarmonyLib;
using UnityEngine;

namespace JeanGreyMod
{
    public class JeanGreyMod : Mod
    {
        public static JeanGreySettings settings;

        public JeanGreyMod(ModContentPack content) : base(content)
        {
            Log.Message("[JeanGreyMod] Initializing Harmony patches...");
            MarvelUnification.MarvelHarmony.EnsurePatched();
            Log.Message("[JeanGreyMod] Harmony patches initialized.");
            
            settings = GetSettings<JeanGreySettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.Label($"Telekinetic Push Damage: {JeanGreySettings.TelekineticPushDamage}");
            JeanGreySettings.TelekineticPushDamage = listingStandard.Slider(JeanGreySettings.TelekineticPushDamage, 1f, 50f);
            
            listingStandard.Label($"Molecular Deconstruction Damage: {JeanGreySettings.MolecularDeconstructionDamage}");
            JeanGreySettings.MolecularDeconstructionDamage = listingStandard.Slider(JeanGreySettings.MolecularDeconstructionDamage, 10f, 200f);
            
            listingStandard.Label($"Resurrection Cooldown (Days): {JeanGreySettings.ResurrectionCooldownDays}");
            JeanGreySettings.ResurrectionCooldownDays = listingStandard.Slider(JeanGreySettings.ResurrectionCooldownDays, 0.1f, 10f);
            
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Jean Grey - Phoenix Force";
        }
    }
}
