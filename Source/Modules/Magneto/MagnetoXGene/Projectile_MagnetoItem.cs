using RimWorld;
using UnityEngine;
using Verse;

namespace MagnetoXGene
{
    public class Projectile_MagnetoItem : Projectile
    {
        private Thing payload;
        private float payloadDamage;
        private Graphic payloadGraphic;
        private Color colorOne = Color.white;
        private Color colorTwo = Color.white;
        private Vector2 drawSize = Vector2.one;

        public void Prepare(Thing thing)
        {
            payload = thing;
            payloadDamage = MagnetoUtility.ComputePayloadDamage(thing);
            payloadGraphic = thing.Graphic ?? thing.def?.graphicData?.GraphicColoredFor(thing);
            colorOne = thing.DrawColor;
            colorTwo = thing.DrawColorTwo;
            drawSize = thing.def?.graphicData?.drawSize ?? Vector2.one;
        }

        public override Graphic Graphic => payloadGraphic ?? base.Graphic;
        public override Vector2 DrawSize => payloadGraphic != null ? drawSize : base.DrawSize;
        public override Color DrawColor => payloadGraphic != null ? colorOne : base.DrawColor;
        public override Color DrawColorTwo => payloadGraphic != null ? colorTwo : base.DrawColorTwo;

        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = Map;
            IntVec3 position = Position;

            if (payload != null && map != null)
            {
                bool embed = hitThing is Pawn;

                if (hitThing != null && !blockedByShield)
                {
                    float armorPenetration = Mathf.Clamp(payloadDamage / 20f, 0.2f, 8f);
                    DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, payloadDamage, armorPenetration, ExactRotation.eulerAngles.y, launcher, null, equipmentDef);
                    hitThing.TakeDamage(dinfo);
                    embed &= !hitThing.Destroyed;
                }

                if (!embed)
                {
                    GenPlace.TryPlaceThing(payload, position, map, ThingPlaceMode.Near);
                }
                else
                {
                    payload.Destroy();
                }

                payload = null;
            }

            base.Impact(hitThing, blockedByShield);
        }
    }
}
