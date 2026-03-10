// ============================================================
//  CombatEvents.cs
//  A lightweight static event bus for combat-related signals.
//
//  WHY this exists:
//    Card effects need to trigger UI behaviours (e.g. "ask the
//    player to choose a card to discard") without directly
//    referencing UI code. This bus decouples them completely.
//
//  How to use:
//    Subscribe:   CombatEvents.OnEnemyKilled += HandleEnemyKilled;
//    Unsubscribe: CombatEvents.OnEnemyKilled -= HandleEnemyKilled;
//    Invoke:      CombatEvents.OnEnemyKilled?.Invoke(entity);
//
//  Unsubscribe in OnDisable/OnDestroy to avoid memory leaks.
// ============================================================

using System;

namespace CardGame
{
    public static class CombatEvents
    {
        public static Action OnPlayerTurnStart;
        public static Action OnPlayerTurnEnd;
        public static Action OnEnemyTurnStart;
        public static Action OnEnemyTurnEnd;

        public static Action<CardInstance>           OnCardPlayedGlobal;   // Any card played
        public static Action<CardInstance>           OnCardDrawnGlobal;
        public static Action<CardInstance>           OnCardDiscardedGlobal;
        public static Action<CardInstance>           OnCardExhaustedGlobal;

        public static Action<CombatEntity>           OnEnemyKilled;
        public static Action                         OnPlayerDeath;
        public static Action                         OnCombatVictory;

        public static Action<CombatEntity, int>      OnDamageDealt;        // (target, amount)
        public static Action<CombatEntity, int>      OnBlockGained;        // (entity, amount)
        public static Action<CombatEntity, int>      OnEntityHealed;

        public static Action<CombatEntity, StatusType, int> OnStatusApplied; // (entity, type, stacks)

        public static Action<int>   RequestDiscardSelection;     // Param: number to discard
        public static Action<int>   RequestExhaustSelection;     // Param: number to exhaust
        public static Action        RequestAddRandomCardToHand;
        public static Action<int>   RequestCardUpgradeSelection; // e.g. after a boss reward

        // Damage breakdowns (replaces the old OnDamageDealt(entity, int))
        public static Action<CombatEntity, DamageReport> OnDamageDealtDetailed;

        public static Action<CombatEntity, int, DefenseLayer> OnDefenseGained; //Whenever player gains any defense stats.

        public static Action<RelicInstance> OnRelicTriggered;   // Fired by relics when they activate

        public static void ResetAllCombatEvents()
        {
            OnPlayerTurnStart         = null;
            OnPlayerTurnEnd           = null;
            OnEnemyTurnStart          = null;
            OnEnemyTurnEnd            = null;
            OnCardPlayedGlobal        = null;
            OnCardDrawnGlobal         = null;
            OnCardDiscardedGlobal     = null;
            OnCardExhaustedGlobal     = null;
            OnEnemyKilled             = null;
            OnPlayerDeath             = null;
            OnCombatVictory           = null;
            OnDamageDealt             = null;
            OnBlockGained             = null;
            OnEntityHealed            = null;
            OnStatusApplied           = null;
            RequestDiscardSelection   = null;
            RequestExhaustSelection   = null;
            RequestAddRandomCardToHand = null;
            RequestCardUpgradeSelection = null;
        }
    }

    public static class GameEvents
    {
        public static Action<int>           OnGoldChanged;          // (newTotal)
        public static Action<int>           OnEssenceChanged;
        public static Action<int>           OnSoulsChanged;
        public static Action<string>        OnAchievementUnlocked;  // (achievementID)
        public static Action<string>        OnCardUnlocked;         // (cardID)
        public static Action<CardInstance>  OnCardAddedToDeck;
        public static Action<CardInstance>  OnCardRemovedFromDeck;
        public static Action<CardInstance>  OnCardUpgraded;
        public static Action                OnRunStarted;
        public static Action<bool>          OnRunEnded;             // (wasVictory)
        public static Action                OnCharacterSelected;
        public static Action<int>           OnRunGoldRewardGenerated; //How much gold player is rewarded with upon beating a room.
        // Relic events
        public static Action<RelicInstance> OnRelicEquipped;
        public static Action<RelicInstance> OnRelicRemoved;
    }

    public static class RewardHooks
    {
        private static System.Collections.Generic.List<System.Func<int, int>> _goldModifiers
            = new System.Collections.Generic.List<System.Func<int, int>>();

        public static void AddGoldModifier(System.Func<int, int> modifier)
            => _goldModifiers.Add(modifier);

        public static void RemoveGoldModifier(System.Func<int, int> modifier)
            => _goldModifiers.Remove(modifier);

        public static int ApplyGoldModifiers(int baseGold)
        {
            int result = baseGold;
            foreach (var mod in _goldModifiers)
                result = mod(result);
            return result;
        }

        public static void ClearAllModifiers() => _goldModifiers.Clear();
    }
}
