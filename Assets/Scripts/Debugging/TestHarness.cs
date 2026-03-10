// ============================================================
//  TestHarness.cs
//  Full run-loop test harness. IMGUI only. No ScriptableObjects
//  in the inspector — all data comes from Concrete* classes.
//
//  SETUP:
//    1. New empty scene
//    2. Empty GameObject → Add TestHarness
//    3. Assign DefenseConfig in Inspector (only required field)
//    4. Press Play
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
        // Inspector — only what cannot be hardcoded
        // ----------------------------------------------------------

        [Header("Required")]
        public DefenseConfig defenseConfig;

        [Header("Settings")]
        public CharacterClass startingCharacter = CharacterClass.Orin;
        public float          enemyActionDelay  = 0.2f;

        // ----------------------------------------------------------
        // Systems
        // ----------------------------------------------------------

        private CombatManager       _combatManager;
        private DeckManager         _deckManager;
        private NavigationSystem    _navigation;
        private MysteryRoomResolver _mysteryResolver;
        private FloorConfig         _floorConfig;

        // ----------------------------------------------------------
        // Run state
        // ----------------------------------------------------------

        private HarnessState _state       = HarnessState.Navigation;
        private HarnessState _returnState = HarnessState.Navigation;

        private CharacterDefinition    _charDef;
        private int                    _currentHP;
        private int                    _maxHP;
        private int                    _gold;
        private int                    _floor        = 1;
        private int                    _roomsCleared = 0;

        private List<CardInstance>     _masterDeck  = new();
        private List<CardData>         _cardPool    = new();
        private CombatEntity           _playerEntity;

        // ----------------------------------------------------------
        // Per-room state
        // ----------------------------------------------------------

        private List<RoomChoice>    _currentChoices = new();
        private RoomChoice          _activeChoice;
        private List<EnemyInstance> _activeEnemies  = new();

        // Reward
        private List<CardData> _rewardChoices = new();

        // Shop
        private List<CardData>                                       _shopCards   = new();
        private List<(string label, int cost, System.Action action)> _shopActions = new();

        // Event
        private EventData _activeEvent;

        // ----------------------------------------------------------
        // IMGUI scroll state
        // ----------------------------------------------------------

        private Vector2 _logScroll;
        private Vector2 _handScroll;
        private Vector2 _deckScroll;
        private string  _log = "";

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

            // Character
            _charDef   = ConcreteCharacters.Get(startingCharacter);
            _maxHP     = _charDef.BaseHP;
            _currentHP = _maxHP;
            _gold      = 99;

            // Starter deck and card pool from Concrete
            _masterDeck.AddRange(_charDef.BuildStarterDeck());
            _cardPool.AddRange(ConcreteCharacters.GetCardPool(startingCharacter));

            Log($"✓ Character: {_charDef.CharacterName}  HP:{_maxHP}  " +
                $"Deck:{_masterDeck.Count}  Pool:{_cardPool.Count} cards");

            // Unity components
            var deckGO     = new GameObject("DeckManager");
            _deckManager   = deckGO.AddComponent<DeckManager>();
            _deckManager.maxHandSize = 10;

            var cmGO       = new GameObject("CombatManager");
            _combatManager = cmGO.AddComponent<CombatManager>();
            _combatManager.deckManager       = _deckManager;
            _combatManager.cardsDrawnPerTurn = _charDef.CardsDrawn;
            _combatManager.energyPerTurn     = _charDef.BaseEnergy;
            _combatManager.enemyActionDelay  = enemyActionDelay;
            _combatManager.defenseConfig     = defenseConfig;

            _combatManager.OnStateChanged   += s => Log($"  → Combat state: {s}");
            CombatEvents.OnCombatComplete += HandleCombatComplete;

            _deckManager.OnCardDrawn      += c  => Log($"  Drew: {c.Data.cardName}");
            _deckManager.OnCardDiscarded  += c  => Log($"  Discarded: {c.Data.cardName}");
            _deckManager.OnDeckReshuffled += () => Log("  🔀 Reshuffled");

            // Floor config and navigation — built entirely in code
            _floorConfig     = BuildFloorConfig();
            _navigation      = new NavigationSystem(_floorConfig);
            _navigation.InitializeFloor(_floorConfig);
            _mysteryResolver = new MysteryRoomResolver(_floorConfig);

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
                case HarnessState.Combat:     DrawCombat();     break;
                case HarnessState.Reward:     DrawReward();     break;
                case HarnessState.Shop:       DrawShop();       break;
                case HarnessState.Rest:       DrawRest();       break;
                case HarnessState.Event:      DrawEvent();      break;
                case HarnessState.DeckReview: DrawDeckReview(); break;
                case HarnessState.Victory:    DrawVictory();    break;
                case HarnessState.Defeat:     DrawDefeat();     break;
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

            GUILayout.Label($"❤ {_currentHP}/{_maxHP}",  Bold(17), GUILayout.Width(132));
            GUILayout.Label($"💰 {_gold}g",               Bold(17), GUILayout.Width(96));
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
                _state       = HarnessState.DeckReview;
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
                :  choice.MysteryOutcome == MysteryOutcome.Shop         ? RoomType.Shop
                :  RoomType.Mystery)
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
                : EnemyDefinitionHelper.ToDataList(ConcreteEnemies.All()).GetRange(0, 1);

            foreach (var eData in enemyDataList)
            {
                var inst = new EnemyInstance(eData, defenseConfig);
                inst.Entity.OnDamageTaken += bd => Log($"  {eData.enemyName} hit — total:{bd.TotalDamage}");
                inst.Entity.OnDeath       += () => Log($"  ☠ {eData.enemyName} defeated!");
                _activeEnemies.Add(inst);
            }

            Log($"\n⚔ Combat: {string.Join(", ", enemyDataList.ConvertAll(e => e.enemyName))}");

            _combatManager.StartCombat(
                enemyDataList,
                new List<CardInstance>(_masterDeck),
                _playerEntity,
                _cardPool,
                _charDef.Class
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
                GUILayout.Label($"Block:   {_playerEntity.CurrentBlock}",   Style(14));
                GUILayout.Label($"Armor:   {_playerEntity.CurrentArmor}",   Style(14));
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
                if (enemy.IsDead)
                {
                    GUILayout.Label($"[{enemy.Data.enemyName}: DEAD]", Style(13));
                    continue;
                }

                GUILayout.Label(enemy.Data.enemyName, Bold(14));
                GUILayout.Label($"  HP:    {enemy.Entity.CurrentHealth}/{enemy.Entity.MaxHealth}", Style(13));
                GUILayout.Label($"  Block: {enemy.Entity.CurrentBlock}", Style(13));
                GUILayout.Label(BuildIntentDisplay(enemy.CurrentIntent), Bold(13));

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
                int  cost    = card.GetEffectiveCost();
                bool canPlay = isPlayerTurn && cost <= _combatManager.GetCurrentEnergy();
                GUI.enabled  = canPlay;
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

            int goldEarned = result.GoldEarned + GoldBonusForRoom(_activeChoice?.RoomType ?? RoomType.NormalCombat);
            _gold += goldEarned;
            Log($"\n🏆 Victory!  +{goldEarned}g  (total: {_gold}g)");

            // Always show reward — use CombatManager result, or roll from card pool directly
            _rewardChoices.Clear();

            if (result.Reward?.CardChoices != null && result.Reward.CardChoices.Count > 0)
            {
                _rewardChoices.AddRange(result.Reward.CardChoices);
            }
            else
            {
                var pool     = new List<CardData>(_cardPool);
                var used     = new HashSet<string>();
                int attempts = 0;
                while (_rewardChoices.Count < 3 && pool.Count > 0 && attempts++ < 30)
                {
                    int      idx  = Random.Range(0, pool.Count);
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

            var pool = new List<CardData>(_cardPool);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                _shopCards.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            int removeCost = 75;
            _shopActions.Add(("Remove a card from deck", removeCost, () =>
            {
                if (_gold < removeCost)      { Log("✗ Not enough gold"); return; }
                if (_masterDeck.Count == 0)  { Log("✗ Deck is empty");   return; }
                _gold -= removeCost;
                var removed = _masterDeck[Random.Range(0, _masterDeck.Count)];
                _masterDeck.Remove(removed);
                Log($"  Removed {removed.Data.cardName} from deck");
            }));

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
            CardRarity.Common   => 50,
            CardRarity.Uncommon => 75,
            CardRarity.Rare     => 125,
            _                   => 50
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
                { _roomsCleared++; PostRoomComplete(); }
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

                if (choice.goldCost   > 0) GUILayout.Label($"  Cost: {choice.goldCost}g",       Style(13));
                if (choice.hpCost     > 0) GUILayout.Label($"  Cost: {choice.hpCost} HP",        Style(13));
                if (choice.goldReward > 0) GUILayout.Label($"  Gain: {choice.goldReward}g",      Style(13));
                if (choice.hpChange   > 0) GUILayout.Label($"  Heal: {choice.hpChange} HP",      Style(13));
                if (choice.hpChange   < 0) GUILayout.Label($"  Take: {-choice.hpChange} damage", Style(13));
                if (choice.gainRandomCard) GUILayout.Label("  Gain a random card",               Style(13));
                if (!canSelect)            GUILayout.Label("  [Cannot afford]",                  Style(13));

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
            if (choice.goldCost > 0 && _gold      < choice.goldCost) return false;
            if (choice.hpCost   > 0 && _currentHP <= choice.hpCost)  return false;
            return true;
        }

        void ResolveEventChoice(EventChoice choice)
        {
            if (choice.goldCost   > 0) _gold      -= choice.goldCost;
            if (choice.hpCost     > 0) _currentHP -= choice.hpCost;
            if (choice.goldReward > 0) _gold      += choice.goldReward;

            if (choice.essenceReward > 0)
                Log($"  +{choice.essenceReward} Essence (not tracked in harness)");

            if (choice.hpChange != 0)
            {
                _currentHP = Mathf.Clamp(_currentHP + choice.hpChange, 0, _maxHP);
                if (_currentHP <= 0) { _state = HarnessState.Defeat; return; }
            }

            if (choice.gainRandomCard && _cardPool.Count > 0)
            {
                var card = _cardPool[Random.Range(0, _cardPool.Count)];
                _masterDeck.Add(new CardInstance(card));
                Log($"  + Gained {card.cardName}");
            }

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
            var used     = new HashSet<string>();
            int attempts = 0;
            while (_rewardChoices.Count < 3 && _cardPool.Count > 0 && attempts++ < 30)
            {
                var pick = _cardPool[Random.Range(0, _cardPool.Count)];
                if (used.Contains(pick.cardID)) continue;
                _rewardChoices.Add(pick);
                used.Add(pick.cardID);
            }

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
                string tier = card.CurrentTier != UpgradeTier.Base ? $" +{(int)card.CurrentTier}" : "";
                GUILayout.Label($"[{card.GetEffectiveCost()}e]",  Bold(14),  GUILayout.Width(42));
                GUILayout.Label(card.Data.cardName + tier,         Bold(14),  GUILayout.Width(192));
                GUILayout.Label(card.Data.cardType.ToString(),     Style(13), GUILayout.Width(72));
                GUILayout.Label(card.Data.rarity.ToString(),       Style(13), GUILayout.Width(84));
                GUILayout.Label(card.Data.BuildDescription(),      Style(13));
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
            GUILayout.Label($"Floors cleared:  {_floor}",                Style(17));
            GUILayout.Label($"Rooms cleared:   {_roomsCleared}",         Style(17));
            GUILayout.Label($"Gold remaining:  {_gold}g",                Style(17));
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
                _navigation.InitializeFloor(_floorConfig);
                Log($"\n🏁 Floor cleared! Advancing to floor {_floor}...");
            }

            if (_floor > 3) { _state = HarnessState.Victory; return; }

            GoToNavigation();
        }

        void GoToNavigation()
        {
            _currentChoices = _navigation.GenerateChoices(BuildFakeRunState());
            _state          = HarnessState.Navigation;

            Log($"\n── Navigation  |  Floor {_floor}  Room {_roomsCleared + 1}/{_navigation.TotalRoomsThisFloor} ──");
            foreach (var c in _currentChoices)
                Log($"  {RoomIcon(c.RoomType)} [{c.Label}]  {c.Hint}");
        }

        RunState BuildFakeRunState()
        {
            var rs = new RunState(_charDef.Class, _maxHP, _masterDeck);
            rs.SetCurrentHP(_currentHP);
            for (int i = 0; i < _roomsCleared; i++) rs.AdvanceRoom();
            return rs;
        }

        // ============================================================
        // FLOOR CONFIG — built entirely in code from Concrete pools
        // ============================================================

        FloorConfig BuildFloorConfig()
        {
            var cfg = ScriptableObject.CreateInstance<FloorConfig>();
            cfg.floorName   = "Floor";
            cfg.floorNumber = 1;
            cfg.minRooms    = 5;
            cfg.maxRooms    = 7;
            cfg.minChoices  = 2;
            cfg.maxChoices  = 3;

            cfg.normalCombatWeight = 40f;
            cfg.eliteWeight        = 10f;
            cfg.shopWeight         = 15f;
            cfg.mysteryWeight      = 20f;
            cfg.restWeight         = 12f;
            cfg.treasureWeight     = 3f;

            cfg.mysteryEventWeight  = 60f;
            cfg.mysteryCombatWeight = 30f;
            cfg.mysteryShopWeight   = 10f;

            cfg.guaranteedShopAtRoom  = 3;
            cfg.eliteUnlockAfterRoom  = 2;
            cfg.minRoomsBetweenElites = 2;

            cfg.normalEnemyPool = EnemyDefinitionHelper.ToDataList(ConcreteEnemies.All()).ToArray();
            cfg.eliteEnemyPool  = EnemyDefinitionHelper.ToDataList(ConcreteEliteEnemies.All()).ToArray();
            cfg.bossEnemies     = EnemyDefinitionHelper.ToDataList(ConcreteBossEnemies.All()).ToArray();
            cfg.eventPool       = BuildEventPool();

            return cfg;
        }

        // Events still need EventData SOs for now — built here rather than scattered across TestHarness
        EventData[] BuildEventPool()
        {
            var shrine = ScriptableObject.CreateInstance<EventData>();
            shrine.eventName   = "Forgotten Shrine";
            shrine.eventID     = "shrine";
            shrine.flavourText = "A crumbling shrine glows faintly. You feel a pull toward it.";
            shrine.choices = new[]
            {
                new EventChoice { choiceText = "Pray (30 HP)", hpCost = 30, goldReward = 50,
                    outcomeText = "Gold spills from the base." },
                new EventChoice { choiceText = "Desecrate", hpChange = -10,
                    outcomeText = "Bad idea. Something punishes you." },
                new EventChoice { choiceText = "Walk away",
                    outcomeText = "Probably for the best." }
            };

            var merchant = ScriptableObject.CreateInstance<EventData>();
            merchant.eventName   = "Wandering Merchant";
            merchant.eventID     = "wandering_merchant";
            merchant.flavourText = "A hunched figure offers you a strange glowing card.";
            merchant.choices = new[]
            {
                new EventChoice { choiceText = "Buy the card (50g)", goldCost = 50, gainRandomCard = true,
                    outcomeText = "Worth it." },
                new EventChoice { choiceText = "Ignore them",
                    outcomeText = "Their eyes follow you." }
            };

            return new[] { shrine, merchant };
        }

        // ============================================================
        // GOLD BONUS PER ROOM TYPE
        // ============================================================

        int GoldBonusForRoom(RoomType type) => type switch
        {
            RoomType.NormalCombat => 0,
            RoomType.EliteCombat  => 15,
            RoomType.Boss         => 30,
            RoomType.Shop         => 0,
            RoomType.Rest         => Random.Range(0, 2) == 0 ? Random.Range(5, 16) : 0,
            RoomType.Treasure     => Random.Range(20, 41),
            RoomType.Mystery      => Random.Range(5, 16),
            _                     => 0
        };

        // ============================================================
        // INTENT DISPLAY
        // Uses customIntentDescription if set, otherwise infers.
        // ============================================================

        string BuildIntentDisplay(EnemyAction intent)
        {
            if (intent == null) return "  ❓ Unknown";

            if (!string.IsNullOrEmpty(intent.customIntentDescription))
                return $"  {IntentIcon(intent.intentType)} {intent.customIntentDescription}";

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
                _ =>
                    $"  {icon} {name}"
            };
        }

        // ============================================================
        // GUI STYLE HELPERS
        // ============================================================

        GUIStyle Bold(int size = 14)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontStyle = FontStyle.Bold;
            s.fontSize  = size;
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
            RoomType.EliteCombat  => "💀",
            RoomType.Boss         => "👑",
            RoomType.Shop         => "🛒",
            RoomType.Mystery      => "❓",
            RoomType.Rest         => "🔥",
            RoomType.Treasure     => "💎",
            _                     => "•"
        };

        string IntentIcon(EnemyIntent intent) => intent switch
        {
            EnemyIntent.Attack => "⚔",
            EnemyIntent.Defend => "🛡",
            EnemyIntent.Buff   => "✨",
            EnemyIntent.Debuff => "💀",
            _                  => "❓"
        };

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
    }
}
