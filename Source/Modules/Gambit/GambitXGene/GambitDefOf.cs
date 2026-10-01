using RimWorld;
using Verse;

namespace GambitXGene;

[DefOf]
public static class GambitDefOf
{
    public static HediffDef Gambit_DeckHediff = null!;
    public static AbilityDef Gambit_TouchCharge = null!;
    public static AbilityDef Gambit_ThrowCard = null!;
    public static AbilityDef Gambit_52CardPickup = null!;
    public static AbilityDef Gambit_KineticToggle = null!;
    public static GeneDef Gambit_XGene = null!;
    public static HediffDef Gambit_XGene_Hediff = null!;

    static GambitDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(GambitDefOf));
    }
}
