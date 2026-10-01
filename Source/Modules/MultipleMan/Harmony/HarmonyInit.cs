using HarmonyLib;
using Verse;

namespace MultipleManXGene
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            MarvelUnification.MarvelHarmony.EnsurePatched();
        }
    }
}
