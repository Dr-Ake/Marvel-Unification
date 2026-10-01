using Verse;
using UnityEngine;

namespace JeanGreyMod
{
    public class JeanGreySettings : ModSettings
    {
        public static float TelekineticPushDamage = 10f;
        public static float MolecularDeconstructionDamage = 50f;
        public static float ResurrectionCooldownDays = 1.0f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref TelekineticPushDamage, "TelekineticPushDamage", 10f);
            Scribe_Values.Look(ref MolecularDeconstructionDamage, "MolecularDeconstructionDamage", 50f);
            Scribe_Values.Look(ref ResurrectionCooldownDays, "ResurrectionCooldownDays", 1.0f);
            base.ExposeData();
        }
    }
}
