using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;


namespace MultipleManXGene
{
    [StaticConstructorOnStartup]
    public static class DupeUtility
    {
        private static PawnKindDef? cachedKind;

        private static readonly Texture2D SummonGizmoIcon = LoadIcon("UI/Commands/CommandSummonDupe", TexCommand.Attack);
        private static readonly Texture2D DismissGizmoIcon = LoadIcon("UI/Commands/CommandDismissDupe", TexCommand.DesirePower);
        private static readonly Texture2D DismissSelfGizmoIcon = LoadIcon("UI/Commands/CommandDismissDupeSelf", TexCommand.DesirePower);
        private static readonly FieldInfo StoryMelaninField = AccessTools.Field(typeof(Pawn_StoryTracker), "melanin");
        private static readonly FieldInfo GeneOverriddenField = AccessTools.Field(typeof(Gene), "overriddenByGene");
        private static readonly FieldInfo GeneTrackerXenotypeField = AccessTools.Field(typeof(Pawn_GeneTracker), "xenotype");
        private static readonly FieldInfo GeneTrackerXenotypeNameField = AccessTools.Field(typeof(Pawn_GeneTracker), "xenotypeName");
        private static readonly FieldInfo GeneTrackerIconField = AccessTools.Field(typeof(Pawn_GeneTracker), "iconDef");
        private static readonly FieldInfo GeneTrackerHybridField = AccessTools.Field(typeof(Pawn_GeneTracker), "hybrid");
        private static readonly FieldInfo GeneTrackerCustomXenoField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedCustomXenotype");
        private static readonly FieldInfo GeneTrackerHasCustomXenoField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedHasCustomXenotype");
        private static readonly FieldInfo GeneTrackerCachedGenesField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedGenes");
        private static readonly FieldInfo GeneTrackerCachedDamageField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedDamageFactors");
        private static readonly FieldInfo GeneTrackerCachedAddictionField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedAddictionChanceFactors");
        private static readonly FieldInfo GeneTrackerCachedEnabledNeedsField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedEnabledNeeds");
        private static readonly FieldInfo GeneTrackerCachedDisabledNeedsField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedDisabledNeeds");
        private static readonly FieldInfo GeneTrackerCachedGenesAffectAgeField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedGenesAffectAge");
        private static readonly FieldInfo GeneTrackerCachedTattoosField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedTattoosVisible");
        private static readonly FieldInfo GeneTrackerCachedWaterCostField = AccessTools.Field(typeof(Pawn_GeneTracker), "cachedWaterCellCost");
        private static readonly FieldInfo GeneTrackerHasCachedWaterField = AccessTools.Field(typeof(Pawn_GeneTracker), "hasCachedWaterCost");
        private static readonly MethodInfo GeneTrackerAddGeneMethod = AccessTools.Method(typeof(Pawn_GeneTracker), "AddGene", new[] { typeof(Gene), typeof(bool) });
        private static readonly MethodInfo GeneTrackerRecacheNeeds = AccessTools.Method(typeof(Pawn_GeneTracker), "RecacheNeeds");
        private static readonly MethodInfo GeneTrackerEnsureSkinColor = AccessTools.Method(typeof(Pawn_GeneTracker), "EnsureCorrectSkinColorOverride");

        private static PawnKindDef DupeKind => cachedKind ??= DefDatabase<PawnKindDef>.GetNamedSilentFail("DupePawnKind") ?? PawnKindDefOf.Colonist;

        public static bool IsDupe(this Pawn pawn)
        {
            if (pawn == null || Verse.Current.Game == null)
            {
                return false;
            }

            return DupeManager.Current.IsRegisteredDupe(pawn);
        }

        public static Command_Action? BuildSummonGizmo(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Faction != Faction.OfPlayer || pawn.IsDupe())
            {
                return null;
            }

            if (!PawnHasDupeAbility(pawn))
            {
                return null;
            }

            var command = new Command_Action
            {
                defaultLabel = "Summon dupe",
                defaultDesc = "Create a temporary dupe of this pawn",
                icon = SummonGizmoIcon,
                action = () => TrySummonDupe(pawn)
            };

