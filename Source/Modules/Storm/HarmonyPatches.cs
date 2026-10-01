using System;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace Rimworld_Storm
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            MarvelUnification.MarvelHarmony.EnsurePatched();
        }
    }

    [HarmonyPatch(typeof(WeatherDecider), "CurrentWeatherCommonality")]
    public static class Patch_WeatherDecider_CurrentWeatherCommonality
    {
        public static void Postfix(WeatherDecider __instance, WeatherDef weather, ref float __result)
        {
            // Access the map via reflection or helper if needed, but WeatherDecider has a 'map' field.
            // Since 'map' is private in WeatherDecider, we need to traverse or use AccessTools.
            // Alternatively, we can check if ANY map has a storm active, but that's inefficient.
            
            // Better approach: Get the map from the instance.
            Map map = Traverse.Create(__instance).Field("map").GetValue<Map>();
            
            if (map != null)
            {
                var component = map.GetComponent<StormGodComponent>();
                if (component != null && component.isStormActive && component.targetWeather != null)
                {
                    if (weather == component.targetWeather)
                    {
                        __result = 10000f; // Massive commonality
                    }
                    else
                    {
                        __result = 0f; // Impossible
                    }
                }
            }
        }
    }
}
