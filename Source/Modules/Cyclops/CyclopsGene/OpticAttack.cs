using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CyclopsGene
{
    public enum OpticAttackMode
    {
        Basic,
        Volley,
        Focused,
        Cone
    }

    public class CompProperties_OpticAttack : CompProperties_AbilityEffect
    {
        public OpticAttackMode mode;
        public float strainCost;
        public float armorPenetration;
        public int pulseCount = 1;
        public int ticksBetweenPulses = 1;
        public int durationTicks = 6;
        public int stunTicks;

        public CompProperties_OpticAttack()
        {
            compClass = typeof(CompAbilityEffect_OpticAttack);
        }
    }

    public class CompAbilityEffect_OpticAttack : CompAbilityEffect
    {
        private static readonly Color PreviewColor = new Color(1f, 0.08f, 0.04f, 0.8f);

        public CompProperties_OpticAttack OpticProps => (CompProperties_OpticAttack)props;
        public float StrainCost => OpticProps.strainCost;

        private HediffComp_OpticController Controller => CyclopsUtility.GetController(parent.pawn);

        private bool MissingRequiredVisor => OpticProps.mode != OpticAttackMode.Basic
            && CyclopsGeneMod.Settings?.visorDependencyEnabled == true
            && !CyclopsUtility.HasControlVisor(parent.pawn);

        public override bool ShouldHideGizmo => Controller == null;

        public override bool GizmoDisabled(out string reason)
        {
            HediffComp_OpticController controller = Controller;
            if (controller == null)
            {
                reason = "CyclopsRequiresGene".Translate();
                return true;
            }

            if (MissingRequiredVisor)
            {
                reason = "CyclopsRequiresVisor".Translate();
                return true;
            }

            if (!controller.CanAddStrain(StrainCost))
            {
                reason = "CyclopsTooMuchStrain".Translate();
                return true;
            }

            reason = null;
            return false;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }

            if (!target.IsValid || target.Cell == parent.pawn.Position)
            {
                if (throwMessages)
                {
                    Messages.Message("CyclopsInvalidTarget".Translate(), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }

            return true;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            HediffComp_OpticController controller = Controller;
            if (controller == null || MissingRequiredVisor || !controller.CanAddStrain(StrainCost)
                || parent.pawn.Map == null)
            {
                return;
            }

            controller.AddStrain(StrainCost);

            OpticBlastEffect effect = ThingMaker.MakeThing(CyclopsDefOf.Cyclops_OpticBlastEffect) as OpticBlastEffect;
            if (effect == null)
            {
                Log.Error("Cyclops X-Gene could not create its optic blast effect.");
                return;
            }

            int duration = DurationForMode(OpticProps.mode);
            int pulses = PulseCountForMode(OpticProps.mode, duration);
            if (OpticProps.mode == OpticAttackMode.Volley)
            {
                duration = Mathf.Max(duration,
                    (pulses - 1) * Mathf.Max(OpticProps.ticksBetweenPulses, 1) + 6);
            }
            effect.Initialize(parent.pawn, target, OpticProps.mode, DamageForMode(OpticProps.mode),
                OpticProps.armorPenetration, pulses, OpticProps.ticksBetweenPulses,
                duration, OpticProps.stunTicks);
            GenSpawn.Spawn(effect, parent.pawn.Position, parent.pawn.Map);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (!target.IsValid || parent.pawn?.Map == null)
            {
                return;
            }

            if (OpticProps.mode == OpticAttackMode.Cone)
            {
                GenDraw.DrawFieldEdges(OpticBlastEffect.ConeCells(parent.pawn.Position, target.Cell,
                    ConeRange, ConeAngle, parent.pawn.Map), PreviewColor);
            }
            else
            {
                Vector3 start = parent.pawn.DrawPos;
                Vector3 end = target.CenterVector3;
                start.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                end.y = start.y;
                GenDraw.DrawLineBetween(start, end, SimpleColor.Red, OpticProps.mode == OpticAttackMode.Focused ? 0.35f : 0.2f);
            }
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (!base.AICanTargetNow(target) || Controller == null || MissingRequiredVisor
                || !Controller.CanAddStrain(StrainCost))
            {
                return false;
            }

            if (OpticProps.mode != OpticAttackMode.Cone)
            {
                return target.Thing == null || target.Thing.Faction == null
                    || target.Thing.Faction.HostileTo(parent.pawn.Faction);
            }

            List<IntVec3> cells = OpticBlastEffect.ConeCells(parent.pawn.Position, target.Cell,
                ConeRange, ConeAngle, parent.pawn.Map);
            int hostiles = 0;
            foreach (Pawn pawn in cells.SelectMany(cell => cell.GetThingList(parent.pawn.Map)).OfType<Pawn>().Distinct())
            {
                if (pawn == parent.pawn || pawn.Dead)
                {
                    continue;
                }

                if (pawn.Faction != null && pawn.Faction == parent.pawn.Faction)
                {
                    return false;
                }

                if (pawn.HostileTo(parent.pawn))
                {
                    hostiles++;
                }
            }
            return hostiles > 0;
        }

        public override string ExtraTooltipPart()
        {
            float damage = DamageForMode(OpticProps.mode);
            int pulses = PulseCountForMode(OpticProps.mode, DurationForMode(OpticProps.mode));
            string damageText = OpticProps.mode == OpticAttackMode.Cone
                ? "CyclopsConeDamageTooltip".Translate(damage.ToString("0"))
                : "CyclopsPulseDamageTooltip".Translate(damage.ToString("0"), pulses);
            return "CyclopsOpticAttackTooltip".Translate(damageText, StrainCost.ToString("0"));
        }

        public static float ConeRange => CyclopsGeneMod.Settings?.coneRange ?? CyclopsGeneSettings.ConeRangeDefault;
        public static float ConeAngle => CyclopsGeneMod.Settings?.coneAngle ?? CyclopsGeneSettings.ConeAngleDefault;

        public static float DamageForMode(OpticAttackMode mode)
        {
            CyclopsGeneSettings settings = CyclopsGeneMod.Settings;
            switch (mode)
            {
                case OpticAttackMode.Volley:
                    return settings?.rapidDamage ?? CyclopsGeneSettings.RapidDamageDefault;
                case OpticAttackMode.Focused:
                    return settings?.focusedDamage ?? CyclopsGeneSettings.FocusedDamageDefault;
                case OpticAttackMode.Cone:
                    return settings?.coneDamage ?? CyclopsGeneSettings.ConeDamageDefault;
                default:
                    return settings?.damageAmount ?? CyclopsGeneSettings.DamageDefault;
            }
        }

        public static int DurationForMode(OpticAttackMode mode)
        {
            if (mode == OpticAttackMode.Focused)
            {
                return CyclopsGeneMod.Settings?.focusedDurationTicks ?? CyclopsGeneSettings.FocusedDurationDefault;
            }

            return mode == OpticAttackMode.Cone ? 20 : mode == OpticAttackMode.Volley ? 28 : 6;
        }

        private int PulseCountForMode(OpticAttackMode mode, int duration)
        {
            if (mode == OpticAttackMode.Focused)
            {
                return Mathf.CeilToInt(duration / (float)Mathf.Max(OpticProps.ticksBetweenPulses, 1));
            }

            if (mode == OpticAttackMode.Volley)
            {
                return CyclopsGeneMod.Settings?.rapidPulseCount ?? CyclopsGeneSettings.RapidPulseCountDefault;
            }

            return OpticProps.pulseCount;
        }
    }
}
