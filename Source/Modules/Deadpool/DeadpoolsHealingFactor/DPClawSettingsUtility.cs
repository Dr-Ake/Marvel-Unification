using UnityEngine;
using Verse;
using System.Linq;

namespace DeadpoolsHealingFactor
{
    public static class DPClawSettingsUtility
    {
        private const float MinDamage = 0f;
        private const float MaxDamage = 100f;
        private const float MinCooldown = 0.1f;
        private const float MaxCooldown = 10f;

        public static float ClampDamage(float value) => Mathf.Clamp(value, MinDamage, MaxDamage);

        public static float ClampArmorPenetration(float value) => Mathf.Clamp01(value);

        public static float ClampCooldown(float value) => Mathf.Clamp(value, MinCooldown, MaxCooldown);

        public static void Apply()
        {
            var settings = DeadpoolsHealingFactorMod.settings;
            if (settings == null)
            {
                return;
            }

            settings.clawDamage = ClampDamage(settings.clawDamage);
            settings.clawArmorPen = ClampArmorPenetration(settings.clawArmorPen);
            settings.clawCooldown = ClampCooldown(settings.clawCooldown);

            ThingDef clawsDef = DPDefOf.DP_WolverineClawWeapon;
            if (clawsDef?.tools == null)
            {
                return;
            }

            foreach (Tool tool in clawsDef.tools)
            {
                if (tool == null)
                {
                    continue;
                }
                tool.power = settings.clawDamage;
                tool.armorPenetration = settings.clawArmorPen;
                tool.cooldownTime = settings.clawCooldown;
            }
        }
    }
}

