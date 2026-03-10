// ============================================================
//  TestHarness.cs
//  Full run-loop test harness with ScriptableObject support.
//
//  SETUP:
//    1. New empty scene
//    2. Empty GameObject → Add TestHarness
//    3. Assign DefenseConfig (required)
//    4. Optionally assign CardPool, NormalEnemyPool, EliteEnemyPool,
//       BossEnemyPool, EventPool, CharacterData in the Inspector.
//       If left empty, hardcoded fallbacks are used automatically.
//    5. Press Play
//
//  FIXES INCLUDED:
//    • Dead enemies are skipped on their turn (guard in DrawCombat
//      and a note for CombatManager — see DEAD ENEMY FIX below)
//    • Rewards always appear after combat
//    • Intent display infers from EnemyAction fields or uses
//      EnemyAction.customIntentDescription if set
//
//  NOTE — add this field to EnemyAction.cs for custom intents:
//    [Tooltip("If set, overrides the auto-generated intent description.")]
//    public string customIntentDescription = "";
//
//  NOTE — add this guard to CombatManager.DoEnemyTurn() to prevent
//  dead enemies from attacking:
//    foreach (var enemy in _enemies)
//    {
//        if (enemy.IsDead) continue;   // ← ADD THIS LINE
//        yield return StartCoroutine(enemy.ExecuteIntent(...));
//    }
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public enum HarnessState
    {
        Navigation,
        Combat,
        Reward,
        Shop,
        Rest,
        Event,
        DeckReview,
        Victory,
        Defeat
    }

    public class TestHarness : MonoBehaviour
    {
        // ----------------------------------------------------------
        // Inspector — ScriptableObject data sources
        // ----------------------------------------------------------

        [Header("Required")]
        public DefenseConfig defenseConfig;

        [Header("Character (optional — uses fallback if empty)")]
        [Tooltip("Assigns character class, starting HP, and starter deck.")]
        public CharacterData characterData;

        [Header("Card Pool (optional — uses fallback if empty)")]
        [Tooltip("Cards that can appear as rewards and in the shop.")]
        public CardData[] cardPool;

        [Header("Enemy Pools (optional — uses fallback if empty)")]
        public EnemyData[] normalEnemyPool;
        public EnemyData[] eliteEnemyPool;
        public EnemyData[] bossEnemyPool;

        [Header("Events (optional — uses fallback if empty)")]
        public EventData[] eventPool;

        [Header("Floor Config (optional — uses fallback if empty)")]
        public FloorConfig floorConfig;

        [Header("Player Stats (used when no CharacterData assigned)")]
        public int startingHP = 80;
        public int startingEnergy = 3;
        public int drawPerTurn = 5;

        // ----------------------------------------------------------
        // Systems
        // ----------------------------------------------------------

        private CombatManager _combatManager;
        private DeckManager _deckManager;
        private NavigationSystem _navigation;
        private MysteryRoomResolver _mysteryResolver;

        // ----------------------------------------------------------
        // Run state
        // ----------------------------------------------------------

        private HarnessState _state = HarnessState.Navigation;
        private HarnessState _returnState = HarnessState.Navigation;

        private int _currentHP;
        private int _maxHP;
        private int _gold;
        private int _floor = 1;
        private int _roomsCleared = 0;

        private CharacterClass _playerClass = CharacterClass.Orin;
        private List<CardInstance> _masterDeck = new List<CardInstance>();
        private CombatEntity _playerEntity;

        // ----------------------------------------------------------
        // Per-room state
        // ----------------------------------------------------------

        private List<RoomChoice> _currentChoices = new List<RoomChoice>();
        private RoomChoice _activeChoice;
        private List<EnemyInstance> _activeEnemies = new List<EnemyInstance>();

        // Reward
        private List<CardData> _rewardChoices = new List<CardData>();

        // Shop
        private List<CardData> _shopCards = new List<CardData>();
        private List<(string label, int cost, System.Action action)> _shopActions = new();

        // Event
        private EventData _activeEvent;

        // ----------------------------------------------------------
        // Runtime pools (merged from SO + fallback)
        // ----------------------------------------------------------

        private List<CardData> _runtimeCardPool = new List<CardData>();
        private List<EnemyData> _runtimeNormalPool = new List<EnemyData>();
        private List<EnemyData> _runtimeElitePool = new List<EnemyData>();
        private List<EnemyData> _runtimeBossPool = new List<EnemyData>();
        private List<EventData> _runtimeEventPool = new List<EventData>();
        private FloorConfig _runtimeFloorConfig;

        // ----------------------------------------------------------
        // IMGUI scroll state
        // ----------------------------------------------------------

        private Vector2 _logScroll;
        private Vector2 _handScroll;
        private Vector2 _deckScroll;
        private string _log = "";

        // ============================================================
        // UNITY LIFECYCLE
        // ============================================================

        void Awake()
        {
            if (defenseConfig == null)
            {
                defenseConfig = ScriptableObject.CreateInstance<DefenseConfig>();
                Log("⚠ No DefenseConfig assigned — using defaults.");
            }

            // Build runtime pools (SO data takes priority, fallbacks fill gaps)
            BuildRuntimePools();
            BuildRuntimeFloorConfig();

            // Unity components
            var deckGO = new GameObject("DeckManager");
            _deckManager = deckGO.AddComponent<DeckManager>();
            _deckManager.maxHandSize = 10;

            var cmGO = new GameObject("CombatManager");
            _combatManager = cmGO.AddComponent<CombatManager>();
            _combatManager.deckManager = _deckManager;
            _combatManager.cardsDrawnPerTurn = drawPerTurn;
            _combatManager.energyPerTurn = startingEnergy;
            _combatManager.enemyActionDelay = 0.2f;
            _combatManager.defenseConfig = defenseConfig;

            _combatManager.OnStateChanged += s => Log($"  → Combat state: {s}");
            _combatManager.OnCombatComplete += HandleCombatComplete;

            _deckManager.OnCardDrawn += c => Log($"  Drew: {c.Data.cardName}");
            _deckManager.OnCardDiscarded += c => Log($"  Discarded: {c.Data.cardName}");
            _deckManager.OnDeckReshuffled += () => Log("  🔀 Reshuffled");

            // Navigation
            _navigation = new NavigationSystem(_runtimeFloorConfig);
            _navigation.InitializeFloor(_runtimeFloorConfig);
            _mysteryResolver = new MysteryRoomResolver(_runtimeFloorConfig);

            // Player setup
            if (characterData != null)
            {
                _maxHP = characterData.baseHP;
                _playerClass = characterData.characterClass;
                foreach (var card in characterData.starterDeck)
                    _masterDeck.Add(new CardInstance(card));

                Log($"✓ Character: {characterData.characterName} ({_playerClass}), {_maxHP} HP, " +
                    $"{_masterDeck.Count} starter cards");
            }
            else
            {
                _maxHP = startingHP;
                _playerClass = CharacterClass.Orin;
                BuildFallbackStarterDeck();
                Log($"✓ No CharacterData assigned — using fallback stats ({_maxHP} HP)");
            }

            _currentHP = _maxHP;
            _gold = 99;

            Log("✓ Harness ready.");
            GoToNavigation();
        }

        // ============================================================
        // IMGUI — MAIN DISPATCHER
        // ============================================================

        void OnGUI()
        {
            DrawTopBar();
            GUILayout.BeginArea(new Rect(10, 66, Screen.width - 20, Screen.height - 76));

            switch (_state)
            {
                case HarnessState.Navigation: DrawNavigation(); break;
                case HarnessState.Combat: DrawCombat(); break;
                case HarnessState.Reward: DrawReward(); break;
                case HarnessState.Shop: DrawShop(); break;
                case HarnessState.Rest: DrawRest(); break;
                case HarnessState.Event: DrawEvent(); break;
                case HarnessState.DeckReview: DrawDeckReview(); break;
                case HarnessState.Victory: DrawVictory(); break;
                case HarnessState.Defeat: DrawDefeat(); break;
            }

            GUILayout.EndArea();
        }

        // ============================================================
        // TOP BAR
        // ============================================================

        void DrawTopBar()
        {
            GUI.Box(new Rect(0, 0, Screen.width, 60), "");
            GUILayout.BeginArea(new Rect(8, 6, Screen.width - 16, 52));
            GUILayout.BeginHorizontal();

            GUILayout.Label($"❤ {_currentHP}/{_maxHP}", Bold(17), GUILayout.Width(132));
            GUILayout.Label($"💰 {_gold}g", Bold(17), GUILayout.Width(96));
            GUILayout.Label(
                $"Floor {_floor}  |  Room {_roomsCleared + 1}/{_navigation.TotalRoomsThisFloor}",
                Bold(17), GUILayout.Width(240));
            GUILayout.Label($"Deck: {_masterDeck.Count}", Style(14), GUILayout.Width(96));

            GUILayout.FlexibleSpace();

            bool canReview = _state != HarnessState.DeckReview
                          && _state != HarnessState.Combat
                          && _state != HarnessState.Victory
                          && _state != HarnessState.Defeat;

            if (canReview && GUILayout.Button("📖 Deck", GUILayout.Height(43), GUILayout.Width(96)))
            {
                _returnState = _state;
                _state = HarnessState.DeckReview;
            }

            if (_state == HarnessState.DeckReview
             && GUILayout.Button("✕ Close", GUILayout.Height(43), GUILayout.Width(96)))
                _state = _returnState;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // ============================================================
        // NAVIGATION
        // ============================================================

        void DrawNavigation()
        {
            GUILayout.Label("── Choose Your Next Room ──", Bold(19));
            GUILayout.Label(
                $"Floor {_floor}  ·  {_navigation.GetProgressLabel(_roomsCleared)}",
                Style(16));
            GUILayout.Space(12);

            if (_currentChoices.Count == 0)
            {
                GUILayout.Label("No choices available.", Style(16));
                return;
            }

            GUILayout.BeginHorizontal();

            foreach (var choice in _currentChoices)
            {
                GUILayout.BeginVertical("box", GUILayout.Width(264), GUILayout.Height(180));

                GUILayout.Label(RoomIcon(choice.RoomType) + " " + choice.Label, Bold(18));
                GUILayout.Label(choice.Hint, Style(14));
                GUILayout.Space(6);

                if (choice.RoomType == RoomType.NormalCombat
                 || choice.RoomType == RoomType.EliteCombat
                 || choice.RoomType == RoomType.Boss)
                {
                    foreach (var e in choice.Enemies)
                        GUILayout.Label($"  • {e.enemyName}  (HP: {e.baseHP})", Style(13));
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Enter", GUILayout.Height(36)))
                    SelectRoom(choice);

                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(12);
            DrawLog(168);
        }

        void SelectRoom(RoomChoice choice)
        {
            _activeChoice = choice;

            if (choice.RoomType == RoomType.Mystery)
            {
                var outcome = _mysteryResolver.Resolve(choice);
                Log($"🔮 Mystery resolved: {outcome}");
            }

            RoomType effective = choice.RoomType == RoomType.Mystery
                ? (choice.MysteryOutcome == MysteryOutcome.NormalCombat ? RoomType.NormalCombat
                : choice.MysteryOutcome == MysteryOutcome.Shop ? RoomType.Shop
                : RoomType.Mystery)
                : choice.RoomType;

            Log($"▶ Entering: {choice.Label}");

            switch (effective)
            {
                case RoomType.NormalCombat:
                case RoomType.EliteCombat:
                case RoomType.Boss:
                    StartCombat(choice);
                    break;

                case RoomType.Shop:
                    OpenShop();
                    break;

                case RoomType.Rest:
                    _state = HarnessState.Rest;
                    break;

                case RoomType.Treasure:
                    OpenTreasure();
                    break;

                default:
                    if (choice.Event != null)
                    {
                        _activeEvent = choice.Event;
                        _state = HarnessState.Event;
                    }
                    else
                    {
                        _state = HarnessState.Rest;
                        Log("  (No event data — defaulting to Rest)");
                    }
                    break;
            }
        }

        // ============================================================
        // COMBAT
        // ============================================================

        void StartCombat(RoomChoice choice)
        {
            _state = HarnessState.Combat;
            _activeEnemies.Clear();

            _playerEntity = new CombatEntity("Player", _maxHP, isPlayer: true, defenseConfig);
            if (_currentHP < _maxHP)
                _playerEntity.TakeDamage(_maxHP - _currentHP, DamageType.True);

            _playerEntity.OnDamageTaken += bd =>
                Log($"  Player hit — Blk:{bd.BlockDamage} HP:{bd.HealthDamage}");
            _playerEntity.OnDeath += () => Log("💀 Player died!");

            var enemyDataList = choice.Enemies.Count > 0
                ? choice.Enemies
                : new List<EnemyData> { _runtimeNormalPool[0] };

            foreach (var eData in enemyDataList)
            {
                var inst = new EnemyInstance(eData);
                inst.Entity.OnDamageTaken += bd => Log($"  {eData.enemyName} hit — total:{bd.TotalDamage}");
                inst.Entity.OnDeath += () => Log($"  ☠ {eData.enemyName} defeated!");
                _activeEnemies.Add(inst);
            }

            Log($"\n⚔ Combat: {string.Join(", ", enemyDataList.ConvertAll(e => e.enemyName))}");

            _combatManager.StartCombat(
                enemyDataList,
                new List<CardInstance>(_masterDeck),
                _playerEntity,
                _runtimeCardPool,
                _playerClass
            );
        }

        void DrawCombat()
        {
            bool isPlayerTurn = _combatManager.CurrentState == CombatState.PlayerTurn;

            GUILayout.BeginHorizontal();

            // ── Player ───────────────────────────────────────────
            GUILayout.BeginVertical("box", GUILayout.Width(240));
            GUILayout.Label("── PLAYER ──", Bold(14));
            if (_playerEntity != null)
            {
                GUILayout.Label($"HP:      {_playerEntity.CurrentHealth} / {_playerEntity.MaxHealth}", Style(14));
                GUILayout.Label($"Block:   {_playerEntity.CurrentBlock}", Style(14));
                GUILayout.Label($"Armor:   {_playerEntity.CurrentArmor}", Style(14));
                GUILayout.Label($"Barrier: {_playerEntity.CurrentBarrier}", Style(14));
                foreach (var kv in _playerEntity.GetAllStatuses())
                    GUILayout.Label($"  {kv.Key}: {kv.Value}", Style(13));
            }
            GUILayout.EndVertical();

            // ── Enemies ───────────────────────────────────────────
            GUILayout.BeginVertical("box", GUILayout.Width(276));
            GUILayout.Label("── ENEMIES ──", Bold(14));
            foreach (var enemy in _activeEnemies)
            {
                // DEAD ENEMY FIX: skip display of dead enemies cleanly
                if (enemy.IsDead)
                {
                    GUILayout.Label($"[{enemy.Data.enemyName}: DEAD]", Style(13));
                    continue;
                }

                GUILayout.Label(enemy.Data.enemyName, Bold(14));
                GUILayout.Label($"  HP:    {enemy.Entity.CurrentHealth}/{enemy.Entity.MaxHealth}", Style(13));
                GUILayout.Label($"  Block: {enemy.Entity.CurrentBlock}", Style(13));

                // Intent
                string intentLine = BuildIntentDisplay(enemy.CurrentIntent);
                GUILayout.Label(intentLine, Bold(13));

                foreach (var kv in enemy.Entity.GetAllStatuses())
                    GUILayout.Label($"  {kv.Key}: {kv.Value}", Style(13));

                GUILayout.Space(5);
            }
            GUILayout.EndVertical();

            // ── Hand ─────────────────────────────────────────────
            GUILayout.BeginVertical("box", GUILayout.Width(252));
            GUILayout.Label($"── HAND ({_deckManager.HandCount}) ──", Bold(14));
            GUILayout.Label(
                $"Draw: {_deckManager.DrawCount}  Disc: {_deckManager.DiscardCount}",
                Style(13));
            GUILayout.Label(
                $"Energy: {_combatManager.GetCurrentEnergy()} / {_combatManager.GetMaxEnergy()}",
                Bold(14));
            GUILayout.Space(5);

            _handScroll = GUILayout.BeginScrollView(_handScroll, GUILayout.Height(216));
            foreach (var card in _deckManager.Hand)
            {
                int cost = card.GetEffectiveCost();
                bool canPlay = isPlayerTurn && cost <= _combatManager.GetCurrentEnergy();
                GUI.enabled = canPlay;
                if (GUILayout.Button($"[{cost}e] {card.Data.cardName}", GUILayout.Height(34)))
                    PlayCard(card);
                GUI.enabled = true;
            }
            GUILayout.EndScrollView();

            GUILayout.Space(7);
            GUI.enabled = isPlayerTurn && !_combatManager.IsQueueProcessing;
            if (GUILayout.Button("End Turn ▶", GUILayout.Height(43)))
                _combatManager.EndPlayerTurn();
            GUI.enabled = true;

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            DrawLog(156);
        }

        void PlayCard(CardInstance card)
        {
            CombatEntity target = null;
            foreach (var e in _activeEnemies)
                if (!e.IsDead) { target = e.Entity; break; }

            if (!_combatManager.TryPlayCard(card, target))
                Log($"✗ Cannot play {card.Data.cardName}");
        }

        void HandleCombatComplete(CombatResult result)
        {
            if (_playerEntity != null)
                _currentHP = _playerEntity.CurrentHealth;

            if (!result.WasVictory)
            {
                _state = HarnessState.Defeat;
                Log("💀 DEFEAT.");
                return;
            }

            _roomsCleared++;

            // Gold from combat result + room-type bonus
            int goldEarned = result.GoldEarned + GoldBonusForRoom(_activeChoice?.RoomType ?? RoomType.NormalCombat);
            _gold += goldEarned;
            Log($"\n🏆 Victory!  +{goldEarned}g  (total: {_gold}g)");

            // Always show reward screen — roll cards if CombatManager returned none
            _rewardChoices.Clear();

            if (result.Reward?.CardChoices != null && result.Reward.CardChoices.Count > 0)
            {
                _rewardChoices.AddRange(result.Reward.CardChoices);
            }
            else
            {
                var pool = new List<CardData>(_runtimeCardPool);
                var used = new HashSet<string>();
                int attempts = 0;
                while (_rewardChoices.Count < 3 && pool.Count > 0 && attempts++ < 30)
                {
                    int idx = Random.Range(0, pool.Count);
                    CardData pick = pool[idx];
                    pool.RemoveAt(idx);
                    if (used.Contains(pick.cardID)) continue;
                    _rewardChoices.Add(pick);
                    used.Add(pick.cardID);
                }
            }

            _state = HarnessState.Reward;
        }

        // ============================================================
        // REWARD
        // ============================================================

        void DrawReward()
        {
            GUILayout.Label("── Card Reward ──", Bold(19));
            GUILayout.Label("Pick one card to add to your deck, or skip.", Style(16));
            GUILayout.Space(12);

            GUILayout.BeginHorizontal();

            foreach (var card in _rewardChoices)
            {
                GUILayout.BeginVertical("box", GUILayout.Width(240));
                GUILayout.Label(card.cardName, Bold(17));
                GUILayout.Label($"Cost: {card.energyCost}e  |  {card.cardType}  |  {card.rarity}", Style(14));
                GUILayout.Space(4);
                GUILayout.Label(card.BuildDescription(), Style(13));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Take", GUILayout.Height(36)))
                {
                    _masterDeck.Add(new CardInstance(card));
                    Log($"  + Added {card.cardName} to deck");
                    PostRoomComplete();
                }
                GUILayout.EndVertical();
                GUILayout.Space(7);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            if (GUILayout.Button("Skip Reward", GUILayout.Height(38), GUILayout.Width(168)))
            {
                Log("  Skipped reward.");
                PostRoomComplete();
            }

            DrawLog(108);
        }

        // ============================================================
        // SHOP
        // ============================================================

        void OpenShop()
        {
            _state = HarnessState.Shop;
            _shopCards.Clear();
            _shopActions.Clear();

            var pool = new List<CardData>(_runtimeCardPool);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                _shopCards.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            int removeCost = 75;
            _shopActions.Add(("Remove a card from deck", removeCost, () =>
            {
                if (_gold < removeCost) { Log("✗ Not enough gold"); return; }
                if (_masterDeck.Count == 0) { Log("✗ Deck is empty"); return; }
                _gold -= removeCost;
                var removed = _masterDeck[Random.Range(0, _masterDeck.Count)];
                _masterDeck.Remove(removed);
                Log($"  Removed {removed.Data.cardName} from deck");
            }
            ));

            Log("🛒 Shop open.");
        }

        void DrawShop()
        {
            GUILayout.Label("── Shop ──", Bold(19));
            GUILayout.Label($"💰 {_gold}g", Bold(16));
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            foreach (var card in _shopCards)
            {
                int price = ShopPrice(card.rarity);
                GUILayout.BeginVertical("box", GUILayout.Width(228));
                GUILayout.Label(card.cardName, Bold(16));
                GUILayout.Label($"Cost: {card.energyCost}e  |  {card.cardType}", Style(13));
                GUILayout.Label(card.BuildDescription(), Style(13));
                GUILayout.FlexibleSpace();
                GUI.enabled = _gold >= price;
                if (GUILayout.Button($"Buy  ({price}g)", GUILayout.Height(34)))
                {
                    _gold -= price;
                    _masterDeck.Add(new CardInstance(card));
                    _shopCards.Remove(card);
                    Log($"  Bought {card.cardName} for {price}g");
                    break;
                }
                GUI.enabled = true;
                GUILayout.EndVertical();
                GUILayout.Space(7);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("── Services ──", Bold(14));
            foreach (var (label, cost, action) in _shopActions)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{label} — {cost}g", Style(14), GUILayout.Width(360));
                GUI.enabled = _gold >= cost;
                if (GUILayout.Button("Purchase", GUILayout.Width(108), GUILayout.Height(31)))
                    action();
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(12);
            if (GUILayout.Button("Leave Shop", GUILayout.Height(38), GUILayout.Width(156)))
            {
                _roomsCleared++;
                PostRoomComplete();
            }

            DrawLog(108);
        }

        int ShopPrice(CardRarity rarity) => rarity switch
        {
            CardRarity.Common => 50,
            CardRarity.Uncommon => 75,
            CardRarity.Rare => 125,
            _ => 50
        };

        // ============================================================
        // REST
        // ============================================================

        void DrawRest()
        {
            GUILayout.Label("── Rest Site ──", Bold(19));
            GUILayout.Space(10);

            int healAmount = Mathf.RoundToInt(_maxHP * 0.30f);

            GUILayout.BeginHorizontal();

            // Heal option
            GUILayout.BeginVertical("box", GUILayout.Width(264));
            GUILayout.Label("🔥 Rest & Heal", Bold(17));
            GUILayout.Label(
                $"Restore {healAmount} HP  ({_currentHP} → {Mathf.Min(_currentHP + healAmount, _maxHP)})",
                Style(14));
            GUILayout.Space(5);
            if (GUILayout.Button("Rest", GUILayout.Height(38)))
            {
                _currentHP = Mathf.Min(_currentHP + healAmount, _maxHP);
                int gold = GoldBonusForRoom(RoomType.Rest);
                if (gold > 0) { _gold += gold; Log($"  Found {gold}g while resting."); }
                Log($"  HP restored to {_currentHP}");
                _roomsCleared++;
                PostRoomComplete();
            }
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Upgrade option
            GUILayout.BeginVertical("box", GUILayout.Width(264));
            GUILayout.Label("⚒ Upgrade a Card", Bold(17));
            GUILayout.Label("Permanently upgrade one card in your deck.", Style(14));
            GUILayout.Space(5);

            var upgradeable = _masterDeck.FindAll(c => c.CanUpgrade());
            if (upgradeable.Count == 0)
            {
                GUILayout.Label("No upgradeable cards.", Style(13));
            }
            else
            {
                _deckScroll = GUILayout.BeginScrollView(_deckScroll, GUILayout.Height(132));
                foreach (var card in upgradeable)
                {
                    if (GUILayout.Button($"Upgrade  {card.Data.cardName}", GUILayout.Height(31)))
                    {
                        card.Upgrade();
                        Log($"  Upgraded {card.Data.cardName} → Tier {(int)card.CurrentTier}");
                        _roomsCleared++;
                        PostRoomComplete();
                        break;
                    }
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            DrawLog(108);
        }

        // ============================================================
        // EVENT
        // ============================================================

        void DrawEvent()
        {
            if (_activeEvent == null)
            {
                GUILayout.Label("(No event data)", Style(17));
                if (GUILayout.Button("Continue", GUILayout.Height(38)))
                {
                    _roomsCleared++;
                    PostRoomComplete();
                }
                return;
            }

            GUILayout.Label($"── {_activeEvent.eventName} ──", Bold(19));
            GUILayout.Space(7);
            GUILayout.Label(_activeEvent.flavourText, Style(16));
            GUILayout.Space(12);

            foreach (var choice in _activeEvent.choices)
            {
                bool canSelect = CanAffordEventChoice(choice);
                GUI.enabled = canSelect;

                GUILayout.BeginVertical("box");
                GUILayout.Label(choice.choiceText, Bold(16));

                if (choice.goldCost > 0) GUILayout.Label($"  Cost: {choice.goldCost}g", Style(13));
                if (choice.hpCost > 0) GUILayout.Label($"  Cost: {choice.hpCost} HP", Style(13));
                if (choice.goldReward > 0) GUILayout.Label($"  Gain: {choice.goldReward}g", Style(13));
                if (choice.hpChange > 0) GUILayout.Label($"  Heal: {choice.hpChange} HP", Style(13));
                if (choice.hpChange < 0) GUILayout.Label($"  Take: {-choice.hpChange} damage", Style(13));
                if (choice.gainRandomCard) GUILayout.Label("  Gain a random card", Style(13));
                if (!canSelect) GUILayout.Label("  [Cannot afford]", Style(13));

                if (GUILayout.Button("Choose", GUILayout.Height(34)))
                    ResolveEventChoice(choice);

                GUILayout.EndVertical();
                GUI.enabled = true;
                GUILayout.Space(5);
            }

            DrawLog(84);
        }

        bool CanAffordEventChoice(EventChoice choice)
        {
            if (choice.goldCost > 0 && _gold < choice.goldCost) return false;
            if (choice.hpCost > 0 && _currentHP <= choice.hpCost) return false;
            return true;
        }

        void ResolveEventChoice(EventChoice choice)
        {
            if (choice.goldCost > 0) _gold -= choice.goldCost;
            if (choice.hpCost > 0) _currentHP -= choice.hpCost;
            if (choice.goldReward > 0) _gold += choice.goldReward;

            if (choice.essenceReward > 0)
                Log($"  +{choice.essenceReward} Essence (not tracked in harness)");

            if (choice.hpChange != 0)
            {
                _currentHP = Mathf.Clamp(_currentHP + choice.hpChange, 0, _maxHP);
                if (_currentHP <= 0) { _state = HarnessState.Defeat; return; }
            }

            if (choice.gainRandomCard && _runtimeCardPool.Count > 0)
            {
                var card = _runtimeCardPool[Random.Range(0, _runtimeCardPool.Count)];
                _masterDeck.Add(new CardInstance(card));
                Log($"  + Gained {card.cardName}");
            }

            // Small gold bonus for completing an event room
            int bonus = GoldBonusForRoom(RoomType.Mystery);
            if (bonus > 0) { _gold += bonus; Log($"  +{bonus}g (event completion)"); }

            Log($"  {choice.outcomeText}");
            _roomsCleared++;
            PostRoomComplete();
        }

        // ============================================================
        // TREASURE
        // ============================================================

        void OpenTreasure()
        {
            _rewardChoices.Clear();
            var used = new HashSet<string>();
            int attempts = 0;
            while (_rewardChoices.Count < 3 && _runtimeCardPool.Count > 0 && attempts++ < 30)
            {
                var pick = _runtimeCardPool[Random.Range(0, _runtimeCardPool.Count)];
                if (used.Contains(pick.cardID)) continue;
                _rewardChoices.Add(pick);
                used.Add(pick.cardID);
            }

            // Treasure rooms also give some gold
            int gold = GoldBonusForRoom(RoomType.Treasure);
            if (gold > 0) { _gold += gold; Log($"  Found {gold}g in the treasure room!"); }

            _roomsCleared++;
            _state = HarnessState.Reward;
            Log("💎 Treasure room — pick a card.");
        }

        // ============================================================
        // DECK REVIEW OVERLAY
        // ============================================================

        void DrawDeckReview()
        {
            GUILayout.Label("── Deck Review ──", Bold(19));
            GUILayout.Label($"{_masterDeck.Count} cards total", Style(16));
            GUILayout.Space(7);

            var sorted = new List<CardInstance>(_masterDeck);
            sorted.Sort((a, b) =>
            {
                int c = a.GetEffectiveCost().CompareTo(b.GetEffectiveCost());
                return c != 0 ? c : string.Compare(a.Data.cardName, b.Data.cardName);
            });

            _deckScroll = GUILayout.BeginScrollView(_deckScroll, GUILayout.Height(Screen.height - 180));
            foreach (var card in sorted)
            {
                GUILayout.BeginHorizontal("box");
                string tier = card.CurrentTier != UpgradeTier.Base
                    ? $" +{(int)card.CurrentTier}" : "";
                GUILayout.Label($"[{card.GetEffectiveCost()}e]", Bold(14), GUILayout.Width(42));
                GUILayout.Label(card.Data.cardName + tier, Bold(14), GUILayout.Width(192));
                GUILayout.Label(card.Data.cardType.ToString(), Style(13), GUILayout.Width(72));
                GUILayout.Label(card.Data.rarity.ToString(), Style(13), GUILayout.Width(84));
                GUILayout.Label(card.Data.BuildDescription(), Style(13));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        // ============================================================
        // VICTORY / DEFEAT
        // ============================================================

        void DrawVictory()
        {
            GUILayout.Label("🏆  VICTORY — Run Complete!", Bold(24));
            GUILayout.Space(12);
            GUILayout.Label($"Floors cleared:  {_floor}", Style(17));
            GUILayout.Label($"Rooms cleared:   {_roomsCleared}", Style(17));
            GUILayout.Label($"Gold remaining:  {_gold}g", Style(17));
            GUILayout.Label($"Final deck:      {_masterDeck.Count} cards", Style(17));
            GUILayout.Space(20);
            if (GUILayout.Button("Play Again", GUILayout.Height(48), GUILayout.Width(180)))
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        void DrawDefeat()
        {
            GUILayout.Label("💀  DEFEAT", Bold(24));
            GUILayout.Space(12);
            GUILayout.Label($"Survived {_roomsCleared} room(s) across {_floor} floor(s).", Style(17));
            GUILayout.Space(20);
            if (GUILayout.Button("Try Again", GUILayout.Height(48), GUILayout.Width(180)))
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // ============================================================
        // ROOM COMPLETION
        // ============================================================

        void PostRoomComplete()
        {
            if (_activeChoice?.RoomType == RoomType.Boss)
            {
                _floor++;
                _roomsCleared = 0;
                _navigation.InitializeFloor(_runtimeFloorConfig);
                Log($"\n🏁 Floor cleared! Advancing to floor {_floor}...");
            }

            if (_floor > 3)
            {
                _state = HarnessState.Victory;
                return;
            }

            GoToNavigation();
        }

        void GoToNavigation()
        {
            _currentChoices = _navigation.GenerateChoices(BuildFakeRunState());
            _state = HarnessState.Navigation;

            Log($"\n── Navigation  |  Floor {_floor}  Room {_roomsCleared + 1}/{_navigation.TotalRoomsThisFloor} ──");
            foreach (var c in _currentChoices)
                Log($"  {RoomIcon(c.RoomType)} [{c.Label}]  {c.Hint}");
        }

        RunState BuildFakeRunState()
        {
            var rs = new RunState(_playerClass, _maxHP, _masterDeck);
            rs.SetCurrentHP(_currentHP);
            for (int i = 0; i < _roomsCleared; i++) rs.AdvanceRoom();
            return rs;
        }

        // ============================================================
        // GOLD BONUS PER ROOM TYPE
        // Gold from combat comes from CombatResult — this covers
        // the flat bonus for non-combat rooms and elite/boss bumps.
        // ============================================================

        int GoldBonusForRoom(RoomType type) => type switch
        {
            RoomType.NormalCombat => 0,         // CombatResult already handles this
            RoomType.EliteCombat => 15,        // Extra on top of CombatResult gold
            RoomType.Boss => 30,        // Extra on top of CombatResult gold
            RoomType.Shop => 0,
            RoomType.Rest => Random.Range(0, 2) == 0 ? Random.Range(5, 16) : 0,
            RoomType.Treasure => Random.Range(20, 41),
            RoomType.Mystery => Random.Range(5, 16),  // Event completion bonus
            _ => 0
        };

        // ============================================================
        // INTENT DISPLAY
        // Infers a readable description from EnemyAction fields.
        // If EnemyAction.customIntentDescription is non-empty, uses that.
        // Requires adding to EnemyAction.cs:
        //   public string customIntentDescription = "";
        // ============================================================

        string BuildIntentDisplay(EnemyAction intent)
        {
            if (intent == null) return "  ❓ Unknown";

            // Custom description takes full priority
            // Uncomment when customIntentDescription is added to EnemyAction:
            // if (!string.IsNullOrEmpty(intent.customIntentDescription))
            //     return $"  {IntentIcon(intent.intentType)} {intent.customIntentDescription}";

            string icon = IntentIcon(intent.intentType);
            string name = intent.actionName;

            return intent.intentType switch
            {
                EnemyIntent.Attack when intent.hitCount > 1 =>
                    $"  {icon} {name}  {intent.damage} × {intent.hitCount} = {intent.damage * intent.hitCount} dmg",

                EnemyIntent.Attack =>
                    $"  {icon} {name}  {intent.damage} dmg",

                EnemyIntent.Defend =>
                    $"  {icon} {name}  +{intent.selfBlock} Block",

                EnemyIntent.Buff =>
                    $"  {icon} {name}",

                EnemyIntent.Debuff =>
                    $"  {icon} {name}",

                _ => $"  {icon} {name}"
            };
        }

        // ============================================================
        // RUNTIME POOL BUILDERS
        // SO data takes priority; hardcoded fallbacks fill any gaps.
        // ============================================================

        void BuildRuntimePools()
        {
            // Card pool
            if (cardPool != null && cardPool.Length > 0)
            {
                _runtimeCardPool.AddRange(cardPool);
                Log($"✓ Card pool: {_runtimeCardPool.Count} cards from ScriptableObjects");
            }
            else
            {
                BuildFallbackCardPool();
                Log($"✓ Card pool: {_runtimeCardPool.Count} cards from fallback");
            }

            // Normal enemies
            if (normalEnemyPool != null && normalEnemyPool.Length > 0)
                _runtimeNormalPool.AddRange(normalEnemyPool);
            else
                BuildFallbackNormalPool();

            // Elite enemies
            if (eliteEnemyPool != null && eliteEnemyPool.Length > 0)
                _runtimeElitePool.AddRange(eliteEnemyPool);
            else
                BuildFallbackElitePool();

            // Boss enemies
            if (bossEnemyPool != null && bossEnemyPool.Length > 0)
                _runtimeBossPool.AddRange(bossEnemyPool);
            else
                BuildFallbackBossPool();

            // Events
            if (eventPool != null && eventPool.Length > 0)
                _runtimeEventPool.AddRange(eventPool);
            else
                BuildFallbackEventPool();

            Log($"✓ Enemies — Normal:{_runtimeNormalPool.Count} " +
                $"Elite:{_runtimeElitePool.Count}  Boss:{_runtimeBossPool.Count}");
            Log($"✓ Events: {_runtimeEventPool.Count}");
        }

        void BuildRuntimeFloorConfig()
        {
            if (floorConfig != null)
            {
                _runtimeFloorConfig = floorConfig;
                // Override pools with the runtime ones so SO-vs-hardcoded stays consistent
                _runtimeFloorConfig.normalEnemyPool = _runtimeNormalPool.ToArray();
                _runtimeFloorConfig.eliteEnemyPool = _runtimeElitePool.ToArray();
                _runtimeFloorConfig.bossEnemies = _runtimeBossPool.ToArray();
                _runtimeFloorConfig.eventPool = _runtimeEventPool.ToArray();
                Log("✓ FloorConfig: from ScriptableObject");
            }
            else
            {
                _runtimeFloorConfig = ScriptableObject.CreateInstance<FloorConfig>();
                _runtimeFloorConfig.floorName = "Test Floor";
                _runtimeFloorConfig.floorNumber = 1;
                _runtimeFloorConfig.minRooms = 5;
                _runtimeFloorConfig.maxRooms = 7;
                _runtimeFloorConfig.minChoices = 2;
                _runtimeFloorConfig.maxChoices = 3;

                _runtimeFloorConfig.normalCombatWeight = 40f;
                _runtimeFloorConfig.eliteWeight = 10f;
                _runtimeFloorConfig.shopWeight = 15f;
                _runtimeFloorConfig.mysteryWeight = 20f;
                _runtimeFloorConfig.restWeight = 12f;
                _runtimeFloorConfig.treasureWeight = 3f;

                _runtimeFloorConfig.mysteryEventWeight = 60f;
                _runtimeFloorConfig.mysteryCombatWeight = 30f;
                _runtimeFloorConfig.mysteryShopWeight = 10f;

                _runtimeFloorConfig.guaranteedShopAtRoom = 3;
                _runtimeFloorConfig.eliteUnlockAfterRoom = 2;
                _runtimeFloorConfig.minRoomsBetweenElites = 2;

                _runtimeFloorConfig.normalEnemyPool = _runtimeNormalPool.ToArray();
                _runtimeFloorConfig.eliteEnemyPool = _runtimeElitePool.ToArray();
                _runtimeFloorConfig.bossEnemies = _runtimeBossPool.ToArray();
                _runtimeFloorConfig.eventPool = _runtimeEventPool.ToArray();
                Log("✓ FloorConfig: fallback (5–7 rooms for fast testing)");
            }
        }

        // ============================================================
        // HARDCODED FALLBACKS
        // Only used when no ScriptableObjects are assigned.
        // ============================================================

        void BuildFallbackStarterDeck()
        {
            // Requires at least Strike and Defend in the card pool
            for (int i = 0; i < 5; i++) _masterDeck.Add(MakeCardByName("Strike"));
            for (int i = 0; i < 4; i++) _masterDeck.Add(MakeCardByName("Defend"));
            _masterDeck.Add(MakeCardByName("VenomBlade"));
            _masterDeck.Add(MakeCardByName("Acrobatics"));
        }

        CardInstance MakeCardByName(string name)
        {
            foreach (var cd in _runtimeCardPool)
                if (cd.cardName == name) return new CardInstance(cd);
            return new CardInstance(_runtimeCardPool[0]);
        }

        void BuildFallbackCardPool()
        {
            _runtimeCardPool.Add(MakeCardData("Strike", CardType.Attack, 1, new[] { (6, DamageType.Physical) }));
            _runtimeCardPool.Add(MakeCardData("Heavy Strike", CardType.Attack, 2, new[] { (14, DamageType.Physical) }));
            _runtimeCardPool.Add(MakeCardData("Defend", CardType.Skill, 1, null, blockGain: 5));
            _runtimeCardPool.Add(MakeCardData("Iron Wave", CardType.Skill, 1, new[] { (5, DamageType.Physical) }, blockGain: 5));
            _runtimeCardPool.Add(MakeCardData("VenomBlade", CardType.Attack, 2, new[] { (5, DamageType.Physical) }, poisonApply: 3));
            _runtimeCardPool.Add(MakeCardData("Whirlwind", CardType.Attack, 2, new[] { (5, DamageType.Physical) }));
            _runtimeCardPool.Add(MakeCardData("Acrobatics", CardType.Skill, 0, null, drawCards: 2));
            _runtimeCardPool.Add(MakeCardData("Barricade", CardType.Power, 3, null, blockGain: 12));
        }

        void BuildFallbackNormalPool()
        {
            _runtimeNormalPool.Add(MakeEnemy("Slime", 24, new[] { ("Chomp", 6, 1), ("Ooze", 0, 0) }));
            _runtimeNormalPool.Add(MakeEnemy("Cultist", 18, new[] { ("Ritual", 0, 0), ("Dark Strike", 5, 2) }));
            _runtimeNormalPool.Add(MakeEnemy("Rat", 14, new[] { ("Bite", 4, 1), ("Scratch", 3, 2) }));
        }

        void BuildFallbackElitePool()
        {
            _runtimeElitePool.Add(MakeEnemy("Slime King", 50, new[] { ("Engulf", 12, 1), ("Split", 0, 0), ("Slam", 8, 2) }));
            _runtimeElitePool.Add(MakeEnemy("Dark Priest", 45, new[] { ("Curse", 0, 0), ("Dark Beam", 10, 1) }));
        }

        void BuildFallbackBossPool()
        {
            _runtimeBossPool.Add(MakeEnemy("The Guardian", 100,
                new[] { ("Shield Bash", 10, 1), ("Crush", 18, 1), ("Wall", 0, 0) }));
        }

        void BuildFallbackEventPool()
        {
            var shrine = ScriptableObject.CreateInstance<EventData>();
            shrine.eventName = "Forgotten Shrine";
            shrine.eventID = "shrine";
            shrine.flavourText = "A crumbling shrine glows faintly. You feel a pull toward it.";
            shrine.choices = new[]
            {
                new EventChoice { choiceText = "Pray (30 HP)", hpCost = 30, goldReward = 50,
                    outcomeText = "The shrine accepts your offering. Gold spills from the base." },
                new EventChoice { choiceText = "Desecrate", hpChange = -10,
                    outcomeText = "Bad idea. Something punishes you." },
                new EventChoice { choiceText = "Walk away",
                    outcomeText = "Probably for the best." }
            };

            var merchant = ScriptableObject.CreateInstance<EventData>();
            merchant.eventName = "Wandering Merchant";
            merchant.eventID = "wandering_merchant";
            merchant.flavourText = "A hunched figure offers you a strange glowing card.";
            merchant.choices = new[]
            {
                new EventChoice { choiceText = "Buy the card (50g)", goldCost = 50, gainRandomCard = true,
                    outcomeText = "Worth it." },
                new EventChoice { choiceText = "Ignore them",
                    outcomeText = "Their eyes follow you." }
            };

            _runtimeEventPool.Add(shrine);
            _runtimeEventPool.Add(merchant);
        }

        // ============================================================
        // FACTORY HELPERS
        // ============================================================

        CardData MakeCardData(string name, CardType type, int cost,
            (int mag, DamageType dtype)[] dmgEffects = null,
            int blockGain = 0, int poisonApply = 0, int drawCards = 0)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.cardName = name;
            data.cardID = name.ToLower().Replace(" ", "_");
            data.cardType = type;
            data.energyCost = cost;
            data.owner = _playerClass;
            data.rarity = CardRarity.Common;

            var effects = new List<CardEffect>();

            if (dmgEffects != null)
            {
                foreach (var (mag, dtype) in dmgEffects)
                {
                    var fx = ScriptableObject.CreateInstance<DealDamageEffect>();
                    fx.trigger = EffectTrigger.OnPlay;
                    fx.target = EffectTarget.SingleEnemy;
                    fx.damageType = dtype;
                    fx.baseMagnitude = mag;
                    effects.Add(fx);
                }
            }

            if (blockGain > 0)
            {
                var fx = ScriptableObject.CreateInstance<GainBlockEffect>();
                fx.trigger = EffectTrigger.OnPlay;
                fx.target = EffectTarget.Self;
                fx.baseMagnitude = blockGain;
                effects.Add(fx);
            }

            if (poisonApply > 0)
            {
                var fx = ScriptableObject.CreateInstance<ApplyStatusEffect>();
                fx.trigger = EffectTrigger.OnPlay;
                fx.target = EffectTarget.SingleEnemy;
                fx.statusType = StatusType.Poison;
                fx.baseMagnitude = poisonApply;
                effects.Add(fx);
            }

            if (drawCards > 0)
            {
                var fx = ScriptableObject.CreateInstance<DrawCardsEffect>();
                fx.trigger = EffectTrigger.OnPlay;
                fx.baseMagnitude = drawCards;
                effects.Add(fx);
            }

            data.effects = effects.ToArray();
            return data;
        }

        EnemyData MakeEnemy(string name, int hp, (string actionName, int dmg, int hits)[] actions)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = name;
            data.enemyID = name.ToLower().Replace(" ", "_");
            data.baseHP = hp;

            var actionList = new List<EnemyAction>();
            foreach (var (aName, dmg, hits) in actions)
            {
                var a = ScriptableObject.CreateInstance<EnemyAction>();
                a.actionName = aName;
                a.damage = dmg;
                a.hitCount = Mathf.Max(1, hits);

                if (dmg > 0)
                    a.intentType = EnemyIntent.Attack;
                else if (hits == 0)
                { a.intentType = EnemyIntent.Defend; a.selfBlock = 8; }
                else
                    a.intentType = EnemyIntent.Buff;

                actionList.Add(a);
            }

            data.actionPattern = actionList.ToArray();
            return data;
        }

        // ============================================================
        // LOG
        // ============================================================

        void DrawLog(float height)
        {
            GUILayout.Space(7);
            GUILayout.Label("── Log ──", Bold(13));
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.Height(height));
            GUILayout.Label(_log, Style(13));
            GUILayout.EndScrollView();
        }

        void Log(string msg)
        {
            _log += msg + "\n";
            _logScroll.y = float.MaxValue;
            Debug.Log($"[Harness] {msg}");
        }

        // ============================================================
        // GUI STYLE HELPERS
        // ============================================================

        GUIStyle Bold(int size = 14)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize = size;
            return s;
        }

        GUIStyle Style(int size = 14)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = size;
            s.wordWrap = true;
            return s;
        }

        string RoomIcon(RoomType type) => type switch
        {
            RoomType.NormalCombat => "⚔",
            RoomType.EliteCombat => "💀",
            RoomType.Boss => "👑",
            RoomType.Shop => "🛒",
            RoomType.Mystery => "❓",
            RoomType.Rest => "🔥",
            RoomType.Treasure => "💎",
            _ => "•"
        };

        string IntentIcon(EnemyIntent intent) => intent switch
        {
            EnemyIntent.Attack => "⚔",
            EnemyIntent.Defend => "🛡",
            EnemyIntent.Buff => "✨",
            EnemyIntent.Debuff => "💀",
            _ => "❓"
        };
    }
}