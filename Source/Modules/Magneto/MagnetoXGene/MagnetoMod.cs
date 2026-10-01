using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MagnetoXGene
{
    public class MagnetoMod : Mod
    {
        public static MagnetoSettings Settings { get; private set; }

        public static MagnetoSettings ActiveSettings
        {
            get
            {
                if (Settings != null)
                {
                    return Settings;
                }

                MagnetoMod mod = LoadedModManager.GetMod<MagnetoMod>();
                if (mod != null)
                {
                    Settings = mod.GetSettings<MagnetoSettings>();
                    return Settings;
                }

                return Settings ??= new MagnetoSettings();
            }
        }

        public MagnetoMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<MagnetoSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Magneto X-Gene";
        }
    }

    public class MagnetoSettings : ModSettings
    {
        private const int OrbitMin = 1;
        private const int OrbitMax = 24;
        private const float RadiusMin = 3f;
        private const float RadiusMax = 30f;
        private const float DamageMin = 0.25f;
        private const float DamageMax = 5f;
        private const float OrbitSpeedMin = 1f;
        private const float OrbitSpeedMax = 24f;
        private const float OrbitDistanceMin = 0.5f;
        private const float OrbitDistanceMax = 3f;
        private const float LaunchCooldownMin = 1f;
        private const float LaunchCooldownMax = 15f;

        public int maxOrbitingItems = 6;
        public float magneticRadius = 12f;
        public float damageMultiplier = 1f;
        public float orbitSpeedDegreesPerTick = 6f;
        public float orbitDistanceMultiplier = 1f;
        public float launchCooldownSeconds = 5f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxOrbitingItems, "maxOrbitingItems", 6);
            Scribe_Values.Look(ref magneticRadius, "magneticRadius", 12f);
            Scribe_Values.Look(ref damageMultiplier, "damageMultiplier", 1f);
            Scribe_Values.Look(ref orbitSpeedDegreesPerTick, "orbitSpeedDegreesPerTick", 6f);
            Scribe_Values.Look(ref orbitDistanceMultiplier, "orbitDistanceMultiplier", 1f);
            Scribe_Values.Look(ref launchCooldownSeconds, "launchCooldownSeconds", 5f);
            maxOrbitingItems = Mathf.Clamp(maxOrbitingItems, OrbitMin, OrbitMax);
            magneticRadius = Mathf.Clamp(magneticRadius, RadiusMin, RadiusMax);
            damageMultiplier = Mathf.Clamp(damageMultiplier, DamageMin, DamageMax);
            orbitSpeedDegreesPerTick = Mathf.Clamp(orbitSpeedDegreesPerTick, OrbitSpeedMin, OrbitSpeedMax);
            orbitDistanceMultiplier = Mathf.Clamp(orbitDistanceMultiplier, OrbitDistanceMin, OrbitDistanceMax);
            launchCooldownSeconds = Mathf.Clamp(launchCooldownSeconds, LaunchCooldownMin, LaunchCooldownMax);
        }

        public void DoSettingsWindowContents(Rect rect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(rect);
            listing.Label("MagnetoSettings_MaxOrbit".Translate(maxOrbitingItems));
            maxOrbitingItems = Mathf.RoundToInt(listing.Slider(maxOrbitingItems, OrbitMin, OrbitMax));
            listing.Gap(6f);

            listing.Label("MagnetoSettings_MagneticRadius".Translate(magneticRadius.ToString("F1")));
            magneticRadius = listing.Slider(magneticRadius, RadiusMin, RadiusMax);
            listing.Gap(6f);

            listing.Label("MagnetoSettings_DamageMultiplier".Translate(damageMultiplier.ToString("F2")));
            damageMultiplier = listing.Slider(damageMultiplier, DamageMin, DamageMax);
            listing.Gap(6f);

            listing.Label("MagnetoSettings_OrbitSpeed".Translate(orbitSpeedDegreesPerTick.ToString("F1")));
            orbitSpeedDegreesPerTick = listing.Slider(orbitSpeedDegreesPerTick, OrbitSpeedMin, OrbitSpeedMax);
            listing.Gap(6f);

            listing.Label("MagnetoSettings_OrbitDistance".Translate(orbitDistanceMultiplier.ToString("F2")));
            orbitDistanceMultiplier = listing.Slider(orbitDistanceMultiplier, OrbitDistanceMin, OrbitDistanceMax);
            listing.Gap(6f);

            listing.Label("MagnetoSettings_LaunchCooldown".Translate(launchCooldownSeconds.ToString("F1")));
            launchCooldownSeconds = listing.Slider(launchCooldownSeconds, LaunchCooldownMin, LaunchCooldownMax);
            listing.Gap(6f);
            listing.End();
        }
    }

    [DefOf]
    public static class MagnetoDefOf
    {
        public static HediffDef Magneto_Magnetism;
        public static ThingDef Magneto_MagnetoInjector;
        public static ThingDef Magneto_ProjectileOrbitItem;
        public static ThingDef Magneto_OrbitingVisual;

        static MagnetoDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(MagnetoDefOf));
        }
    }

    public static class MagnetoUtility
    {
        private static readonly HashSet<string> MetallicDefNames = new HashSet<string>
        {
            "Steel",
            "Plasteel",
            "Gold",
            "Uranium",
            "ComponentIndustrial",
            "ComponentSpacer",
            "SteelSlagChunk",
            "ChunkSlagSteel",
            "ChunkSteel"
        };

        public static bool IsMetallic(Thing thing)
        {
            if (thing == null || thing.Destroyed)
            {
                return false;
            }

            if (thing is Pawn)
            {
                return false;
            }

            if (thing.def == null)
            {
                return false;
            }

            if (thing.def.thingCategories != null)
            {
                if (thing.def.thingCategories.Contains(ThingCategoryDefOf.Weapons) && thing.Stuff != null)
                {
                    return IsMetallicStuff(thing.Stuff);
                }

                if (thing.def.thingCategories.Contains(ThingCategoryDefOf.Chunks))
                {
                    return true;
                }
            }

            if (thing.def.IsMetal || thing.def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true)
            {
                return true;
            }

            if (thing.def.IsWeapon)
            {
                if (thing.Stuff != null && IsMetallicStuff(thing.Stuff))
                {
                    return true;
                }

                if (thing.def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true)
                {
                    return true;
                }

                if (thing.def.costList != null && thing.def.costList.Any(entry => entry != null && IsMetallicThingDef(entry.thingDef)))
                {
                    return true;
                }
            }

            if (thing.Stuff != null && IsMetallicStuff(thing.Stuff))
            {
                return true;
            }

            if (thing.TryGetInnerInteractableThingOwner() != null)
            {
                return false;
            }

            if (thing.def.IsStuff && IsMetallicStuff(thing.def))
            {
                return true;
            }

            return MetallicDefNames.Contains(thing.def.defName);
        }

        private static bool IsMetallicStuff(ThingDef stuff)
        {
            if (stuff == null)
            {
                return false;
            }

            if (stuff.stuffProps?.categories?.Contains(StuffCategoryDefOf.Metallic) == true)
            {
                return true;
            }

            return MetallicDefNames.Contains(stuff.defName);
        }

        private static bool IsMetallicThingDef(ThingDef def)
        {
            if (def == null)
            {
                return false;
            }

            if (def.stuffCategories?.Contains(StuffCategoryDefOf.Metallic) == true)
            {
                return true;
            }

            if (def.IsMetal)
            {
                return true;
            }

            return MetallicDefNames.Contains(def.defName);
        }

        public static float ComputePayloadDamage(Thing payload)
        {
            if (payload == null)
            {
                return 0f;
            }

            float mass = Mathf.Max(payload.GetStatValue(StatDefOf.Mass, true), 0.2f);
            float baseDamage = 6f + mass * 10f;
            ThingDef stuff = payload.Stuff ?? payload.def;
            float materialFactor = 1f;

            if (stuff != null)
            {
                if (stuff == ThingDefOf.Steel)
                {
                    materialFactor = 1.0f;
                }
                else if (stuff == ThingDefOf.Plasteel)
                {
                    materialFactor = 1.3f;
                }
                else if (stuff == ThingDefOf.Uranium)
                {
                    materialFactor = 1.45f;
                }
                else if (stuff == ThingDefOf.Gold)
                {
                    materialFactor = 0.8f;
                }
                else if (stuff == ThingDefOf.Silver)
                {
                    materialFactor = 0.9f;
                }
                else if (stuff == ThingDefOf.ComponentIndustrial || stuff == ThingDefOf.ComponentSpacer)
                {
                    materialFactor = 1.1f;
                }
            }

            return baseDamage * materialFactor * MagnetoMod.ActiveSettings.damageMultiplier;
        }

        public static bool CanLiftMech(Pawn mech)
        {
            if (mech?.RaceProps == null || !mech.RaceProps.IsMechanoid)
            {
                return false;
            }

            return mech.BodySize <= 2.5f;
        }
    }
}
