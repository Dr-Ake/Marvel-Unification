using Verse;
using UnityEngine;

namespace Rimworld_Storm
{
    public class StormMod : Mod
    {
        public static StormSettings settings;

        public StormMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<StormSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            
            listingStandard.Label($"Projectile Deflection Chance: {StormSettings.deflectionChance:P0}");
            StormSettings.deflectionChance = listingStandard.Slider(StormSettings.deflectionChance, 0f, 1f);
            
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Storm Gene Injector";
        }
    }
}
