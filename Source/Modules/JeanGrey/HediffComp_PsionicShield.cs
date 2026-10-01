using Verse;

namespace JeanGreyMod
{
    public class HediffCompProperties_PsionicShield : HediffCompProperties
    {
        public HediffCompProperties_PsionicShield()
        {
            this.compClass = typeof(HediffComp_PsionicShield);
        }
    }

    public class HediffComp_PsionicShield : HediffComp
    {
        // This class primarily serves as a marker for the HediffDef.
        // The actual logic is handled by the Harmony patch Patch_PreApplyDamage.
        // However, we could add shield strength regeneration logic here in the future.
    }
}
