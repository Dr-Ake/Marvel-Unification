using RimWorld;
using Verse;

namespace MultipleManXGene
{
    public class CompProperties_UseEffectMultipleManGene : CompProperties_UseEffect
    {
        public CompProperties_UseEffectMultipleManGene()
        {
            compClass = typeof(CompUseEffect_GiveMultipleManGene);
        }
    }
}
