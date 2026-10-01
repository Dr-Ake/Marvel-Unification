using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace JeanGreyMod
{
    public static class PsionicMechanics
    {
        public static void DoTelekineticPush(Pawn caster, LocalTargetInfo target)
        {
            if (target.Cell == default(IntVec3)) return;

            // Visual Mote: Distortion Wave (Telekinetic Force)
            ThingDef moteDef = ThingDef.Named("Mote_PsionicBlast");
            if (moteDef != null)
            {
                MoteMaker.MakeStaticMote(target.Cell, caster.Map, moteDef, 2.0f);
            }
            
            // Sound Effect
            SoundDef sound = SoundDef.Named("Explosion_Stun");
            if (sound != null)
            {
                sound.PlayOneShot(new TargetInfo(target.Cell, caster.Map));
            }

            // The "Push" - Single Target Damage (Telepathic)
            if (target.Thing != null)
            {
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, JeanGreySettings.TelekineticPushDamage, 0f, -1f, caster, null, null);
                target.Thing.TakeDamage(dinfo);
                
                // Optional: Add a small knockback or stun if possible
                if (target.Thing is Pawn p)
                {
                    p.stances.stunner.StunFor(60, caster); // 1 second stun
                }
            }
        }

        public static void DoMindBlast(Pawn caster, LocalTargetInfo target)
        {
             if (target.Cell == default(IntVec3)) return;

             // Visuals: Phoenix Flash + Smoke (Destructive)
             MoteMaker.MakeStaticMote(target.Cell, caster.Map, ThingDef.Named("Mote_PhoenixFlash"), 3.0f);
             
             // Add smoke to indicate disintegration
             MoteMaker.MakeStaticMote(target.Cell, caster.Map, ThingDef.Named("Mote_PsionicSmoke"), 1.5f);

             SoundDef sound = SoundDef.Named("Explosion_Bomb");
             if (sound != null)
             {
                 sound.PlayOneShot(new TargetInfo(target.Cell, caster.Map));
             }

             // High damage single target (Deconstruction)
             if (target.Thing != null)
             {
                 // Using Bomb damage type for the "deconstruction" feel but applied directly
                 DamageInfo dinfo = new DamageInfo(DamageDefOf.Bomb, JeanGreySettings.MolecularDeconstructionDamage, 100f, -1f, caster, null, null);
                 target.Thing.TakeDamage(dinfo);
             }
        }
    }
}
