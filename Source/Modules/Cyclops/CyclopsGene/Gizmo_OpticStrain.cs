using RimWorld;
using UnityEngine;
using Verse;

namespace CyclopsGene
{
    [StaticConstructorOnStartup]
    public class Gizmo_OpticStrain : Gizmo
    {
        private readonly HediffComp_OpticController controller;

        private static readonly Texture2D EmptyBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.05f, 0.025f, 0.03f));

        private static readonly Texture2D StrainBarTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.72f, 0.05f, 0.04f));

        private static readonly Texture2D StrainPreviewTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(1f, 0.38f, 0.16f));

        public Gizmo_OpticStrain(HediffComp_OpticController controller)
        {
            this.controller = controller;
            Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 180f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect outer = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(outer);

            Rect inner = outer.ContractedBy(7f);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 24f), "CyclopsOpticStrain".Translate());

            Rect bar = new Rect(inner.x, inner.y + 31f, inner.width, 24f);
            Widgets.FillableBar(bar, controller.StrainPercent, StrainBarTex, EmptyBarTex, doBorder: true);

            if (MapGizmoUtility.LastMouseOverGizmo is Command_Ability command
                && command.Pawn == controller.Pawn
                && command.Ability.CompOfType<CompAbilityEffect_OpticAttack>() is CompAbilityEffect_OpticAttack optic)
            {
                float start = controller.StrainPercent;
                float end = Mathf.Clamp01((controller.Strain + optic.StrainCost) / controller.MaxStrain);
                if (end > start)
                {
                    Rect preview = bar.ContractedBy(3f);
                    float usableWidth = preview.width;
                    preview.xMin += usableWidth * start;
                    preview.width = usableWidth * (end - start);
                    GUI.DrawTexture(preview, StrainPreviewTex);
                }
            }

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bar, $"{controller.Strain:0} / {controller.MaxStrain:0}");
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(outer,
                "CyclopsOpticStrainTooltip".Translate(controller.RecoveryPerSecond.ToString("0.#")));
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
