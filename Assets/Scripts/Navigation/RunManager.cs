// ============================================================
//  RunManager.cs  [UPDATED — Phase 3 Navigation]
//
//  CHANGES FROM PREVIOUS VERSION:
//    • Save/load hooks added (save on room clear only)
//    • MysteryRoomResolver integrated
//    • DeckReviewProvider exposed as public property
//    • NavigationSystem.InitializeFloor() called at floor start
//    • Card/relic lookup dictionaries built from Inspector arrays
//    • TotalRoomsThisFloor persisted through save/load
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class RunManager : MonoBehaviour
    {
        // ----------------------------------------------------------
        // Inspector
        // ----------------------------------------------------------

        [Header("Systems")]
        public CombatManager combatManager;
        public RelicManager  relicManager;

        [Header("Run Config")]
        [Tooltip("One FloorConfig per floor, in order.")]
        public FloorConfig[] floorConfigs;

        [Tooltip("All CardData assets in the game.")]
        public CardData[] allCards;

        [Tooltip("All RelicData assets in the game.")]
        public RelicData[] allRelics;

        [Header("Characters")]
        public CharacterData[] characters;

        // ----------------------------------------------------------
        // Public state
        // ----------------------------------------------------------

        public RunState            RunState       { get; private set; }
        public DeckReviewProvider  DeckReview     { get; private set; }
        public List<RoomChoice>    CurrentChoices { get; private set; } = new List<RoomChoice>();
        public RoomType            ActiveRoomType { get; private set; }

        // ----------------------------------------------------------
        // Private
        // ----------------------------------------------------------

        private NavigationSystem    _navigation;
        private MysteryRoomResolver _mysteryResolver;
        private CombatEntity        _playerEntity;
        private int                 _selectedCharacterIndex;
        private RoomChoice          _activeChoice;

        // Lookup tables (built once from Inspector arrays)
        private Dictionary<string, CardData>      _cardLookup;
        private Dictionary<string, RelicData>     _relicLookup;
        private Dictionary<string, CharacterData> _characterLookup;

        // ----------------------------------------------------------
        // Events (UI subscribes)
        // ----------------------------------------------------------

        public event System.Action<List<RoomChoice>>  OnChoicesGenerated;
        public event System.Action<RoomType>          OnRoomEntered;
        public event System.Action<CombatRewardSet>   OnRewardAvailable;
        public event System.Action<EventData>         OnEventStarted;
        public event System.Action                    OnShopOpened;
        public event System.Action                    OnRestSiteOpened;
        public event System.Action<bool>              OnRunEnded;
        public event System.Action<string>            OnProgressLabelUpdated; // "Room 3 / 12"

        // ----------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------

        void Awake()
        {
            BuildLookupTables();
        }

        // ----------------------------------------------------------
        // Entry points
        // ----------------------------------------------------------

        /// <summary>Starts a brand new run.</summary>
        public void StartRun(int characterIndex)
        {
            _selectedCharacterIndex = characterIndex;
            var charData = characters[characterIndex];

            var starterDeck = new List<CardInstance>();
            foreach (var card in charData.starterDeck)
                starterDeck.Add(new CardInstance(card));

            RunState  = new RunState(charData.characterClass, charData.baseHP, starterDeck);
            FinishRunSetup(charData);

            // New run — no save yet
            GameEvents.OnRunStarted?.Invoke();
            InitializeFloor();
        }

        /// <summary>
        /// Loads a saved run and lands on the navigation screen.
        /// </summary>
        public bool TryLoadRun()
        {
            if (!SaveSystem.HasSave()) return false;

            var save = SaveSystem.LoadRun();
            if (save == null) return false;

            RunState = SaveSystem.RestoreRunState(save, _cardLookup, _relicLookup, _characterLookup);
            _selectedCharacterIndex = save.characterIndex;

            var charData = characters[_selectedCharacterIndex];
            FinishRunSetup(charData);

            // Re-equip saved relics
            foreach (var relic in RunState.Relics)
                relicManager.EquipRelic(relic.Data);

            // Restore navigation to correct floor
            int floorIndex = Mathf.Clamp(RunState.CurrentFloor - 1, 0, floorConfigs.Length - 1);
            _navigation.InitializeFloor(floorConfigs[floorIndex]);

            // Restore the rolled room count from the save
            // NavigationSystem needs to know how many rooms this floor has
            // (so the boss still appears at the right index)
            // We set it directly since it was saved
            _navigation.TotalRoomsThisFloor = save.totalRoomsThisFloor;

            GameEvents.OnRunStarted?.Invoke();
            GenerateAndShowChoices();   // Land on navigation screen
            return true;
        }

        // ----------------------------------------------------------
        // Navigation
        // ----------------------------------------------------------

        /// <summary>Called by UI when player selects a room choice.</summary>
        public void SelectChoice(RoomChoice choice)
        {
            // Do NOT save here — only save after clearing
            _activeChoice = choice;
            CurrentChoices.Clear();
            EnterRoom(choice);
        }

        private void GenerateAndShowChoices()
        {
            CurrentChoices = _navigation.GenerateChoices(RunState);
            OnProgressLabelUpdated?.Invoke(_navigation.GetProgressLabel(RunState.CurrentRoomIndex));
            OnChoicesGenerated?.Invoke(CurrentChoices);
        }

        // ----------------------------------------------------------
        // Room entry
        // ----------------------------------------------------------

        private void EnterRoom(RoomChoice choice)
        {
            RoomType effective = choice.RoomType;

            // Mystery rooms resolve on entry
            if (choice.RoomType == RoomType.Mystery)
            {
                var outcome = _mysteryResolver.Resolve(choice);
                effective   = MysteryRoomResolver.ToEffectiveRoomType(outcome);
                Debug.Log($"[RunManager] Mystery resolved to: {outcome}");
            }

            ActiveRoomType = effective;
            OnRoomEntered?.Invoke(effective);

            switch (effective)
            {
                case RoomType.NormalCombat:
                case RoomType.EliteCombat:
                case RoomType.Boss:
                    EnterCombatRoom(choice);
                    break;

                case RoomType.Shop:
                    OnShopOpened?.Invoke();
                    break;

                // Mystery that resolved to Event
                case RoomType.Mystery:
                    OnEventStarted?.Invoke(choice.Event);
                    break;

                case RoomType.Rest:
                    OnRestSiteOpened?.Invoke();
                    break;

                case RoomType.Treasure:
                    EnterTreasureRoom();
                    break;
            }
        }

        // ----------------------------------------------------------
        // Combat
        // ----------------------------------------------------------

        private void EnterCombatRoom(RoomChoice choice)
        {
            SyncPlayerEntityFromRunState();

            CombatEvents.OnCombatComplete += HandleCombatComplete;
            combatManager.StartCombat(
                choice.Enemies,
                RunState.GetDeckCopy(),
                _playerEntity,
                GetAvailableCardPool(),
                RunState.PlayerClass
            );
        }

        private void HandleCombatComplete(CombatResult result)
        {
            CombatEvents.OnCombatComplete -= HandleCombatComplete;
            SyncRunStateFromPlayerEntity();

            if (!result.WasVictory)
            {
                EndRun(victory: false);
                return;
            }

            RunState.AddGold(result.GoldEarned);
            RunState.AdvanceRoom();

            // ── SAVE HERE — room has been cleared ─────────────────
            SaveAfterRoomClear();

            if (result.Reward != null)
                OnRewardAvailable?.Invoke(result.Reward);
            else
                ProceedToNextRoom();
        }

        // ----------------------------------------------------------
        // Reward
        // ----------------------------------------------------------

        public void ClaimReward(CardData chosenCard)
        {
            if (chosenCard != null)
                RunState.AddCard(new CardInstance(chosenCard));

            ProceedToNextRoom();
        }

        // ----------------------------------------------------------
        // Shop
        // ----------------------------------------------------------

        public void CloseShop()
        {
            RunState.AdvanceRoom();
            SaveAfterRoomClear();
            ProceedToNextRoom();
        }

        // ----------------------------------------------------------
        // Events
        // ----------------------------------------------------------

        public void ResolveEvent(EventChoice choice)
        {
            if (!choice.CanSelect(RunState))
            {
                Debug.LogWarning("[RunManager] Player tried to select an unaffordable event choice.");
                return;
            }

            // Apply costs
            if (choice.goldCost > 0)  RunState.SpendGold(choice.goldCost);
            if (choice.hpCost > 0)    RunState.SetCurrentHP(RunState.CurrentHP - choice.hpCost);

            // Apply rewards
            if (choice.goldReward > 0)    RunState.AddGold(choice.goldReward);
            if (choice.essenceReward > 0) GameEvents.OnEssenceChanged?.Invoke(choice.essenceReward);
            if (choice.soulsReward > 0)   GameEvents.OnSoulsChanged?.Invoke(choice.soulsReward);

            if (choice.hpChange != 0)
            {
                if (choice.hpChange > 0) RunState.HealHP(choice.hpChange);
                else
                {
                    RunState.SetCurrentHP(RunState.CurrentHP + choice.hpChange);
                    if (RunState.CurrentHP <= 0) { EndRun(false); return; }
                }
            }

            if (choice.gainRandomCard)
            {
                var pool = GetAvailableCardPool();
                if (pool.Count > 0)
                    RunState.AddCard(new CardInstance(pool[Random.Range(0, pool.Count)]));
            }

            RunState.AdvanceRoom();
            SaveAfterRoomClear();
            ProceedToNextRoom();
        }

        // ----------------------------------------------------------
        // Rest
        // ----------------------------------------------------------

        public enum RestChoice { Heal, Upgrade }

        public void ResolveRest(RestChoice choice, CardInstance cardToUpgrade = null)
        {
            switch (choice)
            {
                case RestChoice.Heal:
                    RunState.HealHP(Mathf.RoundToInt(RunState.MaxHP * 0.30f));
                    break;

                case RestChoice.Upgrade:
                    if (cardToUpgrade != null && cardToUpgrade.CanUpgrade())
                    {
                        cardToUpgrade.Upgrade();
                        GameEvents.OnCardUpgraded?.Invoke(cardToUpgrade);
                    }
                    break;
            }

            RunState.AdvanceRoom();
            SaveAfterRoomClear();
            ProceedToNextRoom();
        }

        // ----------------------------------------------------------
        // Treasure
        // ----------------------------------------------------------

        private void EnterTreasureRoom()
        {
            var pool  = GetAvailableCardPool();
            var cards = new List<CardData>();
            var used  = new HashSet<string>();

            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                var pick = pool[Random.Range(0, pool.Count)];
                if (used.Contains(pick.cardID)) continue;
                cards.Add(pick);
                used.Add(pick.cardID);
            }

            RunState.AdvanceRoom();
            SaveAfterRoomClear();
            OnRewardAvailable?.Invoke(new CombatRewardSet(0, cards));
        }

        // ----------------------------------------------------------
        // Floor transition
        // ----------------------------------------------------------

        private void TryAdvanceFloor()
        {
            RunState.AdvanceFloor();

            int nextIndex = RunState.CurrentFloor - 1;
            if (nextIndex >= floorConfigs.Length)
            {
                EndRun(victory: true);
                return;
            }

            InitializeFloor();
            GenerateAndShowChoices();
        }

        private void InitializeFloor()
        {
            int index    = Mathf.Clamp(RunState.CurrentFloor - 1, 0, floorConfigs.Length - 1);
            var config   = floorConfigs[index];

            _navigation.InitializeFloor(config);
            _mysteryResolver.SetConfig(config);
        }

        // ----------------------------------------------------------
        // Proceed after any room completion
        // ----------------------------------------------------------

        private void ProceedToNextRoom()
        {
            if (ActiveRoomType == RoomType.Boss)
            {
                TryAdvanceFloor();
                return;
            }

            GenerateAndShowChoices();
        }

        // ----------------------------------------------------------
        // Save
        // ----------------------------------------------------------

        /// <summary>
        /// Writes the current run state to disk.
        /// Called only after a room is fully cleared.
        /// </summary>
        private void SaveAfterRoomClear()
        {
            var saveData = SaveSystem.BuildSaveData(
                RunState,
                _selectedCharacterIndex,
                _navigation.TotalRoomsThisFloor);

            SaveSystem.SaveRun(saveData);
        }

        // ----------------------------------------------------------
        // Run end
        // ----------------------------------------------------------

        private void EndRun(bool victory)
        {
            relicManager.OnRunEnd();
            SaveSystem.DeleteSave();   // Clean up save on run end (win or lose)
            GameEvents.OnRunEnded?.Invoke(victory);
            OnRunEnded?.Invoke(victory);
        }

        // ----------------------------------------------------------
        // Deck review (available any time during navigation)
        // ----------------------------------------------------------

        /// <summary>UI calls this to open the deck review panel.</summary>
        public DeckSummary GetDeckSummary() => DeckReview.GetSummary();

        /// <summary>Returns cards sorted/filtered for the review UI.</summary>
        public List<CardInstance> GetDeckForReview(DeckReviewSort sort = DeckReviewSort.ByCost)
        {
            return sort switch
            {
                DeckReviewSort.ByCost => DeckReview.GetSortedByCost(),
                DeckReviewSort.ByName => DeckReview.GetSortedByName(),
                _                    => DeckReview.GetSortedByCost()
            };
        }

        // ----------------------------------------------------------
        // Setup helpers
        // ----------------------------------------------------------

        private void FinishRunSetup(CharacterData charData)
        {
            _playerEntity = new CombatEntity("Player", RunState.MaxHP, isPlayer: true);

            _navigation      = new NavigationSystem(floorConfigs[0]);
            _mysteryResolver = new MysteryRoomResolver(floorConfigs[0]);
            DeckReview       = new DeckReviewProvider(RunState);

            relicManager.InitializeRun(
                addGold:     n => RunState.AddGold(n),
                addEssence:  n => GameEvents.OnEssenceChanged?.Invoke(n),
                addSouls:    n => GameEvents.OnSoulsChanged?.Invoke(n),
                modifyMaxHP: n => RunState.SetMaxHP(RunState.MaxHP + n),
                drawCards:   n => combatManager.deckManager.DrawCards(n),
                gainEnergy:  n => { }
            );

            if (charData.starterRelic != null)
                relicManager.EquipRelic(charData.starterRelic);

            CombatEvents.OnEnemyKilled += _ => RunState.AddEnemiesKilled(1);
        }

        private void SyncPlayerEntityFromRunState()
        {
            _playerEntity.SetMaxHealth(RunState.MaxHP);
            int diff = _playerEntity.CurrentHealth - RunState.CurrentHP;
            if (diff > 0) _playerEntity.TakeDamage(diff, DamageType.True);
            else if (diff < 0) _playerEntity.Heal(-diff);
        }

        private void SyncRunStateFromPlayerEntity()
        {
            RunState.SetCurrentHP(_playerEntity.CurrentHealth);
        }

        private void BuildLookupTables()
        {
            _cardLookup      = new Dictionary<string, CardData>();
            _relicLookup     = new Dictionary<string, RelicData>();
            _characterLookup = new Dictionary<string, CharacterData>();

            foreach (var card  in allCards)    if (card  != null) _cardLookup[card.cardID]               = card;
            foreach (var relic in allRelics)   if (relic != null) _relicLookup[relic.RelicID]            = relic;
            foreach (var ch   in characters)   if (ch    != null) _characterLookup[ch.characterClass.ToString()] = ch;
        }

        private List<CardData> GetAvailableCardPool()
        {
            var result = new List<CardData>();
            foreach (var card in allCards)
            {
                if (card == null) continue;
                if (card.owner != RunState.PlayerClass && card.owner != CharacterClass.Neutral) continue;
                if (card.tier != UpgradeTier.Base) continue;
                result.Add(card);
            }
            return result;
        }
    }

    public enum DeckReviewSort { ByCost, ByName }
}
