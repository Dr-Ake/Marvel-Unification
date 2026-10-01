using RimWorld;
using Verse;

namespace MultipleManXGene
{
    [DefOf]
    public static class MMDefOf
    {
        [MayRequire("Ludeon.RimWorld.Biotech")]
        public static GeneDef MM_MultipleManGene = null!;
        public static HediffDef MM_MultipleManGeneHediff = null!;
        public static ThingDef MM_MultipleManInjector = null!;
        public static RecipeDef MM_AdministerMultipleManInjector = null!;

        static MMDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MMDefOf));
        }
    }
}
