using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace Rimworld_Storm
{
    public class Ability_StormControl : Ability
    {
        public Ability_StormControl(Pawn pawn) : base(pawn)
        {
        }

        public Ability_StormControl(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public override IEnumerable<Command> GetGizmos()
        {
            // Do NOT call base.GetGizmos() to avoid the default command
            
            yield return new Command_Action
            {
                defaultLabel = "Summon Storm",
                defaultDesc = "Summon a specific type of storm.",
                icon = this.def.uiIcon,
                action = () =>
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    
                    options.Add(new FloatMenuOption("Clear", () => Summon(WeatherDef.Named("Clear"))));
                    options.Add(new FloatMenuOption("Rain", () => Summon(WeatherDef.Named("Rain"))));
                    options.Add(new FloatMenuOption("Rainy Thunderstorm", () => Summon(WeatherDef.Named("RainyThunderstorm"))));
                    options.Add(new FloatMenuOption("Foggy Rain", () => Summon(WeatherDef.Named("FoggyRain"))));
                    options.Add(new FloatMenuOption("Snow (Gentle)", () => Summon(WeatherDef.Named("SnowGentle"))));
                    options.Add(new FloatMenuOption("Snow (Hard)", () => Summon(WeatherDef.Named("SnowHard"))));

                    Find.WindowStack.Add(new FloatMenu(options));
                }
            };
        }

        private void Summon(WeatherDef weather)
        {
            Map map = pawn.Map;
            if (map != null)
            {
                var component = map.GetComponent<StormGodComponent>();
                if (component != null)
                {
                    component.SummonStorm(60000, weather); // 1 day storm
                    Messages.Message("Storm summoned: " + weather.label, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
        
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // Fallback if triggered by AI or other means
            Summon(WeatherDef.Named("RainyThunderstorm"));
            return true;
        }
    }
}
