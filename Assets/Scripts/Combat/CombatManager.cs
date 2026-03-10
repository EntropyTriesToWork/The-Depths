using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    [RequireComponent(typeof(CardQueue))]
    public class CombatManager : MonoBehaviour
    {
        [Header("References")]
        public DeckManager deckManager;
        public RewardConfig rewardConfig;
        public DefenseConfig defenseConfig;

        [Header("Settings")]
        [Tooltip("Cards drawn at the start of each player turn.")]
        public int cardsDrawnPerTurn = 5;

        [Tooltip("Energy restored at the start of each player turn.")]
        public int energyPerTurn = 6;

        [Tooltip("Seconds to wait between enemy actions (for visual readability).")]
        public float enemyActionDelay = 0.6f;

        public CombatState CurrentState { get; private set; } = CombatState.Idle;
        public bool IsQueueProcessing => _cardQueue != null && _cardQueue.IsProcessing;

        private CombatContext _ctx;
        private CombatEntity _player;
        private List<EnemyInstance> _enemies = new List<EnemyInstance>();
        private int _turnNumber = 0;
        private CardQueue _cardQueue;
        private bool _combatActive;    // Lets CardQueue know if combat is live

        private bool _playerEndedTurn = false;

        private Action<List<CardInstance>> _pendingSelectionCallback;

        private List<CardData> _availableCardPool = new List<CardData>();
        private CharacterClass _playerClass;
        public event Action<CombatState> OnStateChanged;

        #region States
        void Awake()
        {
            CombatEntity.SetDefenseConfig(defenseConfig);
            Debug.Log($"[Harness] DefenseConfig set: {defenseConfig != null}");

            _cardQueue = GetComponent<CardQueue>();
            if (_cardQueue == null)
                _cardQueue = gameObject.AddComponent<CardQueue>();

            _cardQueue.OnQueueEmpty += OnCardQueueEmpty;
            _cardQueue.OnCardRejected += (card, reason) =>
                Debug.Log($"[CombatManager] Card rejected: {card.Data.cardName} — {reason}");
        }

        public void StartCombat(
            List<EnemyData> enemyDataList,
            List<CardInstance> playerDeck,
            CombatEntity playerEntity,
            List<CardData> cardPool,
            CharacterClass playerClass)
        {
            _player = playerEntity;
            _playerClass = playerClass;
            _availableCardPool = cardPool;
            _enemies.Clear();
            _turnNumber = 0;

            foreach (var data in enemyDataList)
                _enemies.Add(new EnemyInstance(data));

            _ctx = new CombatContext(
                player: _player,
                enemies: BuildEntityList(),
                deckManager: deckManager,
                currentEnergy: energyPerTurn,
                maxEnergy: energyPerTurn,
                currentGold: 0           // RunManager owns gold
            );
            _combatActive = true;
            _cardQueue.Initialize(_ctx, deckManager, () => _combatActive);

            deckManager.InitializeCombat(playerDeck);

            CombatEvents.RequestDiscardSelection += HandleDiscardRequest;
            CombatEvents.RequestExhaustSelection += HandleExhaustRequest;
            CombatEvents.RequestAddRandomCardToHand += HandleAddRandomCard;

            _player.OnDeath += OnPlayerDied;
            foreach (var e in _enemies)
                e.Entity.OnDeath += () => OnEnemyDied(e);

            StartCoroutine(CombatLoop());
        }

        private IEnumerator CombatLoop()
        {
            TransitionTo(CombatState.Initializing);
            yield return null;

            while (true)
            {
                // ── PLAYER TURN ────────────────────────────────────
                TransitionTo(CombatState.PlayerTurn);
                yield return StartCoroutine(DoPlayerTurn());

                // ── CHECK STATE ────────────────────────────────────
                if (CheckPlayerDead()) yield break;

                // ── ENEMY TURN ─────────────────────────────────────
                TransitionTo(CombatState.EnemyTurn);
                yield return StartCoroutine(DoEnemyTurn());

                // ── CHECK STATE ────────────────────────────────────
                if (CheckAllEnemiesDead()) yield break;
                if (CheckPlayerDead()) yield break;
            }
        }
        #endregion

        #region Player's Turn
        private IEnumerator DoPlayerTurn()
        {
            _turnNumber++;
            _playerEndedTurn = false;

            RefillEnergy(); // Restore energy

            _player.ProcessStartOfTurnStatuses();
            foreach (var e in _enemies)
                if (!e.IsDead) e.Entity.ProcessStartOfTurnStatuses();

            foreach (var e in _enemies)
                if (!e.IsDead) e.SelectNextIntent();

            deckManager.DrawCards(cardsDrawnPerTurn);

            FireTriggerEffects(EffectTrigger.StartOfTurn);

            CombatEvents.OnPlayerTurnStart?.Invoke();
            _ctx.ResetTurnTracking();

            // Wait for player to end their turn
            while (!_playerEndedTurn && !_player.IsDead)
                yield return null;

            _cardQueue.Flush();

            FireTriggerEffects(EffectTrigger.EndOfTurn);

            deckManager.DiscardHand();

            _player.ProcessEndOfTurnStatuses();

            CombatEvents.OnPlayerTurnEnd?.Invoke();
        }

        private IEnumerator DoEnemyTurn()
        {
            CombatEvents.OnEnemyTurnStart?.Invoke();

            foreach (var enemy in _enemies)
            {
                // Skip dead enemies — they were alive when the turn started
                // but may have died from status ticks or other effects
                if (enemy.IsDead) continue;

                // Stop if player died mid-turn
                if (_player.IsDead) yield break;

                // Stop if combat ended mid-turn (e.g. DoVictory was triggered
                // by OnCardQueueEmpty during a status tick that killed the last enemy)
                if (!_combatActive) yield break;

                yield return new WaitForSeconds(enemyActionDelay);

                // Re-check after the delay — status ticks can kill between frames
                if (enemy.IsDead) continue;
                if (_player.IsDead) yield break;
                if (!_combatActive) yield break;

                enemy.Entity.ProcessStartOfTurnStatuses();
                enemy.ExecuteIntent(_ctx);
                enemy.Entity.ProcessEndOfTurnStatuses();

                // Check if this enemy's action killed all remaining enemies
                // (e.g. a self-destruct or reflect damage scenario)
                bool anyAlive = false;
                foreach (var e in _enemies)
                    if (!e.IsDead) { anyAlive = true; break; }

                if (!anyAlive) yield break;

                CombatEvents.OnEnemyTurnEnd?.Invoke();
            }
        }
        /// <summary>
        /// Called by the UI when the player taps/clicks a card.
        /// Adds the card to the execution queue rather than playing it instantly.
        /// Returns false if the card is immediately invalid (wrong turn state).
        /// </summary>
        public bool TryPlayCard(CardInstance card, CombatEntity target = null)
        {
            if (CurrentState != CombatState.PlayerTurn) return false;
            return _cardQueue.TryEnqueue(card, target);
        }
        /// <summary>
        /// Called by CardQueue when all queued cards have been executed.
        /// This is where we check for dead enemies after a burst of card plays.
        /// </summary>
        private void OnCardQueueEmpty()
        {
            if (CurrentState != CombatState.PlayerTurn) return;
            CheckForDeadEnemies();

            bool allDead = true;
            foreach (var e in _enemies)
                if (!e.IsDead) { allDead = false; break; }

            if (allDead)
                StartCoroutine(DoVictory());
        }

        public void EndPlayerTurn()
        {
            if (CurrentState != CombatState.PlayerTurn) return;

            if (_cardQueue.IsProcessing)
            {
                void OnEmpty()
                {
                    _cardQueue.OnQueueEmpty -= OnEmpty;
                    _playerEndedTurn = true;
                }
                _cardQueue.OnQueueEmpty += OnEmpty;
                return;
            }

            _playerEndedTurn = true;
        }
        #endregion

        #region State Checks
        private bool CheckAllEnemiesDead()
        {
            foreach (var e in _enemies)
                if (!e.IsDead) return false;

            StartCoroutine(DoVictory());
            return true;
        }

        private bool CheckPlayerDead()
        {
            if (!_player.IsDead) return false;

            TransitionTo(CombatState.Defeat);
            CombatEvents.OnPlayerDeath?.Invoke();
            CombatEvents.OnCombatComplete?.Invoke(new CombatResult(false, 0, null));
            Cleanup();
            return true;
        }

        private void CheckForDeadEnemies()
        {
            foreach (var e in _enemies)
            {
                if (e.IsDead)
                {
                    CombatEvents.OnEnemyKilled?.Invoke(e.Entity); // Note: enemy stays in list so its index is stable.
                    // Filter IsDead checks when iterating for actions.
                }
            }
        }

        private void OnEnemyDied(EnemyInstance enemy)
        {
            // Additional death callbacks can go here
            // (animations, particle effects via CombatEvents)
        }

        private void OnPlayerDied()
        {
            _playerEndedTurn = true;   // Break the player turn wait loop
        }
        private IEnumerator DoVictory()
        {
            _combatActive = false;   // Signals DoEnemyTurn to stop if still running
            TransitionTo(CombatState.Victory);
            CombatEvents.OnCombatVictory?.Invoke();

            yield return new WaitForSeconds(0.5f);  // Brief pause before reward

            bool isElite = false;
            bool isBoss = false;
            foreach (var e in _enemies)
            {
                if (e.Data.isBoss) isBoss = true;
                if (e.Data.isElite) isElite = true;
            }

            CombatRewardSet reward = GenerateReward(isElite, isBoss);

            TransitionTo(CombatState.Reward);

            // Combat is functionally complete — hand reward to RunManager
            CombatEvents.OnCombatComplete?.Invoke(new CombatResult(true, reward.GoldReward, reward));
            Cleanup();
        }
        #endregion

        #region Rewards
        private CombatRewardSet GenerateReward(bool isElite, bool isBoss)
        {
            // Gold
            Vector2Int goldRange = isElite ? rewardConfig.eliteCombatGold
                                 : isBoss ? rewardConfig.bossCombatGold
                                           : rewardConfig.normalCombatGold;
            int gold = RewardHooks.ApplyGoldModifiers(UnityEngine.Random.Range(goldRange.x, goldRange.y + 1));

            // Card choices
            int count = isElite ? rewardConfig.eliteCardChoices
                      : isBoss ? rewardConfig.bossCardChoices
                                : rewardConfig.normalCardChoices;

            Vector3 weights = isElite ? rewardConfig.eliteRarityWeights
                            : isBoss ? rewardConfig.bossRarityWeights
                                      : rewardConfig.normalRarityWeights;

            var cards = PickCardRewards(count, weights);
            var reward = new CombatRewardSet(gold, cards, isElite, isBoss);

            // Essence chance on elite
            if (isElite && UnityEngine.Random.Range(0, 100) < rewardConfig.eliteEssenceChance)
            {
                int essence = UnityEngine.Random.Range(
                    rewardConfig.eliteEssenceAmount.x,
                    rewardConfig.eliteEssenceAmount.y + 1);
                reward.AddEssence(essence);
            }

            // Boss always drops a soul shard
            if (isBoss) reward.AddSoulShard();

            return reward;
        }

        private List<CardData> PickCardRewards(int count, Vector3 rarityWeights)
        {
            var result = new List<CardData>();
            var used = new HashSet<string>();

            // Filter pool to player's class + neutral
            var pool = new List<CardData>();
            foreach (var card in _availableCardPool)
            {
                if (card == null) continue;
                if (card.owner != _playerClass && card.owner != CharacterClass.Neutral) continue;
                if (!string.IsNullOrEmpty(card.unlockID)) continue;   // Skip locked cards
                if (card.tier != UpgradeTier.Base) continue;           // Only offer base tier
                pool.Add(card);
            }

            float totalWeight = rarityWeights.x + rarityWeights.y + rarityWeights.z;

            for (int i = 0; i < count; i++)
            {
                // Pick rarity
                CardRarity targetRarity = RollRarity(rarityWeights, totalWeight);

                // Find a card of that rarity not already offered
                var candidates = new List<CardData>();
                foreach (var card in pool)
                    if (card.rarity == targetRarity && !used.Contains(card.cardID))
                        candidates.Add(card);

                // Fallback: relax rarity if no candidates
                if (candidates.Count == 0)
                    foreach (var card in pool)
                        if (!used.Contains(card.cardID))
                            candidates.Add(card);

                if (candidates.Count == 0) break;

                var picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                result.Add(picked);
                used.Add(picked.cardID);
            }

            return result;
        }

        private CardRarity RollRarity(Vector3 weights, float total)
        {
            float roll = UnityEngine.Random.Range(0f, total);
            if (roll < weights.x) return CardRarity.Common;
            if (roll < weights.x + weights.y) return CardRarity.Uncommon;
            return CardRarity.Rare;
        }
        #endregion

        #region Triggers
        private void FireTriggerEffects(EffectTrigger trigger)
        {
            // Collect cards in all zones that have this trigger
            // (Powers in hand, exhaust triggers, etc.)
            foreach (var card in deckManager.Hand)
                foreach (var effect in card.Data.GetEffectsForTrigger(trigger))
                    effect.Execute(_ctx);
        }
        #endregion

        #region Effect Request Handlers
        private void HandleDiscardRequest(int count)
        {
            // Signal to UI — UI will call back with chosen cards
            // For now, auto-discard random (replace with UI callback later)
            var hand = new List<CardInstance>(deckManager.Hand);
            for (int i = 0; i < count && hand.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, hand.Count);
                deckManager.DiscardCard(hand[idx]);
                hand.RemoveAt(idx);
            }
        }

        private void HandleExhaustRequest(int count)
        {
            var hand = new List<CardInstance>(deckManager.Hand);
            for (int i = 0; i < count && hand.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, hand.Count);
                deckManager.ExhaustCard(hand[idx]);
                hand.RemoveAt(idx);
            }
        }

        private void HandleAddRandomCard()
        {
            if (_availableCardPool.Count == 0) return;
            var card = _availableCardPool[UnityEngine.Random.Range(0, _availableCardPool.Count)];
            deckManager.AddToHand(new CardInstance(card));
        }
        #endregion

        #region Helpers
        private void RefillEnergy()
        {
            while (_ctx.CurrentEnergy < _ctx.MaxEnergy)
                _ctx.GainEnergy(1);
        }
        private CombatEntity GetDefaultTarget()
        {
            foreach (var e in _enemies)
                if (!e.IsDead) return e.Entity;
            return null;
        }
        private List<CombatEntity> BuildEntityList()
        {
            var list = new List<CombatEntity>();
            foreach (var e in _enemies) list.Add(e.Entity);
            return list;
        }

        private void TransitionTo(CombatState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
        private void Cleanup()
        {
            _combatActive = false;
            CombatEvents.RequestDiscardSelection -= HandleDiscardRequest;
            CombatEvents.RequestExhaustSelection -= HandleExhaustRequest;
            CombatEvents.RequestAddRandomCardToHand -= HandleAddRandomCard;

            _player.OnDeath -= OnPlayerDied;
        }
        public int GetCurrentEnergy() => _ctx?.CurrentEnergy ?? 0;
        public int GetMaxEnergy() => _ctx?.MaxEnergy ?? 0;
    }
    #endregion

    public enum CombatState
    {
        Idle,
        Initializing,
        PlayerTurn,
        EnemyTurn,
        CheckState,
        Victory,
        Defeat,
        Reward
    }

    public class CombatResult
    {
        public bool WasVictory { get; }
        public int GoldEarned { get; }
        public CombatRewardSet Reward { get; }

        public CombatResult(bool victory, int gold, CombatRewardSet reward)
        {
            WasVictory = victory;
            GoldEarned = gold;
            Reward = reward;
        }
    }
}