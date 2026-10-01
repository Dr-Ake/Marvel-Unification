using System.Collections.Generic;
using GambitXGene.Gizmos;
using UnityEngine;
using Verse;

namespace GambitXGene.Comps;

public class HediffCompProperties_GambitDeck : HediffCompProperties
{
    public int maxCards = 52;
    public int startingCards = 52;

    public HediffCompProperties_GambitDeck()
    {
        compClass = typeof(HediffComp_GambitDeck);
    }
}

public class HediffComp_GambitDeck : HediffComp
{
    private int cards;
    private int ticksUntilNextCard;
    private bool rechargePaused;
    private bool kineticMeleeEnabled;
    private Dictionary<int, int> meleeCooldownTicks = new();

    public HediffCompProperties_GambitDeck Props => (HediffCompProperties_GambitDeck)props;
    public new Pawn Pawn => parent.pawn;
    public int Cards => cards;
    public int MaxCards => Props.maxCards;
    public bool HasFullDeck => cards >= MaxCards;
    public bool KineticMeleeEnabled => kineticMeleeEnabled;
    public bool CanRecharge => !rechargePaused && Pawn?.Spawned == true;

    public override void CompPostMake()
    {
        base.CompPostMake();
        cards = Mathf.Clamp(Props.startingCards, 0, MaxCards);
        ticksUntilNextCard = RechargeIntervalTicks;
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref cards, "cards", Props.startingCards);
        Scribe_Values.Look(ref ticksUntilNextCard, "ticksUntilNextCard");
        Scribe_Values.Look(ref rechargePaused, "rechargePaused");
        Scribe_Values.Look(ref kineticMeleeEnabled, "kineticMeleeEnabled");
        Scribe_Collections.Look(ref meleeCooldownTicks, "meleeCooldownTicks", LookMode.Value, LookMode.Value);
        meleeCooldownTicks ??= new Dictionary<int, int>();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (Pawn?.DestroyedOrNull() != false)
        {
            return;
        }

        if (Pawn.IsHashIntervalTick(250))
        {
            CleanupCooldowns();
        }

        if (!CanRecharge || cards >= MaxCards)
        {
            return;
        }

        ticksUntilNextCard--;
        if (ticksUntilNextCard > 0)
        {
            return;
        }

        cards++;
        ticksUntilNextCard = RechargeIntervalTicks;
    }

    public override IEnumerable<Gizmo> CompGetGizmos()
    {
        yield return new Gizmo_CardCounter(this);
    }

    public bool HasCards(int amount = 1) => cards >= amount;

    public bool TryConsumeCards(int amount)
    {
        if (!HasCards(amount))
        {
            return false;
        }

        cards -= amount;
        return true;
    }

    public void NotifyVolleyStarted() => rechargePaused = true;

    public void NotifyVolleyEnded()
    {
        rechargePaused = false;
        if (cards < MaxCards)
        {
            ticksUntilNextCard = Mathf.Max(ticksUntilNextCard, 1);
        }
    }

    public bool TryConsumeSingleCard()
    {
        if (!TryConsumeCards(1))
        {
            return false;
        }

        ticksUntilNextCard = RechargeIntervalTicks;
        return true;
    }

    public void ForceFillDeck()
    {
        cards = MaxCards;
        ticksUntilNextCard = 0;
    }

    public bool ReadyForKinetic(Thing? target)
    {
        if (!kineticMeleeEnabled)
        {
            return false;
        }

        var id = target != null ? target.thingIDNumber : Gen.HashCombineInt(Pawn.Position.GetHashCode(), Pawn.Map?.uniqueID ?? 0);
        var currentTick = Find.TickManager.TicksGame;
        if (meleeCooldownTicks.TryGetValue(id, out var nextTick) && currentTick < nextTick)
        {
            return false;
        }

        var settings = GambitMod.Settings;
        var cooldownTicks = Mathf.Max(1, Mathf.RoundToInt((settings?.kineticPerTargetCooldown ?? 0.3f) * 60f));
        meleeCooldownTicks[id] = currentTick + cooldownTicks;
        return true;
    }

    public void ToggleRechargePause(bool paused) => rechargePaused = paused;

    public void ToggleKinetic() => kineticMeleeEnabled = !kineticMeleeEnabled;

    public void SetKinetic(bool value) => kineticMeleeEnabled = value;

    private int RechargeIntervalTicks =>
        Mathf.Max(60, Mathf.RoundToInt((GambitMod.Settings?.rechargeIntervalSeconds ?? 5f) * 60f));

    private void CleanupCooldowns()
    {
        var currentTick = Find.TickManager.TicksGame;
        tmpKeys.Clear();
        foreach (var kvp in meleeCooldownTicks)
        {
            if (kvp.Value <= currentTick)
            {
                tmpKeys.Add(kvp.Key);
            }
        }

        foreach (var key in tmpKeys)
        {
            meleeCooldownTicks.Remove(key);
        }
    }

    private static readonly List<int> tmpKeys = new();
}
