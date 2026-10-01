using GambitXGene.Comps;
using UnityEngine;
using Verse;

namespace GambitXGene.Gizmos;

[StaticConstructorOnStartup]
public class Gizmo_CardCounter : Gizmo
{
    private readonly HediffComp_GambitDeck deck;

    public Gizmo_CardCounter(HediffComp_GambitDeck deck)
    {
        this.deck = deck;
        Order = -100f;
    }

    public override float GetWidth(float maxWidth) => Mathf.Min(200f, maxWidth);

    public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
    {
        var width = GetWidth(maxWidth);
        var rect = new Rect(topLeft.x, topLeft.y, width, 75f);
        Widgets.DrawWindowBackground(rect);

        var labelRect = new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, 24f);
        Widgets.Label(labelRect, $"Cards {deck.Cards}/{deck.MaxCards}");

        var barRect = new Rect(rect.x + 6f, rect.y + 34f, rect.width - 12f, 20f);
        var fillPercent = deck.MaxCards > 0 ? deck.Cards / (float)deck.MaxCards : 0f;
        Widgets.FillableBar(barRect, fillPercent, FillTex, BaseContent.GreyTex, false);

        var tooltip = $"Recharge: {(GambitMod.Settings?.rechargeIntervalSeconds ?? 5f):F1}s per card\n" +
                      $"Recharge {(deck.CanRecharge ? "active" : "paused")}";
        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
            TooltipHandler.TipRegion(rect, tooltip);
        }

        var oldAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(barRect, $"{fillPercent * 100f:0}%");
        Text.Anchor = oldAnchor;

        return new GizmoResult(GizmoState.Clear);
    }
    private static readonly Texture2D FillTex;

    static Gizmo_CardCounter()
    {
        FillTex = new Texture2D(1, 1);
        FillTex.SetPixel(0, 0, new Color(0.87f, 0.1f, 0.42f));
        FillTex.Apply();
    }
}
