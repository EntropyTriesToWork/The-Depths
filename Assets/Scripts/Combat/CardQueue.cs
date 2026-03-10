// ============================================================
//  CardQueue.cs
//  Handles sequential card execution with delays between plays.
//
//  WHY this exists separately from CombatManager:
//    CombatManager owns the turn state machine.
//    CardQueue owns the per-card execution timeline WITHIN a turn.
//    Keeping them separate means the queue can be paused, flushed,
//    or interrupted without touching the turn state machine.
//
//  HOW IT WORKS:
//    1. Player clicks a card → UI calls CombatManager.TryQueueCard()
//    2. CombatManager validates cost/state, adds to CardQueue
//    3. CardQueue processes entries one at a time via coroutine
//    4. Each entry: spend energy → execute effects → animate → next
//    5. If two cards are queued rapidly, they play sequentially
//       with a configurable delay between them
//
//  INTERRUPTIONS:
//    If a card effect kills the last enemy mid-queue, the queue
//    is flushed and CombatManager transitions to Victory.
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class CardQueue : MonoBehaviour
    {
        // ----------------------------------------------------------
        // Settings
        // ----------------------------------------------------------

        [Tooltip("Seconds between sequential card executions.")]
        public float delayBetweenCards = 0.15f;

        [Tooltip("If true, the queue pauses while waiting for a target selection.")]
        public bool pauseOnTargetRequest = true;

        // ----------------------------------------------------------
        // State
        // ----------------------------------------------------------

        public bool IsProcessing    { get; private set; }
        public bool IsPaused        { get; private set; }
        public int  QueuedCount     => _queue.Count;

        private Queue<QueuedCard>   _queue      = new Queue<QueuedCard>();
        private Coroutine           _processing;

        // ----------------------------------------------------------
        // Events
        // ----------------------------------------------------------

        /// <summary>Fires just before a card's effects execute.</summary>
        public event Action<CardInstance>  OnCardExecutionStart;

        /// <summary>Fires after a card's effects execute and it's moved to discard.</summary>
        public event Action<CardInstance>  OnCardExecutionEnd;

        /// <summary>Fires whenever the queue empties.</summary>
        public event Action                OnQueueEmpty;

        /// <summary>Fires if a card was rejected (not enough energy, wrong state).</summary>
        public event Action<CardInstance, string> OnCardRejected;

        // ----------------------------------------------------------
        // Dependencies (set by CombatManager)
        // ----------------------------------------------------------

        private CombatContext   _ctx;
        private DeckManager     _deckManager;
        private Func<bool>      _isCombatActive;    // Returns false if combat ended mid-queue

        public void Initialize(CombatContext ctx, DeckManager deckManager, Func<bool> isCombatActive)
        {
            _ctx            = ctx;
            _deckManager    = deckManager;
            _isCombatActive = isCombatActive;
        }

        // ----------------------------------------------------------
        // Enqueue
        // ----------------------------------------------------------

        /// <summary>
        /// Attempts to add a card to the execution queue.
        /// Returns false immediately if the card cannot be played.
        /// </summary>
        public bool TryEnqueue(CardInstance card, CombatEntity target)
        {
            // Validation — fail fast before adding to queue
            if (!_deckManager.IsInHand(card))
            {
                OnCardRejected?.Invoke(card, "Card not in hand");
                return false;
            }

            if (card.Data.unplayable)
            {
                OnCardRejected?.Invoke(card, "Card is unplayable");
                return false;
            }

            // Energy check: must account for cards already queued
            int reservedEnergy = CalculateReservedEnergy();
            int available      = _ctx.CurrentEnergy - reservedEnergy;

            if (available < card.GetEffectiveCost())
            {
                OnCardRejected?.Invoke(card, "Not enough energy");
                return false;
            }

            _queue.Enqueue(new QueuedCard(card, target));

            // Start processing if not already running
            if (!IsProcessing)
                _processing = StartCoroutine(ProcessQueue());

            return true;
        }

        // ----------------------------------------------------------
        // Queue processing
        // ----------------------------------------------------------

        private IEnumerator ProcessQueue()
        {
            IsProcessing = true;

            while (_queue.Count > 0)
            {
                // Stop if combat ended (e.g. player died from a status tick)
                if (_isCombatActive != null && !_isCombatActive())
                    break;

                // Wait if paused (e.g. waiting for target selection UI)
                while (IsPaused)
                    yield return null;

                var entry = _queue.Dequeue();

                // Re-validate energy at execution time (earlier cards may have spent it)
                if (_ctx.CurrentEnergy < entry.Card.GetEffectiveCost())
                {
                    OnCardRejected?.Invoke(entry.Card, "Energy consumed by earlier card");
                    continue;
                }

                // Re-validate card is still in hand (edge case: discard effects)
                if (!_deckManager.IsInHand(entry.Card))
                {
                    OnCardRejected?.Invoke(entry.Card, "Card left hand before execution");
                    continue;
                }

                yield return StartCoroutine(ExecuteCard(entry));

                // Brief pause between sequential cards (gives room for animations)
                if (_queue.Count > 0)
                    yield return new WaitForSeconds(delayBetweenCards);
            }

            IsProcessing = false;
            OnQueueEmpty?.Invoke();
        }

        private IEnumerator ExecuteCard(QueuedCard entry)
        {
            var card   = entry.Card;
            var target = entry.Target;

            OnCardExecutionStart?.Invoke(card);

            // Set context
            _ctx.PrimaryTarget = target ?? GetDefaultTarget();
            _ctx.ActiveCard    = card;

            // Spend energy
            _ctx.SpendEnergy(card.GetEffectiveCost());

            // Execute all OnPlay effects in order
            foreach (var effect in card.Data.GetOnPlayEffects())
            {
                if (_isCombatActive != null && !_isCombatActive()) yield break;
                effect.Execute(_ctx);

                // Small yield so effects that spawn VFX have a frame to breathe
                yield return null;
            }

            // Move card to discard / exhaust
            _deckManager.ResolveCardAfterPlay(card);

            // Fire global event
            CombatEvents.OnCardPlayedGlobal?.Invoke(card);
            _ctx.TrackCardPlayed();

            OnCardExecutionEnd?.Invoke(card);
        }

        // ----------------------------------------------------------
        // Control
        // ----------------------------------------------------------

        public void Pause()  => IsPaused = true;
        public void Resume() => IsPaused = false;

        /// <summary>
        /// Clears all pending cards without executing them.
        /// Call when combat ends mid-queue.
        /// </summary>
        public void Flush()
        {
            _queue.Clear();
            if (_processing != null)
            {
                StopCoroutine(_processing);
                _processing = null;
            }
            IsProcessing = false;
            IsPaused     = false;
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------

        /// <summary>
        /// Calculates how much energy is already committed to cards
        /// sitting in the queue but not yet executed.
        /// Used to validate new cards before adding them.
        /// </summary>
        private int CalculateReservedEnergy()
        {
            int reserved = 0;
            foreach (var entry in _queue)
                reserved += entry.Card.GetEffectiveCost();
            return reserved;
        }

        private CombatEntity GetDefaultTarget()
        {
            foreach (var e in _ctx.Enemies)
                if (!e.IsDead) return e;
            return null;
        }

        // ----------------------------------------------------------
        // Inner type
        // ----------------------------------------------------------

        private class QueuedCard
        {
            public CardInstance Card   { get; }
            public CombatEntity Target { get; }

            public QueuedCard(CardInstance card, CombatEntity target)
            {
                Card   = card;
                Target = target;
            }
        }
    }
}
