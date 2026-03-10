// ============================================================
//  CardData.cs
//  The ScriptableObject asset that defines a card.
//  This is pure DATA — no runtime logic lives here.
//
//  To create a new card in Unity:
//    Right-click in Project → Create → CardGame → Card Data
//  Then fill in the fields and drag in CardEffect assets.
// ============================================================

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Card Data", fileName = "NewCard")]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        public string cardName        = "Unnamed Card";
        public string cardID          = "";          // Unique string key, e.g. "warrior_strike_t0"
        public CardType cardType      = CardType.Attack;
        public CardRarity rarity      = CardRarity.Common;
        public CharacterClass owner   = CharacterClass.Neutral;

        [Header("Cost")]
        [Tooltip("Energy cost to play this card. Use -1 for X-cost cards.")]
        public int energyCost = 1;

        [Tooltip("If true, this card costs 0 to play this turn after being drawn.")]
        public bool innate = false;

        [Tooltip("If true, this card is removed from the deck after being played.")]
        public bool exhaust = false;

        [Tooltip("If true, this card stays in hand at end of turn instead of discarding.")]
        public bool retain = false;

        [Tooltip("If true, this card is unplayable (Curses, status cards).")]
        public bool unplayable = false;

        [Header("Effects")]
        [Tooltip("All effects attached to this card. They fire based on each effect's trigger setting.")]
        public CardEffect[] effects = new CardEffect[0];

        [Header("Upgrade Chain")]
        public UpgradeTier tier = UpgradeTier.Base;

        [Tooltip("The Tier 1 upgraded version of this card. Leave null if this IS Tier 1 or 2.")]
        public CardData tier1Version;

        [Tooltip("The Tier 2 upgraded version of this card. Leave null if this IS Tier 2.")]
        public CardData tier2Version;

        [Header("Visuals")]
        public Sprite cardArtwork;
        public Sprite cardFrame;           // Different frame per rarity/type

        [Header("Unlock")]
        [Tooltip("Set to empty string for cards that are always available.")]
        public string unlockID = "";

        public bool IsAlwaysUnlocked => string.IsNullOrEmpty(unlockID);

        /// <summary>
        /// Returns the next upgrade tier, or null if already at max.
        /// </summary>
        public CardData GetNextTier()
        {
            switch (tier)
            {
                case UpgradeTier.Base:  return tier1Version;
                case UpgradeTier.Tier1: return tier2Version;
                default:               return null;
            }
        }

        public bool CanUpgrade() => GetNextTier() != null;

        /// <summary>
        /// Returns all OnPlay effects — the most common query.
        /// </summary>
        public IEnumerable<CardEffect> GetOnPlayEffects()
        {
            foreach (var effect in effects)
                if (effect != null && effect.trigger == EffectTrigger.OnPlay)
                    yield return effect;
        }

        /// <summary>
        /// Returns all effects matching a given trigger.
        /// </summary>
        public IEnumerable<CardEffect> GetEffectsForTrigger(EffectTrigger trigger)
        {
            foreach (var effect in effects)
                if (effect != null && effect.trigger == trigger)
                    yield return effect;
        }

        /// <summary>
        /// Builds the card's tooltip description by concatenating
        /// each effect's GetDescription() output. Called by the UI.
        /// </summary>
        public string BuildDescription()
        {
            var sb = new StringBuilder();
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                string line = effect.GetDescription();
                if (!string.IsNullOrEmpty(line))
                {
                    if (sb.Length > 0) sb.Append("\n");
                    sb.Append(line);
                }
            }

            // Append keyword tags
            if (exhaust)  sb.Append("\n<b>Exhaust</b>");
            if (retain)   sb.Append("\n<b>Retain</b>");
            if (innate)   sb.Append("\n<b>Innate</b>");

            return sb.ToString();
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate cardID from name if empty
            if (string.IsNullOrEmpty(cardID) && !string.IsNullOrEmpty(cardName))
            {
                cardID = $"{owner.ToString().ToLower()}_{cardName.ToLower().Replace(" ", "_")}_{(int)tier}";
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
