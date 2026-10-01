using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CyclopsGene
{
    public class HediffCompProperties_OpticController : HediffCompProperties
    {
        public float maxStrain = 100f;
        public int leadershipRefreshTicks = 60;
        public int leadershipBuffTicks = 90;

        public HediffCompProperties_OpticController()
        {
            compClass = typeof(HediffComp_OpticController);
        }
    }

    public class HediffComp_OpticController : HediffComp
    {
        private float strain;
        private bool visorProvisioned;

        private HediffCompProperties_OpticController Props => (HediffCompProperties_OpticController)props;

        public float Strain => strain;
        public float MaxStrain => Props.maxStrain;
        public float StrainPercent => Mathf.Clamp01(strain / Mathf.Max(MaxStrain, 1f));

        public float RecoveryPerSecond
        {
            get
            {
                CyclopsGeneSettings settings = CyclopsGeneMod.Settings;
                return settings?.strainRecoveryPerSecond ?? CyclopsGeneSettings.StrainRecoveryDefault;
            }
        }

        public bool CanAddStrain(float amount)
        {
            return strain + amount <= MaxStrain + 0.001f;
        }

        public void AddStrain(float amount)
        {
            strain = Mathf.Clamp(strain + amount, 0f, MaxStrain);
        }

        public override string CompDescriptionExtra =>
            "CyclopsGeneControllerDescription".Translate(RecoveryPerSecond.ToString("0.#"));

        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            TryProvisionVisor();
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (strain > 0f)
            {
                strain = Mathf.Max(0f, strain - RecoveryPerSecond / 60f);
            }

            if (!visorProvisioned && Pawn.IsHashIntervalTick(60))
            {
                TryProvisionVisor();
            }

            if (ShouldProjectLeadershipAura() && Pawn.IsHashIntervalTick(Mathf.Max(Props.leadershipRefreshTicks, 1)))
            {
                RefreshLeadershipAura();
            }
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref strain, "opticStrain", 0f);
            Scribe_Values.Look(ref visorProvisioned, "visorProvisioned", false);
        }

        public override IEnumerable<Gizmo> CompGetGizmos()
        {
            if (Pawn?.Faction == Faction.OfPlayer && (Pawn.IsColonistPlayerControlled || Pawn.IsColonyAnimal))
            {
                yield return new Gizmo_OpticStrain(this);
            }
        }

        public override void CopyFrom(HediffComp other)
        {
            base.CopyFrom(other);
            if (other is HediffComp_OpticController controller)
            {
                strain = controller.strain;
                visorProvisioned = controller.visorProvisioned;
            }
        }

        private void TryProvisionVisor()
        {
            CyclopsGeneSettings settings = CyclopsGeneMod.Settings;
            if (settings?.visorDependencyEnabled != true || Pawn?.RaceProps?.Humanlike != true)
            {
                return;
            }

            visorProvisioned = CyclopsUtility.EnsureControlVisor(Pawn);
        }

        private bool ShouldProjectLeadershipAura()
        {
            CyclopsGeneSettings settings = CyclopsGeneMod.Settings;
            return settings?.leadershipEnabled != false
                && Pawn?.RaceProps?.Humanlike == true
                && Pawn.Spawned
                && !Pawn.Dead
                && !Pawn.Downed
                && Pawn.Awake()
                && Pawn.Faction != null;
        }

        private void RefreshLeadershipAura()
        {
            float radius = CyclopsGeneMod.Settings?.leadershipRadius ?? CyclopsGeneSettings.LeadershipRadiusDefault;
            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(Pawn.Position, Pawn.Map, radius, useCenter: true))
            {
                if (!(thing is Pawn ally) || ally == Pawn || ally.Dead || ally.RaceProps?.Humanlike != true
                    || ally.Faction != Pawn.Faction || ally.health?.hediffSet == null)
                {
                    continue;
                }

                Hediff presence = ally.health.hediffSet.GetFirstHediffOfDef(CyclopsDefOf.Cyclops_CommandPresence);
                if (presence == null)
                {
                    presence = ally.health.AddHediff(CyclopsDefOf.Cyclops_CommandPresence);
                }

                presence?.TryGetComp<HediffComp_Disappears>()?.SetDuration(Props.leadershipBuffTicks);
            }
        }
    }
}
