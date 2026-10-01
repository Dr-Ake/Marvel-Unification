using System;
using System.Reflection;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;

namespace DeadpoolsHealingFactor
{
    [DefOf]
    public static class DPDefOf
    {
        public static HediffDef DP_HealingFactor;
        public static HediffDef DP_regrowing;
        public static HediffDef DP_adjusting;
        public static HediffDef DP_AdamantiumSkeleton;
        public static HediffDef DP_WolverineClaws;
        public static ThingDef DP_WolverineClawWeapon;
        public static JobDef DP_UseAdamantiumChamber;
        public static JobDef DP_UseAdamantiumChamberWithClaws;
        public static SoundDef DP_WolverineClawExtend;

        static DPDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DPDefOf));
        }
    }
    [StaticConstructorOnStartup]
    public static class DeadpoolsHealingFactor
    {
        static DeadpoolsHealingFactor()
        {
            MarvelUnification.MarvelHarmony.EnsurePatched();
            Log.Message("[DeadpoolsHealingFactor] Initialized");
		try
		{
			DPClawSettingsUtility.Apply();
		}
		catch (Exception ex)
		{
			Log.Warning("[DeadpoolsHealingFactor] Failed to apply claw settings on startup: " + ex);
		}
		try
		{
			DPRimForgeIntegration.Initialize();
		}
		catch (Exception ex)
		{
			Log.Warning("[DeadpoolsHealingFactor] Failed to initialize RimForge integration: " + ex);
		}
		try
		{
			DPRecipeUtility.EnsureInjectorRecipeOnAnimals();
		}
		catch (Exception ex)
		{
			Log.Warning("[DeadpoolsHealingFactor] Failed to register injector recipe for animals: " + ex);
		}
        }
    }

    public static class DPRecipeUtility
    {
        private static readonly FieldInfo AllRecipesCachedField = AccessTools.Field(typeof(ThingDef), "allRecipesCached");

        public static void EnsureInjectorRecipeOnAnimals()
        {
            RecipeDef recipe = DefDatabase<RecipeDef>.GetNamedSilentFail("DP_AdministerHealingInjector");
            if (recipe == null)
            {
                return;
            }

            List<ThingDef> things = DefDatabase<ThingDef>.AllDefsListForReading;
            foreach (ThingDef thing in things)
            {
                if (thing?.race == null || !thing.race.Animal)
                {
                    continue;
                }

                if (thing.recipes == null)
                {
                    thing.recipes = new List<RecipeDef>();
                }

                if (!thing.recipes.Contains(recipe))
                {
                    thing.recipes.Add(recipe);
                    AllRecipesCachedField?.SetValue(thing, null);
                }
            }
        }
    }

    public static class DPHealingUtility
    {
        public static string GetAutomaticSkipReasonForDef(HediffDef def)
        {
            try
            {
                if (def == null)
                {
                    return "invalid hediff";
                }

                if (def == DPDefOf.DP_HealingFactor || def == DPDefOf.DP_adjusting || def == DPDefOf.DP_regrowing)
                {
                    return "internal Deadpool healing hediff";
                }

                Type hediffClass = def.hediffClass;
                if (hediffClass != null)
                {
                    if (typeof(Hediff_MissingPart).IsAssignableFrom(hediffClass))
                    {
                        return "handled by regrowth";
                    }
                    if (typeof(Hediff_AddedPart).IsAssignableFrom(hediffClass))
                    {
                        return "prosthetics and implants are preserved";
                    }
                    if (typeof(Hediff_Injury).IsAssignableFrom(hediffClass))
                    {
                        return null;
                    }
                }

                if (!ShouldCureDefWithHealingFactor(def))
                {
                    return "not an injury or disease-like hediff";
                }
            }
            catch { }

            return null;
        }

        public static bool ShouldCureDefWithHealingFactor(HediffDef def)
        {
            try
            {
                if (def == null || !def.isBad)
                {
                    return false;
                }

                // DiseaseBase/InfectionBase/AddictionBase descendants inherit these markers.
                if (def.isInfection
                    || def.chronic
                    || def.tendable
                    || def.makesSickThought
                    || def.everCurableByItem
                    || def.removeOnRedressChanceByDaysCurve != null
                    || def.chemicalNeed != null)
                {
                    return true;
                }

                if (def.comps != null && def.comps.Any(comp => comp?.compClass != null && typeof(HediffComp_Immunizable).IsAssignableFrom(comp.compClass)))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        // Return true for hediffs this mod should never touch.
        public static bool ShouldSkipForHealing(Hediff h)
        {
            try
            {
                HediffDef def = h?.def;
                if (def == null)
                {
                    return false;
                }
                if (DeadpoolsHealingFactorMod.settings?.IsExcludedFromHealing(def) == true)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public static bool ShouldCureWithHealingFactor(Hediff h)
        {
            try
            {
                HediffDef def = h?.def;
                if (!ShouldCureDefWithHealingFactor(def))
                {
                    return h?.TryGetComp<HediffComp_Immunizable>() != null;
                }

                return true;
            }
            catch { }
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_HealthTracker), "HealthTick")]
    public static class Patch_Pawn_HealthTracker_HealthTick
    {
        // default values are kept in Settings.cs
        private static readonly PropertyInfo NeedCurLevelProperty = AccessTools.Property(typeof(Need), "CurLevel");

        public static void Postfix(Pawn_HealthTracker __instance, Pawn ___pawn)
        {
            if (___pawn == null)
            {
                return;
            }

            DeadpoolsHealingFactorSettings settings = DeadpoolsHealingFactorMod.settings;
            if (settings == null)
            {
                return;
            }

            bool doHealing = settings.enableHealing;
            bool doRegrow = settings.enableRegrowth;
            bool doMood = settings.boostMood;
            bool doPsychopath = settings.forcePsychopath;
            if (!doHealing && !doRegrow && !doMood && !doPsychopath)
            {
                return;
            }

            int interval = settings.ticksBetweenHeals;
            if (interval <= 0)
            {
                interval = 250;
            }
            if (!___pawn.IsHashIntervalTick(interval))
            {
                return;
            }

            // Do not heal or regrow while burning
            try
            {
                if (___pawn.Spawned && FireUtility.IsBurning(___pawn))
                {
                    return;
                }
            }
            catch { }

            if (___pawn.health?.hediffSet?.HasHediff(DPDefOf.DP_HealingFactor) != true)
            {
                return;
            }

            // Keep mood maxed (via reflection to handle non-public setters across versions) and ensure the pawn is a psychopath
            if (doMood && ___pawn.needs?.mood != null)
            {
                try
                {
                    NeedCurLevelProperty?.SetValue(___pawn.needs.mood, 1f, null);
                }
                catch
                {
                    // Ignore if API changes; mood boost is optional
                }
            }
            if (doPsychopath
                && ___pawn.RaceProps?.Humanlike == true
                && ___pawn.story?.traits != null
                && !___pawn.story.traits.HasTrait(TraitDefOf.Psychopath))
            {
                ___pawn.story.traits.GainTrait(new Trait(TraitDefOf.Psychopath));
            }

            float severityFactor = 1f; // always full effect
            float healAmount = settings.baseHealAmount * severityFactor;
            List<Hediff> regrowingHediffs = null;
            List<Hediff> allHediffs = ___pawn.health.hediffSet.hediffs;

            if (doHealing)
            {
                // Heal or remove injuries and bad hediffs (including diseases and scars)
                for (int i = allHediffs.Count - 1; i >= 0; i--)
                {
                    Hediff h = allHediffs[i];
                    if (h == null) continue;
                    HediffDef def = h.def;
                    if (def == DPDefOf.DP_HealingFactor || def == DPDefOf.DP_adjusting)
                    {
                        continue;
                    }
                    if (def == DPDefOf.DP_regrowing)
                    {
                        if (doRegrow)
                        {
                            if (regrowingHediffs == null)
                            {
                                regrowingHediffs = new List<Hediff>();
                            }
                            regrowingHediffs.Add(h);
                        }
                        continue;
                    }
                    // Let regrowth system handle missing parts
                    if (h is Hediff_MissingPart) continue;
                    // Never remove added parts (prosthetics/bionics)
                    if (h is Hediff_AddedPart) continue;

                    // Preserve excluded hediffs and built-in skips.
                    if (DPHealingUtility.ShouldSkipForHealing(h))
                    {
                        continue;
                    }

                    if (h is Hediff_Injury inj)
                    {
                        if (inj.IsPermanent())
                        {
                            if (settings.removeScars)
                            {
                                ___pawn.health.RemoveHediff(inj); // treat scars as removable
                            }
                        }
                        else if (inj.Severity > 0 && inj.CanHealNaturally())
                        {
                            inj.Heal(healAmount);
                        }
                        continue;
                    }

                    // Only cure disease-like conditions; preserve ritual/status hediffs from other mods.
                    if (settings.cureDiseases && DPHealingUtility.ShouldCureWithHealingFactor(h))
                    {
                        if (h.Severity > 0f)
                        {
                            h.Severity = (float)Math.Max(0f, h.Severity - healAmount);
                        }
                        // Ensure eventual cure even if severity is sticky
                        ___pawn.health.RemoveHediff(h);
                    }
                }
            }

            // ---------- Limb regrowth logic ----------
            if (doRegrow)
            {
                HediffDef regrowingDef = DPDefOf.DP_regrowing;
                if (regrowingHediffs == null)
                {
                    for (int i = 0; i < allHediffs.Count; i++)
                    {
                        Hediff h = allHediffs[i];
                        if (h?.def == regrowingDef)
                        {
                            if (regrowingHediffs == null)
                            {
                                regrowingHediffs = new List<Hediff>();
                            }
                            regrowingHediffs.Add(h);
                        }
                    }
                }

                if (regrowingHediffs != null)
                {
                    foreach (var regrow in regrowingHediffs)
                    {
                        regrow.Severity += settings.regrowSpeed * severityFactor;
                        if (regrow.Severity >= 1.0f)
                        {
                            // Restore the first missing child of the regrow anchor part (parent)
                            BodyPartRecord partToRestore = ___pawn.health.hediffSet.GetMissingPartsCommonAncestors()
                                .Select(mp => mp.Part)
                                .FirstOrDefault(p => p?.parent == regrow.Part) ?? regrow.Part;
                            ___pawn.health.RemoveHediff(regrow);
                            ___pawn.health.RestorePart(partToRestore);
                            // Apply a short-lived adjustment debuff to the restored part to simulate recovery
                            ___pawn.health.AddHediff(DPDefOf.DP_adjusting, partToRestore);
                            if (Prefs.DevMode)
                            {
                                Log.Message($"[DeadpoolsHealingFactor] Restored {partToRestore.Label} on {___pawn.LabelShort}.");
                            }
                        }
                    }
                }

                int regrowingCount = regrowingHediffs?.Count ?? 0;
                if (regrowingCount < settings.maxRegrowingParts)
                {
                    var missingParts = ___pawn.health.hediffSet.GetMissingPartsCommonAncestors();
                    foreach (var missing in missingParts)
                    {
                        BodyPartRecord parent = missing.Part?.parent; // anchor on parent, restore child at completion
                        if (parent != null && !___pawn.health.hediffSet.hediffs.Any(h => h.def == regrowingDef && h.Part == parent))
                        {
                            ___pawn.health.AddHediff(regrowingDef, parent);
                            if (Prefs.DevMode)
                            {
                                Log.Message($"[DeadpoolsHealingFactor] Started regrowing a child of {parent.Label} on {___pawn.LabelShort}.");
                            }
                            break;
                        }
                    }
                }
            }
        }
    }

    // Continue healing/regrowth while dead, and resurrect when fully restored
    [HarmonyPatch(typeof(Corpse), "TickRare")]
    public static class Patch_Corpse_TickRare
    {
        public static void Postfix(Corpse __instance)
        {
            if (__instance == null)
            {
                return;
            }

            Pawn pawn = __instance.InnerPawn;
            if (pawn == null)
            {
                return;
            }

            // (No special rot handling for adamantium skeleton here)

            // No healing/regrowth while the corpse is burning
            try
            {
                if (FireUtility.IsBurning(__instance))
                {
                    return;
                }
            }
            catch { }

            // Must have the hediff to process
            Hediff factor = pawn.health?.hediffSet?.GetFirstHediffOfDef(DPDefOf.DP_HealingFactor);
            if (factor == null)
            {
                return;
            }

            int interval = DeadpoolsHealingFactorMod.settings.ticksBetweenHeals;
            if (interval <= 0)
            {
                interval = 250;
            }
            // Corpse.TickRare runs every 250 ticks. Always process here and scale by step factor
            float stepFactor = 250f / Math.Max(1, interval);

            float severityFactor = factor.Severity <= 0f ? 1f : factor.Severity;

            if (DeadpoolsHealingFactorMod.settings.enableHealing)
            {
                float healAmount = DeadpoolsHealingFactorMod.settings.baseHealAmount * severityFactor * stepFactor;
                List<Hediff> all = pawn.health.hediffSet.hediffs;
                List<Hediff> toRemove = new List<Hediff>();
                foreach (var h in all)
                {
                    if (h == null) continue;
                    if (h.def == DPDefOf.DP_HealingFactor || h.def == DPDefOf.DP_regrowing || h.def == DPDefOf.DP_adjusting) continue;
                    if (h is Hediff_MissingPart) continue;
                    if (h is Hediff_AddedPart) continue;

                    // Preserve excluded hediffs and built-in skips.
                    if (DPHealingUtility.ShouldSkipForHealing(h))
                    {
                        continue;
                    }

                    if (h is Hediff_Injury inj)
                    {
                        if (inj.IsPermanent())
                        {
                            if (DeadpoolsHealingFactorMod.settings.removeScars)
                            {
                                toRemove.Add(inj);
                            }
                        }
                        else if (inj.Severity > 0)
                        {
                            inj.Severity = (float)Math.Max(0f, inj.Severity - healAmount);
                            if (inj.Severity <= 0f)
                            {
                                toRemove.Add(inj);
                            }
                        }
                        continue;
                    }

                    if (DeadpoolsHealingFactorMod.settings.cureDiseases && DPHealingUtility.ShouldCureWithHealingFactor(h))
                    {
                        if (h.Severity > 0f)
                        {
                            h.Severity = (float)Math.Max(0f, h.Severity - healAmount);
                        }
                        // Ensure eventual cure even if severity is sticky
                        toRemove.Add(h);
                    }
                }
                foreach (var h in toRemove)
                {
                    pawn.health.RemoveHediff(h);
                }
            }

            if (DeadpoolsHealingFactorMod.settings.enableRegrowth)
            {
                HediffDef regrowingDef = DPDefOf.DP_regrowing;
                var regrowingHediffs = pawn.health.hediffSet.hediffs
                    .Where(h => h.def == regrowingDef)
                    .ToList();

                foreach (var regrow in regrowingHediffs)
                {
                    regrow.Severity += DeadpoolsHealingFactorMod.settings.regrowSpeed * severityFactor * stepFactor;
                    if (regrow.Severity >= 1.0f)
                    {
                        BodyPartRecord partToRestore = pawn.health.hediffSet.GetMissingPartsCommonAncestors()
                            .Select(mp => mp.Part)
                            .FirstOrDefault(p => p?.parent == regrow.Part) ?? regrow.Part;
                        pawn.health.RemoveHediff(regrow);
                        pawn.health.RestorePart(partToRestore);
                        pawn.health.AddHediff(DPDefOf.DP_adjusting, partToRestore);
                    }
                }

                if (regrowingHediffs.Count < DeadpoolsHealingFactorMod.settings.maxRegrowingParts)
                {
                    var missingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors();
                    foreach (var missing in missingParts)
                    {
                        BodyPartRecord parent = missing.Part?.parent;
                        if (parent != null && !pawn.health.hediffSet.hediffs.Any(h => h.def == regrowingDef && h.Part == parent))
                        {
                            pawn.health.AddHediff(regrowingDef, parent);
                            break;
                        }
                    }
                }
            }

            // If fully healed (no non-permanent injuries and no missing parts), resurrect
            bool hasOpenInjury = pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Any(h => !h.IsPermanent() && h.Severity > 0);

            bool hasMissingParts = pawn.health.hediffSet.GetMissingPartsCommonAncestors().Any();

            // Handle rot/skeleton visuals for adamantium/factor
            bool rotten = __instance.GetRotStage() != RotStage.Fresh;
            bool hasHealingFactor = pawn.health.hediffSet.HasHediff(DPDefOf.DP_HealingFactor);
            bool hasAdamantium = pawn.health.hediffSet.HasHediff(DPDefOf.DP_AdamantiumSkeleton);
            if (hasHealingFactor || hasAdamantium)
            {
                // Always keep corpse fresh to prevent deletion; we handle visuals via healing logic
                try
                {
                    var rot = __instance.TryGetComp<CompRottable>();
                    if (rot != null)
                    {
                        rot.RotProgress = 0;
                        rotten = false;
                    }
                }
                catch { }
                // Prevent deterioration from destroying the corpse while waiting to revive
                try
                {
                    if (__instance.def.useHitPoints)
                    {
                        bool burning = false;
                        try { burning = FireUtility.IsBurning(__instance); } catch { }

                        if (!burning && hasHealingFactor && __instance.HitPoints < __instance.MaxHitPoints)
                        {
                            __instance.HitPoints = __instance.MaxHitPoints;
                        }
                        else if (burning && hasAdamantium)
                        {
                            if (__instance.HitPoints < 1)
                            {
                                __instance.HitPoints = 1;
                            }
                            if (__instance.HitPoints <= 1 && __instance.Spawned)
                            {
                                try
                                {
                                    var fire = __instance.GetAttachment(ThingDefOf.Fire);
                                    fire?.Destroy(DestroyMode.Vanish);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            // Stop rotting entirely for pawns with healing factor or adamantium skeleton
            try
            {
                if (rotten && (hasHealingFactor || hasAdamantium))
                {
                    var rot = __instance.TryGetComp<CompRottable>();
                    if (rot != null)
                    {
                        rot.RotProgress = 0;
                        rotten = false;
                    }
                }
            }
            catch { }

            if (DeadpoolsHealingFactorMod.settings.autoResurrection && !hasOpenInjury && !hasMissingParts)
            {
                try
                {
                    // Ensure safe resurrection: temporarily freshen if needed
                    var rot = __instance.TryGetComp<CompRottable>();
                    float? prev = null;
                    try { if (rot != null) { prev = rot.RotProgress; rot.RotProgress = 0; } } catch { }
                    ResurrectionUtility.TryResurrect(pawn);
                    try { if (rot != null && prev.HasValue) { rot.RotProgress = prev.Value; } } catch { }
                    if (!pawn.Dead)
                    {
                        // Ensure no lingering fire and visuals are restored
                        try
                        {
                            if (pawn.Spawned)
                            {
                                var fire = pawn.Position.GetFirstThing(pawn.Map, ThingDefOf.Fire);
                                if (fire != null) fire.Destroy(DestroyMode.Vanish);
                            }
                            pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                        }
                        catch { }
                    }
                }
                catch
                {
                    // ignore if API differs; resurrection is best-effort
                }
            }
        }
    }

}
