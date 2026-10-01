using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GambitXGene;

public class GambitMod : Mod
{
    public static GambitSettings Settings { get; private set; } = null!;

    public GambitMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<GambitSettings>();
        LongEventHandler.ExecuteWhenFinished(() => Settings.ApplyTuning());
        MarvelUnification.MarvelHarmony.EnsurePatched();
    }

    public override string SettingsCategory() => "Gambit X-Gene";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label("Card Arsenal");
        listing.Label($"Targeting range: {Settings.pickupRange:F0} cells");
        Settings.pickupRange = listing.Slider(Settings.pickupRange, GambitSettings.MinPickupRange, GambitSettings.MaxPickupRange);
        listing.Label($"Explosion radius: {Settings.explosionRadius:F1}");
        Settings.explosionRadius = listing.Slider(Settings.explosionRadius, GambitSettings.MinExplosionRadius, GambitSettings.MaxExplosionRadius);
        listing.Label($"Card damage: {Settings.cardDamage:F0}");
        Settings.cardDamage = Mathf.Round(listing.Slider(Settings.cardDamage, GambitSettings.MinCardDamage, GambitSettings.MaxCardDamage));
        listing.Label($"Recharge interval: {Settings.rechargeIntervalSeconds:F1}s per card");
        Settings.rechargeIntervalSeconds = listing.Slider(Settings.rechargeIntervalSeconds, GambitSettings.MinRechargeInterval, GambitSettings.MaxRechargeInterval);
        listing.Label($"Volley rate: {Settings.volleyShotsPerSecond:F1} cards/s");
        Settings.volleyShotsPerSecond = listing.Slider(Settings.volleyShotsPerSecond, GambitSettings.MinVolleyRate, GambitSettings.MaxVolleyRate);

        listing.GapLine();
        listing.Label("Touch-Charge");
        listing.Label($"Radius: {Settings.touchChargeRadius:F1}");
        Settings.touchChargeRadius = listing.Slider(Settings.touchChargeRadius, GambitSettings.MinTouchRadius, GambitSettings.MaxTouchRadius);
        listing.Label($"Damage: {Settings.touchChargeDamage:F0}");
        Settings.touchChargeDamage = Mathf.RoundToInt(listing.Slider(Settings.touchChargeDamage, GambitSettings.MinTouchDamage, GambitSettings.MaxTouchDamage));

        listing.GapLine();
        listing.Label("Kinetic Melee");
        listing.Label($"Radius: {Settings.kineticRadius:F1}");
        Settings.kineticRadius = listing.Slider(Settings.kineticRadius, GambitSettings.MinKineticRadius, GambitSettings.MaxKineticRadius);
        listing.Label($"Damage: {Settings.kineticDamage:F0}");
        Settings.kineticDamage = Mathf.RoundToInt(listing.Slider(Settings.kineticDamage, GambitSettings.MinKineticDamage, GambitSettings.MaxKineticDamage));
        listing.Label($"Armor Pen: {Settings.kineticArmorPen:P0}");
        Settings.kineticArmorPen = listing.Slider(Settings.kineticArmorPen, GambitSettings.MinArmorPen, GambitSettings.MaxArmorPen);
        listing.Label($"Per-target cooldown: {Settings.kineticPerTargetCooldown:F1}s");
        Settings.kineticPerTargetCooldown = listing.Slider(Settings.kineticPerTargetCooldown, GambitSettings.MinKineticCooldown, GambitSettings.MaxKineticCooldown);

        listing.CheckboxLabeled("Ignore friendly pawns", ref Settings.kineticIgnoreFriendlyFire,
            "When enabled, kinetic micro-explosions spare allies; otherwise everyone nearby is fair game.");

        listing.End();

        Settings.ApplyTuning();
    }
}

public class GambitSettings : ModSettings
{
    public const float MinPickupRange = 18f;
    public const float MaxPickupRange = 42f;
    public const float MinExplosionRadius = 1.2f;
    public const float MaxExplosionRadius = 2.4f;
    public const float MinCardDamage = 12f;
    public const float MaxCardDamage = 18f;
    public const float MinRechargeInterval = 2f;
    public const float MaxRechargeInterval = 10f;
    public const float MinTouchRadius = 1.9f;
    public const float MaxTouchRadius = 2.6f;
    public const float MinTouchDamage = 20f;
    public const float MaxTouchDamage = 35f;
    public const float MinKineticRadius = 0.7f;
    public const float MaxKineticRadius = 1.2f;
    public const float MinKineticDamage = 5f;
    public const float MaxKineticDamage = 12f;
    public const float MinArmorPen = 0.05f;
    public const float MaxArmorPen = 0.12f;
    public const float MinKineticCooldown = 0.1f;
    public const float MaxKineticCooldown = 0.8f;
    public const float MinVolleyRate = 6f;
    public const float MaxVolleyRate = 14f;

