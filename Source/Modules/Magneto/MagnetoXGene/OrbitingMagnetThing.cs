using UnityEngine;
using Verse;

namespace MagnetoXGene
{
    public class OrbitingMagnetThing : Thing
    {
        private Thing payload;
        private Pawn owner;
        private HediffComp_Magnetism parentComp;
        private float angle;
        private float radius;
        private float bobTimer;
        private bool parentRemoved;

        public void Initialize(Thing payload, Pawn owner, HediffComp_Magnetism parent, float initialAngle, float orbitRadius)
        {
            this.payload = payload;
            this.owner = owner;
            parentComp = parent;
            angle = initialAngle;
            radius = orbitRadius;
            bobTimer = Rand.Range(0f, 6.28f);
        }

        protected override void Tick()
        {
            base.Tick();

            if (owner == null || parentComp == null || owner.Map != Map || parentComp.Pawn != owner || payload == null)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }

            float orbitSpeed = Mathf.Max(0.01f, MagnetoMod.ActiveSettings?.orbitSpeedDegreesPerTick ?? 6f);
            angle += orbitSpeed;
            if (angle >= 360f)
            {
                angle -= 360f;
            }

            bobTimer += 0.1f;

            if (Spawned && Position != owner.Position)
            {
                Position = owner.Position;
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                if (owner == null)
                {
                    return base.DrawPos;
                }

                Vector3 center = owner.DrawPos;
                float rad = angle * Mathf.Deg2Rad;
                float distanceScale = Mathf.Max(0.1f, MagnetoMod.ActiveSettings?.orbitDistanceMultiplier ?? 1f);
                float effectiveRadius = radius * distanceScale;
                Vector3 offset = new Vector3(Mathf.Cos(rad) * effectiveRadius, 0f, Mathf.Sin(rad) * effectiveRadius);
                Vector3 result = center + offset;
                result.y = AltitudeLayer.MoteOverhead.AltitudeFor() + 0.02f * Mathf.Sin(bobTimer);
                return result;
            }
        }

        public override Graphic Graphic => payload != null ? payload.Graphic : base.Graphic;

        public override Vector2 DrawSize => payload?.def.graphicData?.drawSize ?? base.DrawSize;

        public override Color DrawColor => payload != null ? payload.DrawColor : base.DrawColor;

        public override Color DrawColorTwo => payload != null ? payload.DrawColorTwo : base.DrawColorTwo;

        public void SetRadius(float newRadius)
        {
            radius = newRadius;
        }

        public void SetAngle(float newAngle)
        {
            angle = newAngle;
        }

        public void MarkParentRemoved()
        {
            parentRemoved = true;
            parentComp = null;
            payload = null;
            owner = null;
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Destroyed)
            {
                base.Destroy(mode);
                return;
            }

            bool notify = !parentRemoved && parentComp != null && payload != null;

            base.Destroy(mode);

            if (notify)
            {
                parentComp.NotifyVisualRemoved(payload);
            }

            payload = null;
            owner = null;
            parentComp = null;
        }
    }
}
