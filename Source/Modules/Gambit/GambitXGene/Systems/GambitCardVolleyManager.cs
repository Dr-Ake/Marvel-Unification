using System.Collections.Generic;
using GambitXGene.Comps;
using GambitXGene.Utilities;
using UnityEngine;
using Verse;

namespace GambitXGene.Systems;

public class GambitCardVolleyManager : GameComponent
{
    private readonly List<CardVolley> activeVolleys = new();
    private readonly List<CardVolley> tmpRemove = new();

    public GambitCardVolleyManager(Game game)
    {
    }

    public static GambitCardVolleyManager Instance
    {
        get
        {
            var game = Current.Game;
            if (game == null)
            {
                throw new System.InvalidOperationException("Game not loaded");
            }

            var existing = game.GetComponent<GambitCardVolleyManager>();
            if (existing != null)
            {
                return existing;
            }

            var created = new GambitCardVolleyManager(game);
            game.components.Add(created);
            return created;
        }
    }

    public override void GameComponentTick()
    {
        if (activeVolleys.Count == 0)
        {
            return;
        }

        tmpRemove.Clear();
        foreach (var volley in activeVolleys)
        {
            if (!volley.Tick())
            {
                tmpRemove.Add(volley);
            }
        }

        foreach (var finished in tmpRemove)
        {
            activeVolleys.Remove(finished);
            finished.OnStop();
        }

        tmpRemove.Clear();
    }

    public void StartVolley(Pawn pawn, HediffComp_GambitDeck deck, LocalTargetInfo target)
    {
        if (pawn.Map == null || deck == null)
        {
            return;
        }

        CancelAllFor(pawn);

        var settings = GambitMod.Settings;
        var ticksPerShot = Mathf.Max(1, Mathf.RoundToInt(60f / (settings?.volleyShotsPerSecond ?? 10f)));
        var volley = new CardVolley(pawn, deck, target, ticksPerShot);
        deck.NotifyVolleyStarted();
        activeVolleys.Add(volley);
    }

    public void CancelAllFor(Pawn pawn)
    {
        tmpRemove.Clear();
        foreach (var volley in activeVolleys)
        {
            if (volley.Caster == pawn)
            {
                tmpRemove.Add(volley);
            }
        }

        foreach (var v in tmpRemove)
        {
            activeVolleys.Remove(v);
            v.OnStop();
        }

        tmpRemove.Clear();
    }

    private sealed class CardVolley
    {
        private readonly HediffComp_GambitDeck deck;
        private readonly IntVec3 initialCell;
        private readonly Thing? movingTarget;
        private readonly int ticksPerShot;
        private int ticksUntilNextShot;

        private IntVec3 lastKnownCell;

        public CardVolley(Pawn caster, HediffComp_GambitDeck deck, LocalTargetInfo target, int ticksPerShot)
        {
            Caster = caster;
            this.deck = deck;
            this.ticksPerShot = ticksPerShot;
            ticksUntilNextShot = 0;
            initialCell = target.Cell;
            movingTarget = target.Thing;
            lastKnownCell = initialCell;
        }

        public Pawn Caster { get; }

        public bool Tick()
        {
            if (Caster.DestroyedOrNull() || !Caster.Spawned || deck == null)
            {
                return false;
            }

            if (Caster.Downed || !Caster.Drafted)
            {
                return false;
            }

            if (Caster.pather?.Moving == true)
            {
                return false;
            }

            if (!deck.HasCards())
            {
                return false;
            }

            ticksUntilNextShot--;
            if (ticksUntilNextShot > 0)
            {
                return true;
            }

            if (!deck.TryConsumeSingleCard())
            {
                return false;
            }

            var map = Caster.Map;
            if (map == null)
            {
                return false;
            }

            var cell = ResolveCurrentCell();
            if (!cell.IsValid || !cell.InBounds(map))
            {
                return false;
            }

            GambitExplosionUtility.DoCardExplosion(Caster, cell);
            ticksUntilNextShot = ticksPerShot;
            return true;
        }

        public void OnStop()
        {
            deck?.NotifyVolleyEnded();
        }

        private IntVec3 ResolveCurrentCell()
        {
            if (movingTarget != null && movingTarget.Spawned)
            {
                lastKnownCell = movingTarget.Position;
            }
            else if (!lastKnownCell.IsValid)
            {
                lastKnownCell = initialCell;
            }

            return lastKnownCell;
        }
    }
}