            if (!DupeManager.Current.CanSummon(pawn, out var reason))
            {
                command.Disable(reason);
            }

            return command;
        }

        public static Command_Action? BuildDismissGizmo(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.IsDupe() || pawn.Faction != Faction.OfPlayer)
            {
                return null;
            }

            if (!PawnHasDupeAbility(pawn))
            {
                return null;
            }

            if (!DupeManager.Current.HasActiveDupe(pawn))
            {
                return null;
            }

            var command = new Command_Action
            {
                defaultLabel = "Dismiss oldest dupe",
                defaultDesc = "Immediately remove the oldest active dupe belonging to this pawn.",
                icon = DismissGizmoIcon,
                action = () =>
                {
                    if (!DupeManager.Current.TryDismissOldestDupe(pawn))
                    {
                        Messages.Message("No dupes available to dismiss.", pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }
                }
            };

            return command;
        }

        public static Command_Action? BuildSelfDismissGizmo(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.IsDupe() || pawn.Faction != Faction.OfPlayer)
            {
                return null;
            }

            if (!DupeManager.Current.TryGetData(pawn, out var data) || data == null)
            {
                return null;
            }

            var command = new Command_Action
            {
                defaultLabel = "Dismiss dupe",
                defaultDesc = "Immediately remove this dupe.",
                icon = DismissSelfGizmoIcon,
                action = () => DupeManager.Current.ForceDespawn(data, DupeDespawnReason.Manual)
            };

