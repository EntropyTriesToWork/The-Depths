// ============================================================
//  DeckManager.cs
//  Manages the four card zones during combat:
//    Draw Pile → Hand → Discard Pile → (reshuffled back to Draw)
//                          ↓
//                     Exhaust Pile (permanent removal for the combat)
//
//  This is a MonoBehaviour so you can attach it to a
//  CombatManager GameObject in the scene.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class DeckManager : MonoBehaviour
    {
        // ----------------------------------------------------------
        // Card zones
        // ----------------------------------------------------------

        private List<CardInstance> _drawPile    = new List<CardInstance>();
        private List<CardInstance> _hand        = new List<CardInstance>();
        private List<CardInstance> _discardPile = new List<CardInstance>();
        private List<CardInstance> _exhaustPile = new List<CardInstance>();

        // ----------------------------------------------------------
        // Settings
        // ----------------------------------------------------------

        [Tooltip("Maximum number of cards the player can hold in hand.")]
        public int maxHandSize = 10;

        // ----------------------------------------------------------
        // Read-only views (UI can read these safely)
        // ----------------------------------------------------------

        public IReadOnlyList<CardInstance> Hand        => _hand;
        public IReadOnlyList<CardInstance> DrawPile    => _drawPile;
        public IReadOnlyList<CardInstance> DiscardPile => _discardPile;
        public IReadOnlyList<CardInstance> ExhaustPile => _exhaustPile;

        public int HandCount    => _hand.Count;
        public int DrawCount    => _drawPile.Count;
        public int DiscardCount => _discardPile.Count;

        // ----------------------------------------------------------
        // Events (subscribe for UI / animation hooks)
        // ----------------------------------------------------------

        public event Action<CardInstance>        OnCardDrawn;
        public event Action<CardInstance>        OnCardDiscarded;
        public event Action<CardInstance>        OnCardExhausted;
        public event Action<CardInstance>        OnCardPlayed;
        public event Action                      OnDeckReshuffled;

        // ----------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------

        /// <summary>
        /// Call at the start of each combat with the player's current deck.
        /// Clears all zones and loads a fresh shuffled draw pile.
        /// </summary>
        public void InitializeCombat(List<CardInstance> deck)
        {
            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            _exhaustPile.Clear();

            _drawPile.AddRange(deck);
            Shuffle(_drawPile);

            // Reset per-combat tracking on all cards
            foreach (var card in _drawPile)
                card.ResetCombatTracking();
        }

        // ----------------------------------------------------------
        // Drawing
        // ----------------------------------------------------------

        /// <summary>Draw N cards. Returns actually drawn cards.</summary>
        public List<CardInstance> DrawCards(int count)
        {
            var drawn = new List<CardInstance>();
            for (int i = 0; i < count; i++)
            {
                if (_hand.Count >= maxHandSize) break;

                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break;
                    ReshuffleDiscardIntoDraw();
                }

                if (_drawPile.Count == 0) break;

                var card = _drawPile[_drawPile.Count - 1];
                _drawPile.RemoveAt(_drawPile.Count - 1);
                _hand.Add(card);
                drawn.Add(card);
                OnCardDrawn?.Invoke(card);
            }
            return drawn;
        }

        public CardInstance DrawOne() =>
            DrawCards(1).Count > 0 ? DrawCards(1)[0] : null;

        // ----------------------------------------------------------
        // Playing
        // ----------------------------------------------------------

        /// <summary>
        /// Moves the card from hand to the appropriate destination.
        /// Called by CombatManager after effects have been executed.
        /// </summary>
        public void ResolveCardAfterPlay(CardInstance card)
        {
            _hand.Remove(card);
            card.RecordPlay();
            OnCardPlayed?.Invoke(card);

            if (card.Data.exhaust)
                ExhaustCard(card);
            else
                _discardPile.Add(card);
        }

        // ----------------------------------------------------------
        // Discarding
        // ----------------------------------------------------------

        public void DiscardCard(CardInstance card)
        {
            _hand.Remove(card);
            _discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
        }

        public void DiscardHand()
        {
            // Retain check: cards with Retain stay in hand
            var toDiscard = new List<CardInstance>();
            foreach (var card in _hand)
            {
                if (card.Data.retain) continue;
                toDiscard.Add(card);
            }

            foreach (var card in toDiscard)
                DiscardCard(card);
        }

        // ----------------------------------------------------------
        // Exhausting
        // ----------------------------------------------------------

        public void ExhaustCard(CardInstance card)
        {
            _hand.Remove(card);
            _discardPile.Remove(card);
            card.SetExhausted(true);
            _exhaustPile.Add(card);
            OnCardExhausted?.Invoke(card);
        }

        // ----------------------------------------------------------
        // Reshuffling
        // ----------------------------------------------------------

        private void ReshuffleDiscardIntoDraw()
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
            OnDeckReshuffled?.Invoke();
        }

        // ----------------------------------------------------------
        // Adding / removing cards mid-combat
        // ----------------------------------------------------------

        /// <summary>Adds a card directly to hand (e.g. from an event or relic).</summary>
        public void AddToHand(CardInstance card)
        {
            if (_hand.Count >= maxHandSize)
            {
                _discardPile.Add(card);
                return;
            }
            _hand.Add(card);
            OnCardDrawn?.Invoke(card);
        }

        /// <summary>Adds a card to the top of the draw pile.</summary>
        public void AddToTopOfDraw(CardInstance card)
        {
            _drawPile.Add(card);
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------

        public bool IsInHand(CardInstance card) => _hand.Contains(card);

        public bool CanPlay(CardInstance card, int currentEnergy)
        {
            if (!IsInHand(card)) return false;
            if (card.Data.unplayable) return false;
            return currentEnergy >= card.GetEffectiveCost();
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ----------------------------------------------------------
        // End of combat cleanup
        // ----------------------------------------------------------

        /// <summary>
        /// Returns every card across all zones — used to reconstruct
        /// the player's full deck after combat.
        /// </summary>
        public List<CardInstance> GetAllCards()
        {
            var all = new List<CardInstance>();
            all.AddRange(_drawPile);
            all.AddRange(_hand);
            all.AddRange(_discardPile);
            all.AddRange(_exhaustPile);
            return all;
        }
    }
}
