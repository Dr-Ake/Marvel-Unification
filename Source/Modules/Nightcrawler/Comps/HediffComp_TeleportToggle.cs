using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.Sound;

namespace NightcrawlerTeleportation
{
    public class HediffCompProperties_TeleportToggle : HediffCompProperties
    {
        public bool defaultEnabled;
        public bool checkFog = true;
        public bool playSound;
        public bool allowWhileDrafted = true;
        public bool requireConscious = true;
        public int teleportDistanceThreshold = 10;
        public SoundDef? longJumpSound;
        public FleckDef? teleportEffect;
        public string toggleLabelKey = "Nightcrawler.TeleportToggle_Label";
        public string toggleDescriptionKey = "Nightcrawler.TeleportToggle_Desc";

        public HediffCompProperties_TeleportToggle()
        {
            compClass = typeof(HediffComp_TeleportToggle);
        }
    }

    public class HediffComp_TeleportToggle : HediffComp
    {
        private bool teleportEnabled;

        public HediffCompProperties_TeleportToggle Props => (HediffCompProperties_TeleportToggle)props;

        public bool TeleportEnabled => teleportEnabled;

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref teleportEnabled, "teleportEnabled", Props.defaultEnabled);
        }

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            teleportEnabled = Props.defaultEnabled;
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            var pawn = parent?.pawn;
            if (pawn == null || !pawn.Spawned)
            {
                yield break;
            }

            if (!pawn.IsColonistPlayerControlled)
            {
                yield break;
            }

            yield return BuildToggleGizmo(pawn);
        }

        public void SetTeleportEnabled(bool value)
        {
            if (teleportEnabled == value)
            {
                return;
            }

            teleportEnabled = value;
        }

        public void ToggleTeleport()
        {
            SetTeleportEnabled(!teleportEnabled);
        }

        public void OnTeleported(Pawn pawn, IntVec3 origin, IntVec3 destination)
        {
            if (!destination.IsValid || pawn.Map == null)
            {
                return;
            }

            if (Props.teleportEffect != null)
            {
                if (origin.IsValid)
                {
                    FleckMaker.Static(origin, pawn.Map, Props.teleportEffect);
                }

                FleckMaker.Static(destination, pawn.Map, Props.teleportEffect);
            }

            if (Props.playSound && Props.longJumpSound != null)
            {
                if (origin.IsValid && origin.DistanceTo(destination) >= Props.teleportDistanceThreshold)
                {
                    SoundStarter.PlayOneShot(Props.longJumpSound, SoundInfo.InMap(new TargetInfo(destination, pawn.Map)));
                }
            }
        }

        private Command_Toggle BuildToggleGizmo(Pawn pawn)
        {
            var command = new Command_Toggle
            {
                defaultLabel = Props.toggleLabelKey.Translate(),
                defaultDesc = Props.toggleDescriptionKey.Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/NightcrawlerTeleportToggle", reportFailure: false),
                hotKey = KeyBindingDefOf.Misc2,
                isActive = () => teleportEnabled,
                toggleAction = ToggleTeleport
            };

        if (command.icon == null)
        {
            command.icon = TexCommand.Draft;
        }

            return command;
        }
    }
}
