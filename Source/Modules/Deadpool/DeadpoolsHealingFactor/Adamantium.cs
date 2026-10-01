using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DeadpoolsHealingFactor
{
	public class CompProperties_InfusionProgress : CompProperties
	{
		public CompProperties_InfusionProgress()
		{
			this.compClass = typeof(CompInfusionProgress);
		}
	}

	// (removed duplicate Harmony patch; see the Prefix patch at bottom of file)

	public class CompInfusionProgress : ThingComp
	{
		private int remainingTicks;
		private Pawn subject;

		public bool Active => remainingTicks > 0;
		public Pawn Subject => subject;

		public void BeginInfusion(int durationTicks, Pawn pawn)
		{
			remainingTicks = durationTicks;
			subject = pawn;
			try { parent?.Map?.mapDrawer?.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things); } catch { }
		}

		public void EndInfusion()
		{
			remainingTicks = 0;
			subject = null;
			try { parent?.Map?.mapDrawer?.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things); } catch { }
		}

		public override void CompTick()
		{
			if (remainingTicks > 0)
			{
				remainingTicks--;
				if (remainingTicks % 60 == 0)
				{
					try { parent?.Map?.mapDrawer?.MapMeshDirty(parent.Position, MapMeshFlagDefOf.Things); } catch { }
				}
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (var g in base.CompGetGizmosExtra()) yield return g;
			if (Active)
			{
				yield return new Gizmo_InfusionProgress
				{
					comp = this
				};
			}
		}

		public float Progress01
		{
			get
			{
				float total = Mathf.Max(1f, (float)(DeadpoolsHealingFactorMod.settings?.infusionDurationTicks ?? 30000));
				return Mathf.Clamp01(1f - (remainingTicks / total));
			}
		}

		private void HideSubject()
		{
		}

		private void ShowSubject()
		{
		}
	}

	public class Gizmo_InfusionProgress : Gizmo
	{
		public CompInfusionProgress comp;
		public override float GetWidth(float maxWidth)
		{
			return 212f;
		}
		public override GizmoResult GizmoOnGUI(UnityEngine.Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect overRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 60f);
			Widgets.DrawWindowBackground(overRect);
			Rect barRect = overRect.ContractedBy(6f);
			Widgets.FillableBar(barRect, comp?.Progress01 ?? 0f);
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(barRect, "Infusion Progress");
			Text.Anchor = TextAnchor.UpperLeft;
			return new GizmoResult(GizmoState.Clear);
		}
	}
	public static class DPAdamantiumUtility
	{
		[ThreadStatic]
		public static bool AllowMissingForProstheticRemoval;

                public static bool HasAdamantium(Pawn pawn)
                {
                        return pawn?.health?.hediffSet?.HasHediff(DPDefOf.DP_AdamantiumSkeleton) == true;
                }

                public static bool HasWolverineClaws(Pawn pawn)
                {
                        return pawn?.health?.hediffSet?.HasHediff(DPDefOf.DP_WolverineClaws) == true;
                }

		public static bool IsProtectedBonePart(BodyPartRecord part)
		{
			if (part == null)
			{
				return false;
			}
			return DeadpoolsHealingFactorMod.settings?.IsAdamantiumProtectedBodyPart(part.def) == true;
		}

		public static bool IsProtectedBoneOrAncestor(BodyPartRecord part)
		{
			if (part == null) return false;
			if (IsProtectedBonePart(part)) return true;
			if (part.depth != BodyPartDepth.Outside) return false;
			BodyPartRecord cursor = part.parent;
			while (cursor != null)
			{
				if (IsProtectedBonePart(cursor)) return true;
				cursor = cursor.parent;
			}
			return false;
		}

		public static bool IsBurnableSoftPart(BodyPartRecord part)
		{
			if (part == null)
			{
				return false;
			}
			// Any non-protected and internal depth parts are burnable (organs).
			return !IsProtectedBonePart(part) && part.depth != BodyPartDepth.Outside;
		}

		public static bool HasBurnableSoftPartsRemaining(Pawn pawn)
		{
			if (pawn?.health == null) return false;
			foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
			{
				if (IsBurnableSoftPart(part)) return true;
			}
			return false;
		}

		public static bool IsUndergoingInfusion(Pawn pawn)
		{
			try
			{
				JobDef jobDef = pawn?.CurJobDef;
				if (jobDef == DPDefOf.DP_UseAdamantiumChamber || jobDef == DPDefOf.DP_UseAdamantiumChamberWithClaws)
				{
					Thing t = pawn.CurJob?.targetA.Thing;
					if (t is Building_AdamantiumChamber chamber)
					{
						if (chamber.Infusion?.Active == true && chamber.Infusion.Subject == pawn)
						{
							return true;
						}
					}
				}
			}
			catch { }
			return false;
		}

		public static bool ShouldHidePawnDuringInfusion(Pawn pawn)
		{
			try
			{
				if (pawn == null)
				{
					return false;
				}
				if (!IsUndergoingInfusion(pawn))
				{
					return false;
				}
				Thing thingA = pawn.CurJob?.targetA.Thing;
				Thing thingB = pawn.CurJob?.targetB.Thing;
				Building_AdamantiumChamber chamber = thingA as Building_AdamantiumChamber ?? thingB as Building_AdamantiumChamber;
				if (chamber == null)
				{
					return false;
				}
				if (chamber.Infusion?.Active != true)
				{
					return false;
				}
				return chamber.Infusion.Subject == pawn;
			}
			catch { }
			return false;
		}
	}

        public class Building_AdamantiumChamber : Building
        {
                public const float AdditionalClawFuelCost = 75f;

                public CompRefuelable Refuel => this.GetComp<CompRefuelable>();
                public CompInfusionProgress Infusion => this.GetComp<CompInfusionProgress>();

		public float BaseFuelRequirement => Mathf.Max(0f, (float)(DeadpoolsHealingFactorMod.settings?.chamberFuelRequired ?? 500));

                public float TotalFuelRequired(bool includeClaws)
                {
                        float total = BaseFuelRequirement;
                        if (includeClaws)
                        {
                                total += AdditionalClawFuelCost;
                        }
                        return Mathf.Max(0f, total);
                }

                public float FuelRequiredFor(Pawn pawn, bool includeClaws)
                {
                        float required = 0f;
                        if (pawn == null)
                        {
                                return TotalFuelRequired(includeClaws);
                        }
                        if (!DPAdamantiumUtility.HasAdamantium(pawn))
                        {
                                required += BaseFuelRequirement;
                        }
                        if (includeClaws && !DPAdamantiumUtility.HasWolverineClaws(pawn))
                        {
                                required += AdditionalClawFuelCost;
                        }
                        return Mathf.Max(0f, required);
                }

                public bool HasFuelFor(Pawn pawn, bool includeClaws)
                {
                        return Refuel != null && Refuel.Fuel >= FuelRequiredFor(pawn, includeClaws);
                }

		private Graphic openGraphic;
		private Graphic closedGraphic;

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			try
			{
				if (Refuel != null)
				{
					float baseReq = BaseFuelRequirement;
					float totalReq = TotalFuelRequired(true);
					// Sync refuelable capacity with configured requirement so the gizmo displays correctly
					Refuel.Props.fuelCapacity = Mathf.Max(baseReq, totalReq);
					DPRimForgeIntegration.SyncRefuelComp(Refuel);
					// Note: CompRefuelable does not expose a public setter for Fuel in all versions;
					// we only adjust capacity here so the gizmo displays the configured requirement.
				}
			}
			catch { }
		}

		public override string GetInspectString()
		{
			string baseString = base.GetInspectString();
            float baseReq = BaseFuelRequirement;
            float have = Refuel?.Fuel ?? 0f;
            string fuelLabel = DPRimForgeIntegration.CurrentFuelLabel ?? "fuel";
            string reqLine = $"Required {fuelLabel}: {baseReq:F0}";
                        if (AdditionalClawFuelCost > 0f)
                        {
				reqLine += $" (+{AdditionalClawFuelCost:F0} for claws)";
                        }
                        reqLine += $" (Have: {have:F0})";
			if (string.IsNullOrEmpty(baseString)) return reqLine;
			return baseString + "\n" + reqLine;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (openGraphic == null)
			{
				openGraphic = def.graphicData.Graphic;
			}
			if (closedGraphic == null)
			{
				// Expect the closed-door sprite at Things/Building/Deadpool/AdamantiumChamber_Closed.png
				try
				{
					closedGraphic = GraphicDatabase.Get<Graphic_Single>("Things/Building/Deadpool/AdamantiumChamber_Closed", ShaderDatabase.CutoutComplex, def.graphicData.drawSize, Color.white);
				}
				catch
				{
					closedGraphic = GraphicDatabase.Get<Graphic_Single>("Things/Building/Deadpool/c95dfc59-0a43-4f89-9b0b-f2a6536c55da", ShaderDatabase.CutoutComplex, def.graphicData.drawSize, Color.white);
				}
			}

			// Always draw closed when active; do not draw the base graphic to avoid overlay ordering issues
			if (Infusion?.Active == true)
			{
				Vector3 loc = drawLoc;
				float topAlt = Altitudes.AltitudeFor(AltitudeLayer.BuildingOnTop);
				if (loc.y < topAlt)
				{
					loc.y = topAlt + 0.0002f;
				}
				closedGraphic.Draw(loc, this.Rotation, this);
			}
			else
			{
				openGraphic.Draw(drawLoc, this.Rotation, this);
			}
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
		{
			foreach (var opt in base.GetFloatMenuOptions(myPawn))
			{
				yield return opt;
			}

			if (myPawn == null || myPawn.Dead || myPawn.Map != Map || !Spawned)
			{
				yield break;
			}
			if (!myPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
			{
				yield break;
			}
                        bool hasAdamantium = DPAdamantiumUtility.HasAdamantium(myPawn);
                        bool hasClaws = DPAdamantiumUtility.HasWolverineClaws(myPawn);
                        if (hasAdamantium && hasClaws)
                        {
                                yield return new FloatMenuOption("Already has adamantium skeleton and claws", null);
                                yield break;
                        }
			if (myPawn.RaceProps?.Humanlike != true)
			{
				yield return new FloatMenuOption("Only humanlike pawns can use this.", null);
				yield break;
			}
                        if (Refuel == null)
                        {
                                yield return new FloatMenuOption("No infusion system installed.", null);
                                yield break;
                        }

			float baseReq = FuelRequiredFor(myPawn, false);
			float clawsReq = FuelRequiredFor(myPawn, true);

                        if (!hasAdamantium)
                        {
				if (!HasFuelFor(myPawn, false))
				{
					yield return new FloatMenuOption($"Needs {baseReq:F0} {DPRimForgeIntegration.CurrentFuelLabelCap} loaded", null);
				}
                                else
                                {
                                        yield return FloatMenuUtility.DecoratePrioritizedTask(
                                                new FloatMenuOption("Undergo adamantium infusion", delegate
                                                {
                                                        Job job = JobMaker.MakeJob(DPDefOf.DP_UseAdamantiumChamber, this);
                                                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                                }),
                                                myPawn, this);
                                }
                        }

                        if (!hasClaws)
                        {
                                string clawsLabel = hasAdamantium ? "Install adamantium claws" : "Undergo adamantium infusion with claws";
			if (!HasFuelFor(myPawn, true))
			{
				yield return new FloatMenuOption($"Needs {clawsReq:F0} {DPRimForgeIntegration.CurrentFuelLabelCap} loaded for claws", null);
			}
                                else
                                {
                                        yield return FloatMenuUtility.DecoratePrioritizedTask(
                                                new FloatMenuOption(clawsLabel, delegate
                                                {
                                                        Job job = JobMaker.MakeJob(DPDefOf.DP_UseAdamantiumChamberWithClaws, this);
                                                        myPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                                }),
                                                myPawn, this);
                                }
                        }
                }
        }

	public class JobDriver_UseAdamantiumChamber : JobDriver
	{
		private Building_AdamantiumChamber Chamber => job.targetA.Thing as Building_AdamantiumChamber;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
                        bool wantsClaws = job.def == DPDefOf.DP_UseAdamantiumChamberWithClaws;

                        this.FailOn(() => Chamber == null || Chamber.DestroyedOrNull());
                        this.FailOn(() => Chamber?.Refuel == null);
                        this.FailOn(() => Chamber == null || !Chamber.HasFuelFor(pawn, wantsClaws));

			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

			// Move onto device center and lay down aligned with device orientation (head toward chamber top)
			Toil layDown = new Toil
			{
				initAction = delegate
				{
					pawn.pather.StopDead();
					pawn.Position = Chamber.Position;
					Rot4 desired;
					switch (Chamber.Rotation.AsInt)
					{
						case 0: desired = Rot4.South; break; // chamber up -> head up mapping via South draw
						case 1: desired = Rot4.West; break;  // chamber east -> head east mapping via West draw
						case 2: desired = Rot4.North; break; // chamber south -> head south mapping via North draw
						case 3: desired = Rot4.East; break;  // chamber west -> head west mapping via East draw
						default: desired = Rot4.South; break;
					}
					pawn.Rotation = desired;
					pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
					int duration = Mathf.Max(1, DeadpoolsHealingFactorMod.settings?.infusionDurationTicks ?? 30000);
					Chamber.Infusion?.BeginInfusion(duration, pawn);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return layDown;

			// Configurable infusion duration
			int durationTicks = Mathf.Max(1, DeadpoolsHealingFactorMod.settings?.infusionDurationTicks ?? 30000);
			Toil wait = Toils_General.WaitWith(TargetIndex.A, durationTicks, true);
			wait.WithProgressBarToilDelay(TargetIndex.A);
			wait.tickAction = delegate
			{
				Rot4 desiredTick;
				switch (Chamber.Rotation.AsInt)
				{
					case 0: desiredTick = Rot4.South; break;
					case 1: desiredTick = Rot4.West; break;
					case 2: desiredTick = Rot4.North; break;
					case 3: desiredTick = Rot4.East; break;
					default: desiredTick = Rot4.South; break;
				}
				pawn.Rotation = desiredTick;
				pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp;
			};
			yield return wait;

                        yield return new Toil
                        {
                                initAction = delegate
                                {
                                        if (pawn.RaceProps?.Humanlike != true)
                                        {
                                                return;
                                        }

                                        bool hadAdamantium = DPAdamantiumUtility.HasAdamantium(pawn);
                                        bool hadClaws = DPAdamantiumUtility.HasWolverineClaws(pawn);
                                        bool shouldAddSkeleton = !hadAdamantium;
                                        bool shouldAddClaws = wantsClaws && !hadClaws;

                                        if (!shouldAddSkeleton && !shouldAddClaws)
                                        {
                                                return;
                                        }

				if (!Chamber.HasFuelFor(pawn, wantsClaws))
				{
					float needed = Chamber.FuelRequiredFor(pawn, wantsClaws);
					Messages.Message(pawn.LabelShortCap + $" cannot undergo infusion without {needed:F0} {DPRimForgeIntegration.CurrentFuelLabelCap} loaded.", pawn, MessageTypeDefOf.RejectInput);
					return;
				}

                                        float fuelNeeded = Chamber.FuelRequiredFor(pawn, wantsClaws);
                                        if (fuelNeeded > 0f)
                                        {
                                                Chamber.Refuel?.ConsumeFuel(fuelNeeded);
                                        }

                                        bool addedSkeleton = false;
                                        bool addedClaws = false;

                                        if (shouldAddSkeleton)
                                        {
                                                Hediff h = pawn.health.AddHediff(DPDefOf.DP_AdamantiumSkeleton);
                                                if (h != null)
                                                {
                                                        h.Severity = 1.0f;
                                                }
                                                addedSkeleton = true;
                                                // Massive bleeding from full-body infusion trauma
                                                try
                                                {
                                                        // Apply only superficial damage to visible, non-core parts to avoid engine warnings
                                                        foreach (var part in pawn.health.hediffSet.GetNotMissingParts())
                                                        {
                                                                if (part.depth != BodyPartDepth.Outside) continue;
                                                                string name = part.def?.defName ?? string.Empty;
                                                                if (name == "Head" || name == "Torso" || name.Contains("Waist")) continue;
                                                                float dmg = Rand.Range(1f, 2f);
                                                                var dinfo = new DamageInfo(DamageDefOf.Cut, dmg, 0f, -1f, Chamber, part);
                                                                pawn.TakeDamage(dinfo);
                                                        }
                                                }
                                                catch { }
                                        }

                                        if (shouldAddClaws)
                                        {
                                                Hediff claws = pawn.health.AddHediff(DPDefOf.DP_WolverineClaws);
                                                if (claws != null)
                                                {
                                                        claws.Severity = 1.0f;
                                                }
                                                addedClaws = true;
                                        }

                                        string msg = null;
                                        if (addedSkeleton && addedClaws)
                                        {
                                                msg = pawn.LabelShortCap + " received an adamantium skeleton and claws.";
                                        }
                                        else if (addedClaws)
                                        {
                                                msg = pawn.LabelShortCap + " received adamantium claws.";
                                        }
                                        else if (addedSkeleton)
                                        {
                                                msg = pawn.LabelShortCap + " received an adamantium skeleton.";
                                        }

                                        if (!string.IsNullOrEmpty(msg))
                                        {
                                                Messages.Message(msg, pawn, MessageTypeDefOf.PositiveEvent);
                                        }
                                },
                                defaultCompleteMode = ToilCompleteMode.Instant
                        };

			// Stand up/restore posture
			Toil stand = new Toil
			{
				initAction = delegate { pawn.jobs.posture = PawnPosture.Standing; Chamber.Infusion?.EndInfusion(); },
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return stand;
		}
	}

	// Overload 1: AddHediff(Hediff, BodyPartRecord, DamageInfo?, DamageWorker.DamageResult?)
	[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), new System.Type[] { typeof(Hediff), typeof(BodyPartRecord), typeof(Nullable<DamageInfo>), typeof(DamageWorker.DamageResult) })]
	[HarmonyPriority(Priority.First)]
	public static class Patch_Pawn_HealthTracker_AddHediff_Hediff
	{
		public static bool Prefix(Hediff hediff, BodyPartRecord part, Pawn ___pawn)
		{
			try
			{
				BodyPartRecord targetPart = part ?? hediff?.Part;
				if (hediff is Hediff_MissingPart && DPAdamantiumUtility.HasAdamantium(___pawn) && !DPAdamantiumUtility.AllowMissingForProstheticRemoval)
				{
					if (DPAdamantiumUtility.IsProtectedBoneOrAncestor(targetPart))
					{
						return false;
					}
				}
			}
			catch { }
			return true;
		}
	}

	// Overload 2: AddHediff(HediffDef, BodyPartRecord, DamageInfo?, DamageWorker.DamageResult?)
	[HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff), new System.Type[] { typeof(HediffDef), typeof(BodyPartRecord), typeof(Nullable<DamageInfo>), typeof(DamageWorker.DamageResult) })]
	[HarmonyPriority(Priority.First)]
	public static class Patch_Pawn_HealthTracker_AddHediff_HediffDef
	{
		public static bool Prefix(HediffDef def, BodyPartRecord part, Pawn ___pawn)
		{
			try
			{
				if (def == null)
				{
					return true;
				}
				if ((def == HediffDefOf.MissingBodyPart || typeof(Hediff_MissingPart).IsAssignableFrom(def.hediffClass)) && !DPAdamantiumUtility.AllowMissingForProstheticRemoval)
				{
					if (DPAdamantiumUtility.HasAdamantium(___pawn) && DPAdamantiumUtility.IsProtectedBoneOrAncestor(part))
					{
						return false;
					}
				}
			}
			catch { }
			return true;
		}
	}



	// As a final guard, intercept HediffSet.AddDirect(Hediff hediff, DamageInfo? dinfo, DamageWorker.DamageResult) calls
	// to prevent MissingBodyPart hediffs on protected parts
	[HarmonyPatch(typeof(HediffSet), nameof(HediffSet.AddDirect), new System.Type[] { typeof(Hediff), typeof(Nullable<DamageInfo>), typeof(DamageWorker.DamageResult) })]
	[HarmonyPriority(Priority.First)]
	public static class Patch_HediffSet_AddDirect
	{
		public static bool Prefix(Hediff hediff, Pawn ___pawn)
		{
			try
			{
				if (hediff is Hediff_MissingPart && DPAdamantiumUtility.HasAdamantium(___pawn))
				{
					// Allow only when explicitly enabled for prosthetic removal flow
					if (!DPAdamantiumUtility.AllowMissingForProstheticRemoval && DPAdamantiumUtility.IsProtectedBoneOrAncestor(hediff.Part))
					{
						return false;
					}
				}
			}
			catch { }
			return true;
		}
	}

    // Prevent installing artificial parts that replace bone-bearing protected parts (arms, legs, spine, ribs, etc.)
    // but allow upgrading/replacing an already-installed artificial part on that same slot.
    [HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), nameof(Recipe_InstallArtificialBodyPart.ApplyOnPawn))]
    [HarmonyPriority(Priority.First)]
    public static class Patch_Recipe_InstallArtificialBodyPart_ApplyOnPawn
    {
        public static bool Prefix(Recipe_InstallArtificialBodyPart __instance, Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            try
            {
                if (pawn == null || part == null) return true;
                if (!DPAdamantiumUtility.HasAdamantium(pawn)) return true;
                if (DPAdamantiumUtility.IsProtectedBoneOrAncestor(part))
                {
                    // If an artificial part is already installed on this slot, allow replacement/upgrade.
                    bool hasExistingArtificial = false;
                    try
                    {
                        hasExistingArtificial = pawn.health.hediffSet.hediffs
                            .OfType<Hediff_AddedPart>()
                            .Any(h => h.Part == part);
                    }
                    catch { }

                    if (hasExistingArtificial)
                    {
                        return true; // let vanilla replace the existing artificial part
                    }

                    // Otherwise, block install and refund the implant item (not medicine).
                    try
                    {
                        if (ingredients != null)
                        {
                            // Pick the most likely prosthetic: any non-medicine item; if multiple, prefer the one with BodyParts category,
                            // otherwise fall back to the first non-medicine item.
                            Thing chosen = null;
                            Thing bodyPartCandidate = null;
                            foreach (var ing in ingredients)
                            {
                                if (ing == null) continue;
                                var def = ing.def;
                                if (def == null) continue;
                                if (def.IsMedicine) continue;
                                if (def.category != ThingCategory.Item) continue;
                                var cats = def.thingCategories;
                                bool isBodyPart = cats != null && cats.Contains(ThingCategoryDefOf.BodyParts);
                                if (isBodyPart && bodyPartCandidate == null)
                                {
                                    bodyPartCandidate = ing;
                                }
                                if (chosen == null)
                                {
                                    chosen = ing;
                                }
                            }
                            var toRefund = bodyPartCandidate ?? chosen;
                            if (toRefund != null)
                            {
                                ThingDef def = toRefund.def;
                                ThingDef stuff = toRefund.Stuff;
                                QualityCategory? quality = null;
                                try { var cq = toRefund.TryGetComp<CompQuality>(); if (cq != null) quality = cq.Quality; } catch { }
                                Thing refund = null;
                                try { refund = ThingMaker.MakeThing(def, stuff); } catch { }
                                if (refund != null)
                                {
                                    try
                                    {
                                        var rCQ = refund.TryGetComp<CompQuality>();
                                        if (rCQ != null && quality.HasValue)
                                        {
                                            rCQ.SetQuality(quality.Value, ArtGenerationContext.Outsider);
                                        }
                                    }
                                    catch { }
                                    TryPlaceOrDropNear(billDoer ?? pawn, refund);
                                }
                            }
                        }
                    }
                    catch { }
                    Messages.Message(pawn.LabelShortCap + "'s adamantium bones reject the implant. Surgery canceled and the implant is returned.", pawn, MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            catch { }
            return true;
        }

        private static void TryPlaceOrDropNear(Pawn actor, Thing thing)
        {
            try
            {
                if (actor?.Map == null || thing == null)
                {
                    return;
                }
                // Only place valid, non-destroyed items
                if (thing.DestroyedOrNull() || thing.stackCount <= 0)
                {
                    return;
                }
                IntVec3 pos = actor.Position;
                Thing dropped;
                GenPlace.TryPlaceThing(thing, pos, actor.Map, ThingPlaceMode.Near, out dropped);
            }
            catch { }
        }
    }

	// Allow removing an existing artificial part without amputating the underlying bone for adamantium skeletons
	[HarmonyPatch(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn))]
	[HarmonyPriority(Priority.First)]
	public static class Patch_Recipe_RemoveBodyPart_ApplyOnPawn
	{
		public static bool Prefix(Recipe_RemoveBodyPart __instance, Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
		{
			try
			{
				if (pawn == null || part == null) return true;
				if (!DPAdamantiumUtility.HasAdamantium(pawn)) return true;
				// If the target part has an added artificial part, remove it and skip amputation
				var addedOnPart = pawn.health.hediffSet.hediffs
					.OfType<Hediff_AddedPart>()
					.Where(h => h.Part == part)
					.ToList();
				if (addedOnPart.Count > 0)
				{
					// Try to spawn the corresponding item(s) back if the def exposes one
					foreach (var h in addedOnPart)
					{
						TrySpawnFromAddedPart(billDoer ?? pawn, h);
					}
					foreach (var h in addedOnPart)
					{
						pawn.health.RemoveHediff(h);
					}
					// Mark the part missing to trigger regrowth over time (not instant restore)
					try { DPAdamantiumUtility.AllowMissingForProstheticRemoval = true; pawn.health.AddHediff(HediffDefOf.MissingBodyPart, part); }
					finally { DPAdamantiumUtility.AllowMissingForProstheticRemoval = false; }
					// Seed regrowth on the parent anchor so the system starts immediately
					var parent = part.parent ?? part;
					pawn.health.AddHediff(DPDefOf.DP_regrowing, parent);
					if (billDoer != null)
					{
						TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
					}
					Messages.Message(pawn.LabelShortCap + " had an artificial part removed; regrowth initiated.", pawn, MessageTypeDefOf.PositiveEvent);
					return false; // skip vanilla removal (which would amputate)
				}

				// If no prosthetic present and part is adamantium-protected bone, reject the attempt
				if (DPAdamantiumUtility.IsProtectedBoneOrAncestor(part))
				{
					Messages.Message(pawn.LabelShortCap + " cannot have an adamantium bone amputated.", pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			catch { }
			return true;
		}

		private static void TrySpawnFromAddedPart(Pawn actor, Hediff_AddedPart h)
		{
			try
			{
				ThingDef itemDef = h.def?.spawnThingOnRemoved;
				if (itemDef == null) return;
				if (actor?.Map == null) return;
				var item = ThingMaker.MakeThing(itemDef);
				TryPlaceOrDropNear(actor, item);
			}
			catch { }
		}

		private static void TryPlaceOrDropNear(Pawn actor, Thing thing)
		{
			try
			{
				if (actor?.Map == null)
				{
					return;
				}
				IntVec3 pos = actor.Position;
				Thing dropped;
				if (!GenPlace.TryPlaceThing(thing, pos, actor.Map, ThingPlaceMode.Near, out dropped))
				{
					thing.Position = pos;
					thing.SpawnSetup(actor.Map, respawningAfterLoad: false);
				}
			}
			catch { }
		}
	}



	// Allow burning to damage corpse flesh, but never destroy the corpse fully if pawn had adamantium skeleton
	[HarmonyPatch(typeof(Thing), "TakeDamage")]
	public static class Patch_Thing_TakeDamage
	{
		public static void Prefix(Thing __instance, ref DamageInfo dinfo)
		{
			try
			{
				if (__instance is Corpse corpse)
				{
					Pawn inner = corpse.InnerPawn;
					bool hasSkeleton = DPAdamantiumUtility.HasAdamantium(inner);
					bool isBurning = dinfo.Def == DamageDefOf.Flame || dinfo.Def == DamageDefOf.Burn;
					if (hasSkeleton && isBurning)
					{
						int hp = corpse.HitPoints;
						if (hp <= 1)
						{
							// Clamp at 1 HP and snuff attached fire so the corpse persists
							dinfo.SetAmount(0f);
							if (corpse.HitPoints < 1)
							{
								corpse.HitPoints = 1;
							}
							if (corpse.Spawned)
							{
								var fire = corpse.GetAttachment(ThingDefOf.Fire) ?? corpse.Position.GetFirstThing(corpse.Map, ThingDefOf.Fire);
								fire?.Destroy(DestroyMode.Vanish);
							}
						}
						else
						{
							float maxAllowed = Math.Max(0f, hp - 1);
							if (dinfo.Amount > maxAllowed)
							{
								dinfo.SetAmount(maxAllowed);
							}
						}
					}
				}
				else if (__instance is Pawn pawn && DPAdamantiumUtility.HasAdamantium(pawn))
				{
					// Block blunt damage to protected parts to prevent cracks while alive
					if (dinfo.Def == DamageDefOf.Blunt && DPAdamantiumUtility.IsProtectedBoneOrAncestor(dinfo.HitPart))
					{
						dinfo.SetAmount(0f);
					}
				}
			}
			catch
			{
				// best-effort; ignore if engine details differ
			}
		}
	}

	// Hide the pawn while in the chamber by skipping PawnRenderer.RenderPawnInternal when infusion is active (either job variant)
	[HarmonyPatch(typeof(PawnRenderer), "RenderPawnInternal")]
	public static class Patch_PawnRenderer_RenderPawnInternal
	{
		private static bool ShouldHidePawn(Pawn pawn)
		{
			return DPAdamantiumUtility.ShouldHidePawnDuringInfusion(pawn);
		}

		public static bool Prefix(PawnRenderer __instance, Pawn ___pawn)
		{
			try
			{
				Pawn pawn = ___pawn;
				if (ShouldHidePawn(pawn))
				{
					return false; // skip drawing pawn
				}
			}
			catch { }
			return true;
		}
	}

	// Prevent drafting while undergoing infusion (blocks canceling by draft toggle)
	[HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
	public static class Patch_Pawn_DraftController_set_Drafted
	{
		public static bool Prefix(Pawn_DraftController __instance, bool value)
		{
			try
			{
				var pawnField = AccessTools.Field(typeof(Pawn_DraftController), "pawn");
				Pawn pawn = (Pawn)pawnField.GetValue(__instance);
				if (DPAdamantiumUtility.IsUndergoingInfusion(pawn))
				{
					return false; // block drafting state change
				}
			}
			catch { }
			return true;
		}
	}

	// Prevent giving new ordered jobs while undergoing infusion (blocks cancel order)
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
	public static class Patch_Pawn_JobTracker_TryTakeOrderedJob
	{
		public static bool Prefix(Pawn_JobTracker __instance, Job job, JobTag? tag)
		{
			try
			{
				Pawn pawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance) as Pawn;
				if (DPAdamantiumUtility.IsUndergoingInfusion(pawn))
				{
					Messages.Message(pawn.LabelShortCap + " cannot be interrupted during infusion.", pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			catch { }
			return true;
		}
	}

	// Block attempts to end the infusion job early (e.g., via Stop or Clear prioritized work),
	// but allow normal completion (Succeeded)
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
	public static class Patch_Pawn_JobTracker_EndCurrentJob
	{
		public static bool Prefix(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
		{
			try
			{
				Pawn pawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance) as Pawn;
				if (pawn?.CurJobDef == DPDefOf.DP_UseAdamantiumChamber && DPAdamantiumUtility.IsUndergoingInfusion(pawn) && condition != JobCondition.Succeeded)
				{
					Messages.Message(pawn.LabelShortCap + " cannot leave the chamber until infusion completes.", pawn, MessageTypeDefOf.RejectInput);
					return false; // block premature end
				}
			}
			catch { }
			return true;
		}
	}

	// Block selection attempts at the selector level while pawn is undergoing infusion
	[HarmonyPatch(typeof(Selector), nameof(Selector.Select))]
	public static class Patch_Selector_Select
	{
		public static bool Prefix(Selector __instance, object obj, bool playSound = true, bool forceDesignatorDeselect = true)
		{
			try
			{
				if (obj is Pawn pawn && DPAdamantiumUtility.IsUndergoingInfusion(pawn))
				{
					Messages.Message(pawn.LabelShortCap + " cannot be selected during infusion.", pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			catch { }
			return true;
		}
	}

	// As a fallback, block mass job stopping during infusion
	[HarmonyPatch(typeof(Pawn_JobTracker), "StopAll")]
	public static class Patch_Pawn_JobTracker_StopAll
	{
		public static bool Prefix(Pawn_JobTracker __instance)
		{
			try
			{
				Pawn pawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance) as Pawn;
				if (DPAdamantiumUtility.IsUndergoingInfusion(pawn))
				{
					Messages.Message(pawn.LabelShortCap + " cannot be interrupted during infusion.", pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			catch { }
			return true;
		}
	}

	// Also prevent any new job from starting while infusion is active
	[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
	public static class Patch_Pawn_JobTracker_StartJob
	{
		public static bool Prefix(Pawn_JobTracker __instance, Job newJob)
		{
			try
			{
				Pawn pawn = AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance) as Pawn;
				if (DPAdamantiumUtility.IsUndergoingInfusion(pawn) && (newJob == null || newJob.def != DPDefOf.DP_UseAdamantiumChamber))
				{
					Messages.Message(pawn.LabelShortCap + " cannot start a new job during infusion.", pawn, MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			catch { }
			return true;
		}
	}


    

	// Ensure the game reports a face-up posture while using the chamber, so rendering uses the correct lay variant
	// Keep posture vanilla; no override
	public static class Disabled_Patch_PawnUtility_GetPosture
	{
		public static void Postfix(Pawn p, ref PawnPosture __result)
		{
			try
			{
				if (p?.CurJobDef == DPDefOf.DP_UseAdamantiumChamber)
				{
					// Only force face-up when at the chamber and stationary
					Thing t = p.CurJob?.targetA.Thing;
					if (t is Building_AdamantiumChamber chamber && p.pather?.Moving != true && p.Position == chamber.Position)
					{
						// no-op: keep default posture
					}
				}
			}
			catch { }
		}
	}
}


