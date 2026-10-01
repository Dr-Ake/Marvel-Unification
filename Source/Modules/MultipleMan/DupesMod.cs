using UnityEngine;
using Verse;

namespace MultipleManXGene
{
    public class DupesMod : Mod
    {
        public static DupesMod Instance { get; private set; } = null!;
        public DupesSettings Settings { get; }

        public DupesMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<DupesSettings>();
        }

        public override string SettingsCategory() => "Dupes";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }
    }
}
