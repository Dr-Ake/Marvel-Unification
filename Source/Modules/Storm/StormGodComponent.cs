using System;
using Verse;
using RimWorld;

namespace Rimworld_Storm
{
    public class StormGodComponent : MapComponent
    {
        public bool isStormActive = false;
        public int stormTicksLeft = 0;
        public WeatherDef targetWeather;

        public StormGodComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isStormActive, "isStormActive", false);
            Scribe_Values.Look(ref stormTicksLeft, "stormTicksLeft", 0);
            Scribe_Defs.Look(ref targetWeather, "targetWeather");
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (isStormActive)
            {
                stormTicksLeft--;
                if (stormTicksLeft <= 0)
                {
                    isStormActive = false;
                    targetWeather = null;
                    Messages.Message("The storm dissipates.", MessageTypeDefOf.NeutralEvent);
                }
                else
                {
                    // We rely on HarmonyPatches to force the weather commonality.
                    // We do NOT call TransitionTo here repeatedly, as it resets weather age
                    // and prevents events (lightning) from firing.
                    // HOWEVER, we keep the robust enforcement check below to prevent "Double Click" issues.
                    if (targetWeather != null && map.weatherManager.curWeather != targetWeather)
                    {
                        map.weatherManager.TransitionTo(targetWeather);
                    }
                }
            }
        }

        public void SummonStorm(int durationTicks, WeatherDef weather = null)
        {
            isStormActive = true;
            stormTicksLeft = durationTicks;
            
            if (weather == null)
            {
                weather = WeatherDef.Named("RainyThunderstorm");
            }
            
            targetWeather = weather;
            map.weatherManager.TransitionTo(weather);
            map.weatherManager.lastWeather = weather; // Force instant visual transition
            map.weatherManager.curWeatherAge = 1000; // Make weather established
        }
    }
}