    public float pickupRange = 32f;
    public float explosionRadius = 1.6f;
    public float cardDamage = 16f;
    public float rechargeIntervalSeconds = 5f;
    public float volleyShotsPerSecond = 10f;
    public float touchChargeRadius = 2.2f;
    public int touchChargeDamage = 25;
    public float kineticRadius = 0.9f;
    public int kineticDamage = 8;
    public float kineticArmorPen = 0.08f;
    public float kineticPerTargetCooldown = 0.3f;
    public bool kineticIgnoreFriendlyFire;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref pickupRange, nameof(pickupRange), 32f);
        Scribe_Values.Look(ref explosionRadius, nameof(explosionRadius), 1.6f);
        Scribe_Values.Look(ref cardDamage, nameof(cardDamage), 16f);
        Scribe_Values.Look(ref rechargeIntervalSeconds, nameof(rechargeIntervalSeconds), 5f);
        Scribe_Values.Look(ref volleyShotsPerSecond, nameof(volleyShotsPerSecond), 10f);
        Scribe_Values.Look(ref touchChargeRadius, nameof(touchChargeRadius), 2.2f);
        Scribe_Values.Look(ref touchChargeDamage, nameof(touchChargeDamage), 25);
        Scribe_Values.Look(ref kineticRadius, nameof(kineticRadius), 0.9f);
        Scribe_Values.Look(ref kineticDamage, nameof(kineticDamage), 8);
        Scribe_Values.Look(ref kineticArmorPen, nameof(kineticArmorPen), 0.08f);
        Scribe_Values.Look(ref kineticPerTargetCooldown, nameof(kineticPerTargetCooldown), 0.3f);
        Scribe_Values.Look(ref kineticIgnoreFriendlyFire, nameof(kineticIgnoreFriendlyFire));

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            ApplyTuning();
        }
    }

    public void ApplyTuning()
    {
        ClampAll();
        if (GambitDefOf.Gambit_ThrowCard != null)
        {
            GambitDefOf.Gambit_ThrowCard.verbProperties.range = pickupRange;
        }

        if (GambitDefOf.Gambit_52CardPickup != null)
        {
            GambitDefOf.Gambit_52CardPickup.verbProperties.range = pickupRange;
        }
    }

    private void ClampAll()
    {
        pickupRange = Mathf.Clamp(pickupRange, MinPickupRange, MaxPickupRange);
        explosionRadius = Mathf.Clamp(explosionRadius, MinExplosionRadius, MaxExplosionRadius);
        cardDamage = Mathf.Clamp(cardDamage, MinCardDamage, MaxCardDamage);
        rechargeIntervalSeconds = Mathf.Clamp(rechargeIntervalSeconds, MinRechargeInterval, MaxRechargeInterval);
        volleyShotsPerSecond = Mathf.Clamp(volleyShotsPerSecond, MinVolleyRate, MaxVolleyRate);
        touchChargeRadius = Mathf.Clamp(touchChargeRadius, MinTouchRadius, MaxTouchRadius);
        touchChargeDamage = Mathf.Clamp(touchChargeDamage, Mathf.RoundToInt(MinTouchDamage), Mathf.RoundToInt(MaxTouchDamage));
        kineticRadius = Mathf.Clamp(kineticRadius, MinKineticRadius, MaxKineticRadius);
        kineticDamage = Mathf.Clamp(kineticDamage, Mathf.RoundToInt(MinKineticDamage), Mathf.RoundToInt(MaxKineticDamage));
        kineticArmorPen = Mathf.Clamp(kineticArmorPen, MinArmorPen, MaxArmorPen);
        kineticPerTargetCooldown = Mathf.Clamp(kineticPerTargetCooldown, MinKineticCooldown, MaxKineticCooldown);
    }
}