            return command;
        }

        public static void TrySummonDupe(Pawn pawn)
        {
            if (!DupeManager.Current.CanSummon(pawn, out var reason))
            {
                Messages.Message(reason ?? "Cannot summon dupe", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            var dupe = GenerateDupePawn(pawn);
            if (dupe == null)
            {
                Messages.Message("Failed to generate dupe pawn", pawn, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }

            var map = pawn.MapHeld;
            var cell = CellFinder.RandomClosewalkCellNear(pawn.PositionHeld, map, 2);
            GenSpawn.Spawn(dupe, cell, map, Rot4.Random);
            var data = DupeManager.Current.RegisterDupe(pawn, dupe);
            CopyFromOriginal(pawn, dupe, data);
        }

        private static Pawn GenerateDupePawn(Pawn original)
        {
            var request = new PawnGenerationRequest(
                kind: DupeKind,
                faction: original.Faction,
                context: PawnGenerationContext.PlayerStarter,
                tile: original.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                mustBeCapableOfViolence: !original.WorkTagIsDisabled(WorkTags.Violent),
                fixedBiologicalAge: original.ageTracker.AgeBiologicalYearsFloat,
                fixedChronologicalAge: original.ageTracker.AgeChronologicalYearsFloat,
                fixedGender: original.gender,
                fixedIdeo: original.Ideo);
            return PawnGenerator.GeneratePawn(request);
        }

        private static void CopyFromOriginal(Pawn original, Pawn dupe, DupeLifecycleData data)
        {
            CopyGenes(original, dupe);
            CopyIdentity(original, dupe);
            CopySkills(original, dupe);
            CopyTraits(original, dupe);
            CopyWorkPriorities(original, dupe);
            CopyPlayerSettings(original, dupe);
            CopyDraftStatus(original, dupe);
            CopyEquipment(original, dupe, data);
            CopyHealthState(original, dupe);
        }

        private static void CopyPlayerSettings(Pawn original, Pawn dupe)
        {
            if (original.playerSettings == null || dupe.playerSettings == null)
            {
                return;
            }

            dupe.playerSettings.hostilityResponse = original.playerSettings.hostilityResponse;
        }

        private static void CopyDraftStatus(Pawn original, Pawn dupe)
        {
            if (!DupeSettingsManager.DraftDupesWhenOriginalDrafted)
            {
                return;
            }

            if (original?.drafter?.Drafted == true && dupe?.drafter != null)
            {
                dupe.drafter.Drafted = true;
            }
        }

        private static void CopyIdentity(Pawn original, Pawn dupe)
        {
            if (original.story != null && dupe.story != null)
            {
                dupe.story.bodyType = original.story.bodyType;
                dupe.story.headType = original.story.headType;
                dupe.story.hairDef = original.story.hairDef;
                dupe.story.HairColor = original.story.HairColor;
                dupe.story.SkinColorBase = original.story.SkinColorBase;
                dupe.story.skinColorOverride = original.story.skinColorOverride;
                dupe.story.Childhood = original.story.Childhood;
                dupe.story.Adulthood = original.story.Adulthood;
                dupe.story.favoriteColor = original.story.favoriteColor;
                dupe.story.furDef = original.story.furDef;
                CopyStoryMelanin(original.story, dupe.story);
            }

            if (original.style != null && dupe.style != null)
            {
                dupe.style.beardDef = original.style.beardDef;
                dupe.style.FaceTattoo = original.style.FaceTattoo;
                dupe.style.BodyTattoo = original.style.BodyTattoo;
            }

            if (original.Name is NameTriple triple)
            {
                dupe.Name = new NameTriple(triple.First, $"{triple.Nick} (Dupe)", triple.Last);
            }
            else if (original.Name != null)
            {
                dupe.Name = new NameSingle(original.Name.ToStringFull + " (Dupe)");
            }
        }

        private static void CopySkills(Pawn original, Pawn dupe)
        {
            if (original.skills == null || dupe.skills == null)
            {
                return;
            }

            foreach (var skill in dupe.skills.skills)
            {
                var source = original.skills.GetSkill(skill.def);
                skill.Level = source.Level;
                skill.passion = source.passion;
                skill.xpSinceLastLevel = source.xpSinceLastLevel;
            }
        }

        private static void CopyTraits(Pawn original, Pawn dupe)
        {
            if (original.story?.traits == null || dupe.story?.traits == null)
            {
                return;
            }

            dupe.story.traits.allTraits.Clear();
            foreach (var trait in original.story.traits.allTraits)
            {
                dupe.story.traits.GainTrait(new Trait(trait.def, trait.Degree, forced: true));
            }
        }

        private static void CopyWorkPriorities(Pawn original, Pawn dupe)
        {
            if (original.workSettings == null)
            {
                return;
            }

            dupe.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            original.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (workType == WorkTypeDefOf.Research && !DupeSettingsManager.ResearchAllowed)
                {
                    dupe.workSettings.SetPriority(workType, 0);
                    continue;
                }

                if (original.WorkTypeIsDisabled(workType) || dupe.WorkTypeIsDisabled(workType))
                {
                    dupe.workSettings.SetPriority(workType, 0);
                    continue;
                }

                dupe.workSettings.SetPriority(workType, original.workSettings.GetPriority(workType));
            }
        }

        private static void CopyEquipment(Pawn original, Pawn dupe, DupeLifecycleData data)
        {
            ClearGeneratedLoadout(dupe);
            if (original.equipment?.Primary is ThingWithComps primary)
            {
                var weapon = (ThingWithComps)ThingMaker.MakeThing(primary.def, primary.Stuff);
                CopyThingQuality(primary, weapon);
                dupe.equipment?.AddEquipment(weapon);
                data.RegisterPhantomThing(weapon);
            }

            if (original.apparel != null && dupe.apparel != null)
            {
                var buffer = new List<Apparel>(original.apparel.WornApparel);
                foreach (var apparel in buffer)
                {
                    var copy = (Apparel)ThingMaker.MakeThing(apparel.def, apparel.Stuff);
                    copy.SetColor(apparel.DrawColor, true);
                    CopyThingQuality(apparel, copy);
                    dupe.apparel.Wear(copy, dropReplacedApparel: false);
                    data.RegisterPhantomThing(copy);
                }
            }
        }

        private static void CopyThingQuality(Thing source, Thing target)
        {
            var sourceQuality = source.TryGetComp<CompQuality>();
            var targetQuality = target.TryGetComp<CompQuality>();
            if (sourceQuality != null && targetQuality != null)
            {
                targetQuality.SetQuality(sourceQuality.Quality, ArtGenerationContext.Outsider);
            }
        }

        private static void ClearGeneratedLoadout(Pawn pawn)
        {
            pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.apparel?.DestroyAll(DestroyMode.Vanish);
            pawn.inventory?.innerContainer?.ClearAndDestroyContents(DestroyMode.Vanish);
        }

        private static void CopyGenes(Pawn original, Pawn dupe)
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            if (original.genes == null || dupe.genes == null)
            {
                return;
            }

            var dupeTracker = dupe.genes;
            var originalTracker = original.genes;

            var toRemove = dupeTracker.GenesListForReading.ToList();
            foreach (var gene in toRemove)
            {
                dupeTracker.RemoveGene(gene);
            }

            var geneMap = new Dictionary<Gene, Gene>();
            CloneGeneSet(originalTracker.Endogenes, dupeTracker, dupe, xenogene: false, geneMap);
            CloneGeneSet(originalTracker.Xenogenes, dupeTracker, dupe, xenogene: true, geneMap);
            RemapGeneOverrides(geneMap);
            CopyGeneTrackerState(originalTracker, dupeTracker);
            GeneTrackerRecacheNeeds?.Invoke(dupeTracker, Array.Empty<object>());
            GeneTrackerEnsureSkinColor?.Invoke(dupeTracker, Array.Empty<object>());
        }

        private static void CopyHealthState(Pawn original, Pawn dupe)
        {
            if (original.health == null || dupe.health == null)
            {
                return;
            }

            var existing = dupe.health.hediffSet.hediffs.ToList();
            foreach (var hediff in existing)
            {
                dupe.health.RemoveHediff(hediff);
            }

            foreach (var hediff in original.health.hediffSet.hediffs)
            {
                var clone = HediffMaker.MakeHediff(hediff.def, dupe, hediff.Part);
                clone.CopyFrom(hediff);
                clone.pawn = dupe;
                dupe.health.AddHediff(clone, hediff.Part, null);
            }
        }

        private static void AddGeneToTracker(Pawn_GeneTracker tracker, Gene gene, bool xenogene)
        {
            if (tracker == null || gene == null)
            {
                return;
            }

            if (GeneTrackerAddGeneMethod != null)
            {
                GeneTrackerAddGeneMethod.Invoke(tracker, new object[] { gene, xenogene });
            }
            else
            {
                tracker.AddGene(gene.def, xenogene);
            }
        }

        private static void CloneGeneSet(IEnumerable<Gene> sourceGenes, Pawn_GeneTracker tracker, Pawn targetPawn, bool xenogene, Dictionary<Gene, Gene> geneMap)
        {
            if (tracker == null || targetPawn?.genes == null)
            {
                return;
            }

            foreach (var gene in sourceGenes)
            {
                var clone = CloneGene(gene, targetPawn);
                if (clone == null)
                {
                    continue;
                }

                AddGeneToTracker(tracker, clone, xenogene);
                if (!geneMap.ContainsKey(gene))
                {
                    geneMap.Add(gene, clone);
                }
            }
        }

        private static Gene? CloneGene(Gene source, Pawn targetPawn)
        {
            if (source == null || targetPawn == null)
            {
                return null;
            }

            var clone = GeneMaker.MakeGene(source.def, targetPawn);
            if (clone == null)
            {
                return null;
            }

            CopyGeneSpecificFields(source, clone);
            return clone;
        }

        private static void CopyGeneSpecificFields(Gene source, Gene target)
        {
            var type = source.GetType();
            while (type != null && typeof(Gene).IsAssignableFrom(type))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (ShouldSkipGeneField(field))
                    {
                        continue;
                    }

                    field.SetValue(target, field.GetValue(source));
                }

                type = type.BaseType;
            }
        }

        private static bool ShouldSkipGeneField(FieldInfo field)
        {
            if (field == null || field.IsInitOnly || field.IsLiteral)
            {
                return true;
            }

            return field.Name == "pawn" || field.Name == "loadID" || field.Name == "overriddenByGene";
        }

        private static void RemapGeneOverrides(Dictionary<Gene, Gene> geneMap)
        {
            if (GeneOverriddenField == null)
            {
                return;
            }

            foreach (var kvp in geneMap)
            {
                var sourceOverride = GeneOverriddenField.GetValue(kvp.Key) as Gene;
                if (sourceOverride != null && geneMap.TryGetValue(sourceOverride, out var replacement))
                {
                    GeneOverriddenField.SetValue(kvp.Value, replacement);
                }
                else
                {
                    GeneOverriddenField.SetValue(kvp.Value, null);
                }
            }
        }

        private static void CopyGeneTrackerState(Pawn_GeneTracker source, Pawn_GeneTracker target)
        {
            if (source == null || target == null)
            {
                return;
            }

            CopyGeneTrackerField(GeneTrackerXenotypeField, source, target);
            CopyGeneTrackerField(GeneTrackerXenotypeNameField, source, target);
            CopyGeneTrackerField(GeneTrackerIconField, source, target);
            CopyGeneTrackerField(GeneTrackerHybridField, source, target);
            CopyGeneTrackerField(GeneTrackerCustomXenoField, source, target);
            CopyGeneTrackerField(GeneTrackerHasCustomXenoField, source, target);
            ClearGeneTrackerCaches(target);
        }

        private static void CopyGeneTrackerField(FieldInfo field, Pawn_GeneTracker source, Pawn_GeneTracker target)
        {
            if (field == null)
            {
                return;
            }

            var value = field.GetValue(source);
            field.SetValue(target, value);
        }

        private static void ClearGeneTrackerCaches(Pawn_GeneTracker tracker)
        {
            GeneTrackerCachedGenesField?.SetValue(tracker, null);
            GeneTrackerCachedDamageField?.SetValue(tracker, null);
            GeneTrackerCachedAddictionField?.SetValue(tracker, null);
            GeneTrackerCachedEnabledNeedsField?.SetValue(tracker, null);
            GeneTrackerCachedDisabledNeedsField?.SetValue(tracker, null);
            GeneTrackerCachedGenesAffectAgeField?.SetValue(tracker, null);
            GeneTrackerCachedTattoosField?.SetValue(tracker, null);
            GeneTrackerCachedWaterCostField?.SetValue(tracker, null);
            GeneTrackerHasCachedWaterField?.SetValue(tracker, false);
        }

        private static void CopyStoryMelanin(Pawn_StoryTracker source, Pawn_StoryTracker target)
        {
            if (StoryMelaninField == null)
            {
                return;
            }

            var melanin = StoryMelaninField.GetValue(source);
            StoryMelaninField.SetValue(target, melanin);
        }

        public static bool PawnHasDupeAbility(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.health?.hediffSet?.HasHediff(MMDefOf.MM_MultipleManGeneHediff) == true)
            {
                return true;
            }

            if (!ModsConfig.BiotechActive || pawn.genes == null)
            {
                return false;
            }

            if (pawn.genes.HasActiveGene(MMDefOf.MM_MultipleManGene))
            {
                return true;
            }

            return false;
        }

        public static void EnsureGeneMarkerHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            if (!pawn.health.hediffSet.HasHediff(MMDefOf.MM_MultipleManGeneHediff))
            {
                var hediff = HediffMaker.MakeHediff(MMDefOf.MM_MultipleManGeneHediff, pawn);
                pawn.health.AddHediff(hediff);
            }
        }

        public static void RemoveGeneMarkerHediff(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return;
            }

            var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(MMDefOf.MM_MultipleManGeneHediff);
            if (hediff != null)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        private static Texture2D LoadIcon(string path, Texture2D fallback)
        {
            try
            {
                return ContentFinder<Texture2D>.Get(path, reportFailure: true);
            }
            catch
            {
                Log.Warning($"[MultipleManXGene] Could not load gizmo icon at 'Textures/{path}.png'. Falling back to the vanilla icon.");
                return fallback;
            }
        }
    }
}
