using System.Reflection;
using HarmonyLib;

namespace MarvelUnification
{
    internal static class MarvelHarmony
    {
        private static bool patched;

        // All modules now live in this assembly. Apply its patches exactly once.
        internal static void EnsurePatched()
        {
            if (patched) return;
            new Harmony("DrAke.MarvelUnification").PatchAll(Assembly.GetExecutingAssembly());
            patched = true;
        }
    }
}
