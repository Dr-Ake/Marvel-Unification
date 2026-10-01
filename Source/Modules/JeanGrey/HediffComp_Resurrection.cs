using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace JeanGreyMod
{
    public class HediffCompProperties_Resurrection : HediffCompProperties
    {
        public HediffCompProperties_Resurrection()
        {
            this.compClass = typeof(HediffComp_Resurrection);
        }
    }

    public class HediffComp_Resurrection : HediffComp
    {
        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff hediff)
        {
            // Prevent infinite loops if the pawn was already "drained"
            if (this.Pawn.health.hediffSet.HasHediff(HediffDef.Named("JG_PhoenixCooldown"))) return;

            // Queue the Resurrection Event
            LongEventHandler.QueueLongEvent(() =>
            {
                PhoenixRising(this.Pawn, this.Pawn.Corpse);
            }, "PhoenixRising", false, null);
        }

        private void PhoenixRising(Pawn pawn, Corpse corpse)
        {
            if (corpse == null || !corpse.Spawned) return;

            IntVec3 pos = corpse.Position;
            Map map = corpse.Map;

            // 1. Thermodynamic Explosion (Fire and Force)
            GenExplosion.DoExplosion(
                center: pos,
                map: map,
                radius: 5.9f,
                damType: DamageDefOf.Flame,
                instigator: null,
                damAmount: 20,
                chanceToStartFire: 1.0f, // Guaranteed fire
                damageFalloff: true
            );

            // Visuals: Phoenix Resurrection Mote
            ThingDef moteDef = ThingDef.Named("Mote_PhoenixResurrection");
            if (moteDef != null)
            {
                MoteMaker.MakeStaticMote(pos, map, moteDef, 5.0f);
            }

            // Sound
            SoundDef sound = SoundDef.Named("Explosion_Bomb");
            if (sound != null)
            {
                SoundStarter.PlayOneShot(sound, new TargetInfo(pos, map));
            }

            // 2. Resurrection
            ResurrectionUtility.TryResurrect(corpse.InnerPawn);

            // 3. Post-Resurrection State
            Pawn resurrectedPawn = pos.GetFirstPawn(map);
            if (resurrectedPawn != null)
            {
                // Re-inject the Phoenix Force
                Hediff newPhoenix = HediffMaker.MakeHediff(HediffDef.Named("JG_PhoenixForce"), resurrectedPawn);
                newPhoenix.Severity = 0.5f; // Return at weakened state
                resurrectedPawn.health.AddHediff(newPhoenix);

                // Apply Cooldown/Sickness
                Hediff cooldown = HediffMaker.MakeHediff(HediffDef.Named("JG_PhoenixCooldown"), resurrectedPawn);
                cooldown.Severity = JeanGreySettings.ResurrectionCooldownDays; // Use setting
                resurrectedPawn.health.AddHediff(cooldown);
                
                Messages.Message(resurrectedPawn.Name + " has risen from the ashes!", MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
