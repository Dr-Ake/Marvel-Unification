using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace CyclopsGene
{
    [StaticConstructorOnStartup]
    public class OpticBlastEffect : Thing
    {
        private Pawn caster;
        private Thing targetThing;
        private IntVec3 targetCell;
        private OpticAttackMode mode;
        private float damage;
        private float armorPenetration;
        private int pulseCount;
        private int ticksBetweenPulses;
        private int durationTicks;
        private int stunTicks;
        private int ageTicks;
        private int pulsesFired;
        private int visiblePulseTicks;
        private bool coneDamageApplied;

        private static readonly Material BeamGlowMat = MaterialPool.MatFrom("Effects/OpticBeamCore",
            ShaderDatabase.Transparent, new Color(1f, 0.08f, 0.04f, 0.42f));

        private static readonly Material BeamCoreMat = MaterialPool.MatFrom("Effects/OpticBeamCore",
            ShaderDatabase.Transparent, Color.white);

        private static readonly Material ImpactMat = MaterialPool.MatFrom("Effects/OpticImpact",
            ShaderDatabase.Transparent, new Color(1f, 0.45f, 0.42f));

        private const string ConeFanTexturePath = "Effects/OpticConeFan";

        public void Initialize(Pawn caster, LocalTargetInfo target, OpticAttackMode mode, float damage,
            float armorPenetration, int pulseCount, int ticksBetweenPulses, int durationTicks, int stunTicks)
        {
            this.caster = caster;
            targetThing = mode == OpticAttackMode.Cone ? null : target.Thing;
            targetCell = target.Cell;
            this.mode = mode;
            this.damage = damage;
            this.armorPenetration = armorPenetration;
            this.pulseCount = Mathf.Max(pulseCount, 1);
            this.ticksBetweenPulses = Mathf.Max(ticksBetweenPulses, 1);
            this.durationTicks = Mathf.Max(durationTicks, 2);
            this.stunTicks = stunTicks;
        }

        protected override void Tick()
        {
            base.Tick();
            ageTicks++;

            if (caster == null || caster.Destroyed || caster.Map != Map || !caster.Spawned)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (targetThing != null && !targetThing.Destroyed && targetThing.Map == Map)
            {
                targetCell = targetThing.Position;
            }

            if (!targetCell.InBounds(Map))
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            if (mode == OpticAttackMode.Cone)
            {
                if (!coneDamageApplied)
                {
                    ApplyConeDamage();
                    coneDamageApplied = true;
                }
            }
            else if (pulsesFired < pulseCount && ageTicks >= pulsesFired * ticksBetweenPulses + 1)
            {
                ApplyLineDamage();
                pulsesFired++;
                visiblePulseTicks = mode == OpticAttackMode.Focused ? ticksBetweenPulses + 1 : 4;
            }

            if (visiblePulseTicks > 0)
            {
                visiblePulseTicks--;
            }

            if (ageTicks >= durationTicks)
            {
                Destroy(DestroyMode.Vanish);
            }
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (caster == null || caster.Map != Map || !targetCell.IsValid)
            {
                return;
            }

            float lifeFade = 1f - Mathf.Clamp01((ageTicks - durationTicks * 0.72f) / Mathf.Max(durationTicks * 0.28f, 1f));
            if (mode == OpticAttackMode.Cone)
            {
                DrawCone(lifeFade);
                return;
            }

            if (mode != OpticAttackMode.Focused && visiblePulseTicks <= 0)
            {
                return;
            }

            IntVec3 impactCell = FindImpactCell(out _);
            Vector3 start = BeamStart(impactCell);
            Vector3 end = impactCell.ToVector3Shifted();
            float pulse = 0.92f + Mathf.Sin((ageTicks + thingIDNumber) * 0.65f) * 0.08f;
            float coreWidth = (mode == OpticAttackMode.Focused ? 0.34f : 0.18f) * pulse * lifeFade;
            DrawBeam(start, end, coreWidth, lifeFade);
            DrawImpact(end, (mode == OpticAttackMode.Focused ? 0.82f : 0.5f) * pulse * lifeFade);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_References.Look(ref targetThing, "targetThing");
            Scribe_Values.Look(ref targetCell, "targetCell");
            Scribe_Values.Look(ref mode, "mode", OpticAttackMode.Basic);
            Scribe_Values.Look(ref damage, "damage", 1f);
            Scribe_Values.Look(ref armorPenetration, "armorPenetration", 0f);
            Scribe_Values.Look(ref pulseCount, "pulseCount", 1);
            Scribe_Values.Look(ref ticksBetweenPulses, "ticksBetweenPulses", 1);
            Scribe_Values.Look(ref durationTicks, "durationTicks", 6);
            Scribe_Values.Look(ref stunTicks, "stunTicks", 0);
            Scribe_Values.Look(ref ageTicks, "ageTicks", 0);
            Scribe_Values.Look(ref pulsesFired, "pulsesFired", 0);
            Scribe_Values.Look(ref visiblePulseTicks, "visiblePulseTicks", 0);
            Scribe_Values.Look(ref coneDamageApplied, "coneDamageApplied", false);
        }

        private void ApplyLineDamage()
        {
            IntVec3 impactCell = FindImpactCell(out Thing hitThing);
            if (hitThing != null && !hitThing.Destroyed)
            {
                ApplyDamage(hitThing, damage, stunTicks);
            }

            SpawnImpactFlecks(impactCell, mode == OpticAttackMode.Focused ? 1.1f : 0.7f);
        }

        private IntVec3 FindImpactCell(out Thing hitThing)
        {
            hitThing = null;
            IntVec3 lastValid = caster.Position;

            foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(caster.Position, targetCell))
            {
                if (cell == caster.Position || !cell.InBounds(Map))
                {
                    continue;
                }

                lastValid = cell;
                List<Thing> things = cell.GetThingList(Map);

                if (targetThing != null && !targetThing.Destroyed && things.Contains(targetThing))
                {
                    hitThing = targetThing;
                    return cell;
                }

                Pawn pawn = things.OfType<Pawn>().FirstOrDefault(candidate => candidate != caster && !candidate.Dead);
                if (pawn != null)
                {
                    hitThing = pawn;
                    return cell;
                }

                Building edifice = cell.GetEdifice(Map);
                if (edifice != null && edifice.def.Fillage == FillCategory.Full)
                {
                    hitThing = edifice;
                    return cell;
                }

                if (cell == targetCell)
                {
                    hitThing = things.FirstOrDefault(IsDamageableTarget);
                    return cell;
                }
            }

            return lastValid;
        }

        private void ApplyConeDamage()
        {
            List<IntVec3> cells = ConeCells(caster.Position, targetCell, CompAbilityEffect_OpticAttack.ConeRange,
                CompAbilityEffect_OpticAttack.ConeAngle, Map);
            Vector3 forward = (targetCell - caster.Position).ToVector3().Yto0().normalized;
            HashSet<Thing> hitThings = new HashSet<Thing>();

            foreach (IntVec3 cell in cells)
            {
                foreach (Thing thing in cell.GetThingList(Map).ToList())
                {
                    if (thing == caster || thing.Destroyed || !IsDamageableTarget(thing) || !hitThings.Add(thing))
                    {
                        continue;
                    }

                    Vector3 offset = (thing.Position - caster.Position).ToVector3().Yto0();
                    float distanceFactor = Mathf.Lerp(1f, 0.65f,
                        Mathf.Clamp01(offset.magnitude / Mathf.Max(CompAbilityEffect_OpticAttack.ConeRange, 1f)));
                    float angleFactor = Mathf.Lerp(1f, 0.75f,
                        Mathf.Clamp01(Vector3.Angle(forward, offset.normalized) /
                            Mathf.Max(CompAbilityEffect_OpticAttack.ConeAngle * 0.5f, 1f)));
                    float finalDamage = damage * distanceFactor * angleFactor;
                    if (thing is Building)
                    {
                        finalDamage *= 1.25f;
                    }

                    ApplyDamage(thing, finalDamage, stunTicks);
                }

                if (Rand.Chance(0.18f))
                {
                    SpawnImpactFlecks(cell, 0.5f);
                }
            }
        }

        private void ApplyDamage(Thing thing, float amount, int stunDuration)
        {
            if (thing == null || thing.Destroyed || amount <= 0f)
            {
                return;
            }

            float angle = (thing.Position - caster.Position).AngleFlat;
            thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, amount, armorPenetration, angle, caster,
                weapon: null, intendedTarget: thing));

            if (stunDuration > 0 && thing is Pawn pawn && !pawn.Dead && pawn.stances?.stunner != null)
            {
                pawn.stances.stunner.StunFor(stunDuration, caster, addBattleLog: false);
            }
        }

        private static bool IsDamageableTarget(Thing thing)
        {
            if (thing is Pawn)
            {
                return true;
            }

            return thing != null && thing.def.useHitPoints
                && (thing is Building || thing is Plant || thing.def.category == ThingCategory.Item);
        }

        private Vector3 BeamStart(IntVec3 endCell)
        {
            Vector3 start = caster.DrawPos;
            Vector3 direction = (endCell.ToVector3Shifted() - start).Yto0().normalized;
            start += direction * (caster.RaceProps?.Humanlike == true ? 0.42f : 0.3f);
            start.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            return start;
        }

        private void DrawBeam(Vector3 start, Vector3 end, float coreWidth, float alpha)
        {
            end.y = start.y;
            GenDraw.DrawLineBetween(start, end, BeamGlowMat, Mathf.Max(coreWidth * 3.4f, 0.05f) * alpha);
            GenDraw.DrawLineBetween(start, end, BeamCoreMat, Mathf.Max(coreWidth, 0.025f) * alpha);
        }

        private void DrawImpact(Vector3 position, float scale)
        {
            position.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.01f;
            float rotation = (ageTicks * 13 + thingIDNumber) % 360;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(position,
                Quaternion.AngleAxis(rotation, Vector3.up), new Vector3(scale, 1f, scale)), ImpactMat, 0);
        }

        private void DrawCone(float alpha)
        {
            Vector3 origin = caster.DrawPos;
            Vector3 direction = (targetCell.ToVector3Shifted() - origin).Yto0().normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                return;
            }

            float range = CompAbilityEffect_OpticAttack.ConeRange;
            float width = Mathf.Tan(CompAbilityEffect_OpticAttack.ConeAngle * 0.5f * Mathf.Deg2Rad)
                * range * 2f;
            Vector3 start = origin + direction * (caster.RaceProps?.Humanlike == true ? 0.42f : 0.3f);
            Vector3 center = start + direction * (range * 0.5f);
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.005f;

            float pulse = 1f + Mathf.Sin((ageTicks + thingIDNumber) * 0.28f) * 0.025f;
            float fadeStep = Mathf.Ceil(Mathf.Clamp01(alpha) * 8f) / 8f;
            Material fanMaterial = MaterialPool.MatFrom(ConeFanTexturePath, ShaderDatabase.Transparent,
                new Color(1f, 1f, 1f, 0.72f * fadeStep));

            Graphics.DrawMesh(MeshPool.plane10,
                Matrix4x4.TRS(center, Quaternion.AngleAxis(direction.AngleFlat(), Vector3.up),
                    new Vector3(width * pulse, 1f, range * pulse)), fanMaterial, 0);
        }

        private void SpawnImpactFlecks(IntVec3 cell, float scale)
        {
            if (!cell.InBounds(Map))
            {
                return;
            }

            FleckMaker.Static(cell.ToVector3Shifted(), Map, FleckDefOf.ShotFlash, scale);
            if (Rand.Chance(0.7f))
            {
                FleckMaker.Static(cell.ToVector3Shifted(), Map, FleckDefOf.MicroSparks, scale * 0.8f);
            }
        }

        public static List<IntVec3> ConeCells(IntVec3 origin, IntVec3 target, float range, float angle, Map map)
        {
            List<IntVec3> result = new List<IntVec3>();
            Vector3 forward = (target - origin).ToVector3().Yto0();
            if (forward.sqrMagnitude < 0.001f)
            {
                return result;
            }

            forward.Normalize();
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(origin, range, useCenter: false))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                Vector3 offset = (cell - origin).ToVector3().Yto0();
                if (offset.sqrMagnitude > range * range || Vector3.Angle(forward, offset) > angle * 0.5f)
                {
                    continue;
                }

                if (GenSight.LineOfSight(origin, cell, map, skipFirstCell: true))
                {
                    result.Add(cell);
                }
            }

            return result;
        }
    }
}
