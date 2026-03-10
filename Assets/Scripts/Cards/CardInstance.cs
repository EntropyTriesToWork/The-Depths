// ============================================================
//  CardInstance.cs
//  A runtime wrapper around a CardData asset.
//
//  WHY this exists:
//    CardData is immutable shared data (ScriptableObject).
//    CardInstance is the "live" copy during a run — it tracks
//    per-run state like how many times the card has been played,
//    any temporary modifiers, and its current upgrade tier.
//
//  You never modify CardData directly. You upgrade a CardInstance
//  by swapping which CardData it points to.
// ============================================================

using System;
using UnityEngine;

namespace CardGame
{
    [Serializable]
    public class CardInstance
    {
        // ----------------------------------------------------------
        // Data reference
        // ----------------------------------------------------------

        /// <summary>
        /// The underlying definition for this card at its current tier.
        /// Upgrading = swapping this reference to cardData.tier1Version etc.
        /// </summary>
        public CardData Data { get; private set; }

        // ----------------------------------------------------------
        // Per-run tracking
        // ----------------------------------------------------------

        /// <summary>How many times this card has been played this run.</summary>
        public int TimesPlayedThisRun { get; private set; }

        /// <summary>How many times this card has been played this combat.</summary>
        public int TimesPlayedThisCombat { get; private set; }

        // ----------------------------------------------------------
        // Temporary modifiers (applied by relics/events for one combat)
        // ----------------------------------------------------------

        public int TemporaryCostReduction { get; private set; }
        public int TemporaryDamageBonus   { get; private set; }
        public int TemporaryBlockBonus    { get; private set; }

        // ----------------------------------------------------------
        // State flags
        // ----------------------------------------------------------

        /// <summary>True if this card is currently in the exhaust pile.</summary>
        public bool IsExhausted { get; private set; }

        // ----------------------------------------------------------
        // Unique runtime ID (useful for UI tracking)
        // ----------------------------------------------------------

        public string InstanceID { get; private set; }

        // ----------------------------------------------------------
        // Events
        // ----------------------------------------------------------

        public event Action<CardInstance> OnUpgraded;

        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------

        public CardInstance(CardData data)
        {
            Data       = data;
            InstanceID = Guid.NewGuid().ToString("N")[..8]; // Short unique ID
        }

        // ----------------------------------------------------------
        // Upgrade
        // ----------------------------------------------------------

        /// <summary>
        /// Upgrades this card instance to the next tier.
        /// Returns true if successful.
        /// </summary>
        public bool Upgrade()
        {
            CardData next = Data.GetNextTier();
            if (next == null) return false;

            Data = next;
            OnUpgraded?.Invoke(this);
            return true;
        }

        public bool CanUpgrade() => Data.CanUpgrade();
        public UpgradeTier CurrentTier => Data.tier;

        // ----------------------------------------------------------
        // Cost resolution (considers temporary reductions)
        // ----------------------------------------------------------

        public int GetEffectiveCost()
        {
            if (Data.energyCost < 0) return Data.energyCost;    // X-cost, handled separately
            return Mathf.Max(0, Data.energyCost - TemporaryCostReduction);
        }

        // ----------------------------------------------------------
        // Play tracking
        // ----------------------------------------------------------

        public void RecordPlay()
        {
            TimesPlayedThisRun++;
            TimesPlayedThisCombat++;
        }

        public void ResetCombatTracking()
        {
            TimesPlayedThisCombat = 0;
            TemporaryCostReduction = 0;
            TemporaryDamageBonus   = 0;
            TemporaryBlockBonus    = 0;
        }

        // ----------------------------------------------------------
        // Temporary modifiers
        // ----------------------------------------------------------

        public void AddTemporaryCostReduction(int amount) =>
            TemporaryCostReduction += amount;

        public void AddTemporaryDamageBonus(int amount) =>
            TemporaryDamageBonus += amount;

        public void AddTemporaryBlockBonus(int amount) =>
            TemporaryBlockBonus += amount;

        // ----------------------------------------------------------
        // Exhaust state
        // ----------------------------------------------------------

        public void SetExhausted(bool value) => IsExhausted = value;

        // ----------------------------------------------------------
        // Serialization helpers (for saving mid-run)
        // ----------------------------------------------------------

        public CardSaveData ToSaveData()
        {
            return new CardSaveData
            {
                cardID           = Data.cardID,
                tier             = (int)Data.tier,
                timesPlayedRun   = TimesPlayedThisRun
            };
        }
    }

    // ----------------------------------------------------------
    // Minimal save data container
    // ----------------------------------------------------------

    [Serializable]
    public class CardSaveData
    {
        public string cardID;
        public int    tier;
        public int    timesPlayedRun;
    }
}
