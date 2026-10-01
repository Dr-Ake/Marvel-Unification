using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DeadpoolsHealingFactor
{
    public static class DPRimForgeIntegration
    {
        public const string RimForgePackageId = "co.uk.epicguru.rimforge";
        public const string RimForgeAdamantiumDefName = "RF_Adamantium";

        private static bool detectionPerformed;
        private static bool rimForgeDetected;
        private static ThingDef rimForgeAdamantium;

        private static readonly FieldInfo FuelIconPathField = AccessTools.Field(typeof(CompProperties_Refuelable), "fuelIconPath");
        private static readonly FieldInfo FuelIconTextureField = AccessTools.Field(typeof(CompProperties_Refuelable), "fuelIcon");

        public static bool RimForgeDetected
        {
            get
            {
                EnsureDetection();
                return rimForgeDetected;
            }
        }

        public static bool ShouldUseRimForge
        {
            get
            {
                EnsureDetection();
                return DeadpoolsHealingFactorMod.settings?.preferRimForgeAdamantium == true && rimForgeDetected && rimForgeAdamantium != null;
            }
        }

        public static ThingDef CurrentFuelDef
        {
            get
            {
                ThingDef fuel = ShouldUseRimForge ? rimForgeAdamantium : ThingDefOf.Plasteel;
                return fuel ?? ThingDefOf.Plasteel;
            }
        }

        public static string CurrentFuelLabel => CurrentFuelDef?.label ?? "fuel";
        public static string CurrentFuelLabelCap => CurrentFuelLabel.CapitalizeFirst();

        public static void Initialize()
        {
            EnsureDetection();
            ApplyInfusionFuelPreference();
        }

        public static void HandleSettingsChanged()
        {
            ApplyInfusionFuelPreference();
            RefreshSpawnedChambers();
        }

        public static void SyncRefuelComp(CompRefuelable comp)
        {
            if (comp == null)
            {
                return;
            }

            try
            {
                ThingDef targetFuel = CurrentFuelDef;
                UpdateFuelFilter(comp.Props?.fuelFilter, targetFuel);
                if (comp.Props != null)
                {
                    comp.Props.fuelLabel = CurrentFuelLabelCap;
                    SetIcon(comp.Props, targetFuel);
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DeadpoolsHealingFactor] Failed to sync infusion fuel filter: " + ex);
            }
        }

        private static void EnsureDetection()
        {
            if (detectionPerformed)
            {
                return;
            }

            detectionPerformed = true;
            try
            {
                rimForgeDetected = LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(mod?.PackageId, RimForgePackageId, StringComparison.OrdinalIgnoreCase));
                if (rimForgeDetected)
                {
                    rimForgeAdamantium = DefDatabase<ThingDef>.GetNamedSilentFail(RimForgeAdamantiumDefName);
                    if (rimForgeAdamantium == null)
                    {
                        Log.Warning("[DeadpoolsHealingFactor] RimForge detected but RF_Adamantium ThingDef was not found. Falling back to Plasteel.");
                        rimForgeDetected = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DeadpoolsHealingFactor] Failed to detect RimForge: " + ex);
                rimForgeDetected = false;
            }
        }

        private static void ApplyInfusionFuelPreference()
        {
            try
            {
                ThingDef chamberDef = DefDatabase<ThingDef>.GetNamedSilentFail("DP_AdamantiumChamber");
                if (chamberDef == null)
                {
                    return;
                }

                CompProperties_Refuelable refuelProps = chamberDef.comps?.OfType<CompProperties_Refuelable>().FirstOrDefault();
                if (refuelProps == null)
                {
                    return;
                }

                ThingDef targetFuel = CurrentFuelDef;
                UpdateFuelFilter(refuelProps.fuelFilter, targetFuel);
                refuelProps.fuelLabel = CurrentFuelLabelCap;
                SetIcon(refuelProps, targetFuel);
            }
            catch (Exception ex)
            {
                Log.Warning("[DeadpoolsHealingFactor] Failed to apply RimForge infusion fuel preference: " + ex);
            }
        }

        private static void UpdateFuelFilter(ThingFilter filter, ThingDef targetFuel)
        {
            if (filter == null || targetFuel == null)
            {
                return;
            }

            filter.SetDisallowAll();
            filter.SetAllow(targetFuel, true);
        }

        private static void SetIcon(CompProperties_Refuelable props, ThingDef targetFuel)
        {
            if (props == null || targetFuel == null)
            {
                return;
            }

            try
            {
                string iconPath = ResolveFuelIconPath(targetFuel);

                if (FuelIconPathField != null)
                {
                    FuelIconPathField.SetValue(props, iconPath);
                }
                else
                {
                    props.fuelIconPath = iconPath;
                }

                if (FuelIconTextureField != null)
                {
                    FuelIconTextureField.SetValue(props, targetFuel.uiIcon); // Align gizmo texture with current fuel
                }
            }
            catch
            {
                props.fuelIconPath = ResolveFuelIconPath(targetFuel);
                if (FuelIconTextureField != null)
                {
                    FuelIconTextureField.SetValue(props, targetFuel.uiIcon);
                }
            }
        }

        private static string ResolveFuelIconPath(ThingDef targetFuel)
        {
            if (targetFuel == null)
            {
                return string.Empty;
            }

            if (!targetFuel.uiIconPath.NullOrEmpty())
            {
                return targetFuel.uiIconPath;
            }

            if (targetFuel.graphicData != null && !targetFuel.graphicData.texPath.NullOrEmpty())
            {
                return targetFuel.graphicData.texPath;
            }

            return string.Empty;
        }

        private static void RefreshSpawnedChambers()
        {
            if (Current.ProgramState != ProgramState.Playing || Current.Game?.Maps == null)
            {
                return;
            }

            ThingDef chamberDef = DefDatabase<ThingDef>.GetNamedSilentFail("DP_AdamantiumChamber");
            if (chamberDef == null)
            {
                return;
            }

            try
            {
                foreach (Map map in Current.Game.Maps)
                {
                    foreach (var thing in map.listerThings.ThingsOfDef(chamberDef))
                    {
                        if (thing is Building_AdamantiumChamber chamber)
                        {
                            SyncRefuelComp(chamber.Refuel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DeadpoolsHealingFactor] Failed to refresh existing infusion chambers: " + ex);
            }
        }
    }
}
