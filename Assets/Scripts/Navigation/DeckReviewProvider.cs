// ============================================================
//  DeckReviewProvider.cs
//  Read-only deck data available during navigation.
//
//  The player can open a deck review panel at any time on the
//  navigation screen. This class provides sorted, filtered,
//  and grouped views of the current deck without exposing
//  any mutable state.
//
//  Attach to the same GameObject as RunManager, or access
//  via RunManager.DeckReview after a run is started.
// ============================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardGame
{
    public class DeckReviewProvider
    {
        private RunState _runState;

        public DeckReviewProvider(RunState runState)
        {
            _runState = runState;
        }

        public void SetRunState(RunState runState) => _runState = runState;

        // ----------------------------------------------------------
        // Full deck views
        // ----------------------------------------------------------

        /// <summary>All cards in the deck, no filtering.</summary>
        public IReadOnlyList<CardInstance> GetAllCards() => _runState.Deck;

        /// <summary>Total card count.</summary>
        public int TotalCards => _runState.Deck.Count;

        // ----------------------------------------------------------
        // Sorted views
        // ----------------------------------------------------------

        /// <summary>Cards sorted by energy cost ascending, then name.</summary>
        public List<CardInstance> GetSortedByCost()
        {
            return _runState.Deck
                .OrderBy(c => c.GetEffectiveCost())
                .ThenBy(c => c.Data.cardName)
                .ToList();
        }

        /// <summary>Cards sorted by name alphabetically.</summary>
        public List<CardInstance> GetSortedByName()
        {
            return _runState.Deck
                .OrderBy(c => c.Data.cardName)
                .ToList();
        }

        /// <summary>Cards grouped by CardType (Attack, Skill, Power, Curse).</summary>
        public Dictionary<CardType, List<CardInstance>> GetGroupedByType()
        {
            var groups = new Dictionary<CardType, List<CardInstance>>();

            foreach (var card in _runState.Deck)
            {
                if (!groups.ContainsKey(card.Data.cardType))
                    groups[card.Data.cardType] = new List<CardInstance>();
                groups[card.Data.cardType].Add(card);
            }

            return groups;
        }

        // ----------------------------------------------------------
        // Filtered views
        // ----------------------------------------------------------

        public List<CardInstance> GetByType(CardType type)
        {
            return _runState.Deck
                .Where(c => c.Data.cardType == type)
                .OrderBy(c => c.Data.cardName)
                .ToList();
        }

        public List<CardInstance> GetByRarity(CardRarity rarity)
        {
            return _runState.Deck
                .Where(c => c.Data.rarity == rarity)
                .ToList();
        }

        public List<CardInstance> GetUpgradeable()
        {
            return _runState.Deck
                .Where(c => c.CanUpgrade())
                .ToList();
        }

        public List<CardInstance> GetUpgraded()
        {
            return _runState.Deck
                .Where(c => c.CurrentTier != UpgradeTier.Base)
                .ToList();
        }

        // ----------------------------------------------------------
        // Summary stats (for the deck review header)
        // ----------------------------------------------------------

        public DeckSummary GetSummary()
        {
            var summary = new DeckSummary();

            summary.TotalCards  = _runState.Deck.Count;

            foreach (var card in _runState.Deck)
            {
                summary.AttackCount  += card.Data.cardType == CardType.Attack  ? 1 : 0;
                summary.SkillCount   += card.Data.cardType == CardType.Skill   ? 1 : 0;
                summary.PowerCount   += card.Data.cardType == CardType.Power   ? 1 : 0;
                summary.CurseCount   += card.Data.cardType == CardType.Curse   ? 1 : 0;
                summary.UpgradedCount += card.CurrentTier != UpgradeTier.Base  ? 1 : 0;

                if (card.Data.energyCost >= 0)
                    summary.TotalEnergyCost += card.Data.energyCost;
            }

            summary.AverageEnergyCost = summary.TotalCards > 0
                ? (float)summary.TotalEnergyCost / summary.TotalCards
                : 0f;

            return summary;
        }
    }

    // ----------------------------------------------------------
    //  DeckSummary — plain data container for the review panel header
    // ----------------------------------------------------------

    public class DeckSummary
    {
        public int   TotalCards       { get; set; }
        public int   AttackCount      { get; set; }
        public int   SkillCount       { get; set; }
        public int   PowerCount       { get; set; }
        public int   CurseCount       { get; set; }
        public int   UpgradedCount    { get; set; }
        public int   TotalEnergyCost  { get; set; }
        public float AverageEnergyCost { get; set; }

        public override string ToString() =>
            $"{TotalCards} cards | {AttackCount}A {SkillCount}S {PowerCount}P {CurseCount}C " +
            $"| {UpgradedCount} upgraded | avg cost {AverageEnergyCost:F1}";
    }
}
