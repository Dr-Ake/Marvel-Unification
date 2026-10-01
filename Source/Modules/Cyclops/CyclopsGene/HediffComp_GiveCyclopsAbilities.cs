using RimWorld;
using Verse;

namespace CyclopsGene
{
    public class HediffCompProperties_GiveCyclopsVolley : HediffCompProperties_GiveAbility
    {
        public HediffCompProperties_GiveCyclopsVolley()
        {
            compClass = typeof(HediffComp_GiveCyclopsVolley);
        }
    }

    public class HediffComp_GiveCyclopsVolley : HediffComp_GiveAbility
    {
    }

    public class HediffCompProperties_GiveCyclopsFocusedBeam : HediffCompProperties_GiveAbility
    {
        public HediffCompProperties_GiveCyclopsFocusedBeam()
        {
            compClass = typeof(HediffComp_GiveCyclopsFocusedBeam);
        }
    }

    public class HediffComp_GiveCyclopsFocusedBeam : HediffComp_GiveAbility
    {
    }

    public class HediffCompProperties_GiveCyclopsFullAperture : HediffCompProperties_GiveAbility
    {
        public HediffCompProperties_GiveCyclopsFullAperture()
        {
            compClass = typeof(HediffComp_GiveCyclopsFullAperture);
        }
    }

    public class HediffComp_GiveCyclopsFullAperture : HediffComp_GiveAbility
    {
    }
}
