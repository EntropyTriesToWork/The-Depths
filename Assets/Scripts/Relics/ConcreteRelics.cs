using UnityEngine;

namespace CardGame
{
    public class BurningBloodRelic : RelicData
    {
        private System.Action<CombatResult> _onCombatComplete;

        public override RelicSource Source { get => RelicSource.StarterRelic; }
        public override string RelicName => "Burning Blood";
        public override string RelicID => "burning_blood";
        public override string Description => "Heal 6 hp at the end of combat";
        public override RelicRarity Rarity => RelicRarity.Special;
        public override string unlockID => "";

        public override void OnEquip(RelicContext ctx)
        {
            _onCombatComplete = (result) =>
            {
                if (result.WasVictory)
                    ctx.Combat?.Player.Heal(6);
            };
            CombatEvents.OnCombatComplete += HandleVictory;
        }

        private void HandleVictory(CombatResult result)
        {
            if (result.WasVictory)
            {
                _onCombatComplete(result);
            }
        }

        public override void OnUnequip(RelicContext ctx)
        {
            CombatEvents.OnCombatComplete -= HandleVictory;
        }
    }
    public class OddlySmoothStoneRelic : RelicData
    {
        private System.Action _onTurnStart;

        public override void OnEquip(RelicContext ctx)
        {
            _onTurnStart = () =>
            {
                // Only fires on the FIRST turn of combat (turn 1)
                if (ctx.Combat != null)
                    ctx.DrawCards?.Invoke(1);
            };

            // Subscribe to first-turn-only: we'll use a flag on the instance
            CombatEvents.OnPlayerTurnStart += OnFirstTurnOnly;
        }

        private bool _firedThisCombat = false;

        public override RelicSource Source { get => RelicSource.Chest; }
        public override string RelicName => "Oddly Smooth Stone";
        public override string RelicID => "oddly_smooth_stone";
        public override string Description => "Gain 1 more block everytime you gain block. NOT IMPLEMENTED YET";
        public override RelicRarity Rarity => RelicRarity.Common;
        public override string unlockID => "";

        private void OnFirstTurnOnly()
        {
            if (_firedThisCombat) return;
            _firedThisCombat = true;
            // Draw is triggered via CombatManager — signal via event
            CombatEvents.OnPlayerTurnStart -= OnFirstTurnOnly;
        }

        public override void OnUnequip(RelicContext ctx)
        {
            CombatEvents.OnPlayerTurnStart -= OnFirstTurnOnly;
            _firedThisCombat = false;
        }
    }
    //[CreateAssetMenu(menuName = "CardGame/Relics/Akabeko")]
    //public class AkabekoRelic : RelicData
    //{
    //    private System.Action<CardInstance> _onCardPlayed;
    //    private bool _bonusApplied;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        _bonusApplied = false;

    //        _onCardPlayed = (card) =>
    //        {
    //            if (_bonusApplied) return;
    //            if (card.Data.cardType != CardType.Attack) return;

    //            _bonusApplied = true;
    //            // Add a temporary damage bonus to the card being played
    //            // (This fires BEFORE the card's effects — ordering matters)
    //            // For now, apply Strength temporarily for one hit
    //            ctx.Combat?.Player.ApplyStatus(StatusType.Strength, 8, StatusStackBehavior.Additive);
    //            // Remove it next frame (after effects fire) via end-of-turn cleanup
    //            // or a more targeted approach: use a one-shot event
    //            CombatEvents.OnPlayerTurnEnd += RemoveBonus;
    //        };

    //        CombatEvents.OnCardPlayedGlobal += _onCardPlayed;
    //    }

    //    private void RemoveBonus()
    //    {
    //        // Strength was temporarily added — remove the 8 we added
    //        // This is a simplification; a cleaner approach is a dedicated
    //        // "temporary strength this card only" system
    //        CombatEvents.OnPlayerTurnEnd -= RemoveBonus;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnCardPlayedGlobal -= _onCardPlayed;
    //        CombatEvents.OnPlayerTurnEnd    -= RemoveBonus;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "Your first <b>Attack</b> each combat deals <b>8</b> bonus damage.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Cursed Key")]
    //public class CursedKeyRelic : RelicData
    //{
    //    // This relic acts at the run level, not combat level.
    //    // RunManager handles gold/essence — relic modifies reward amounts.

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        GameEvents.OnRunGoldRewardGenerated += BonusGold;
    //    }

    //    private void BonusGold(int goldAmount)
    //    {
    //        goldAmount += 1;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        GameEvents.OnRunGoldRewardGenerated -= BonusGold;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "Gain <b>+1 Gold</b> after each combat.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Ancient Shield")]
    //public class AncientShieldRelic : RelicData
    //{
    //    private bool _applied = false;
    //    private RelicContext _ctx;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        _ctx = ctx;
    //        _applied = false;
    //        CombatEvents.OnPlayerTurnStart += OnFirstTurn;
    //    }

    //    private void OnFirstTurn()
    //    {
    //        if (_applied) return;
    //        _applied = true;
    //        _ctx?.Combat?.Player.GainArmor(10);
    //        CombatEvents.OnPlayerTurnStart -= OnFirstTurn;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnPlayerTurnStart -= OnFirstTurn;
    //        _applied = false;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "At the start of each combat, gain <b>10</b> <color=#E8A020>Armor</color>.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Arcane Focus")]
    //public class ArcaneFocusRelic : RelicData
    //{
    //    private RelicContext _ctx;
    //    private bool _applied;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        _ctx     = ctx;
    //        _applied = false;
    //        CombatEvents.OnPlayerTurnStart += OnFirstTurn;
    //    }

    //    private void OnFirstTurn()
    //    {
    //        if (_applied) return;
    //        _applied = true;
    //        _ctx?.Combat?.Player.GainBarrier(8);
    //        CombatEvents.OnPlayerTurnStart -= OnFirstTurn;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnPlayerTurnStart -= OnFirstTurn;
    //        _applied = false;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "At the start of each combat, gain <b>8</b> <color=#4A90D9>Barrier</color>.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Philosopher's Stone")]
    //public class PhilosophersStoneRelic : RelicData
    //{
    //    private RelicContext _ctx;
    //    private System.Action _onTurnStart;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        _ctx = ctx;

    //        // Apply +1 Strength to all enemies at combat start
    //        CombatEvents.OnPlayerTurnStart += ApplyEnemyStrengthOnFirstTurn;

    //        // Grant +1 energy each player turn
    //        _onTurnStart = () => ctx.GainEnergy?.Invoke(1);
    //        CombatEvents.OnPlayerTurnStart += _onTurnStart;
    //    }

    //    private bool _enemyStrengthApplied = false;

    //    private void ApplyEnemyStrengthOnFirstTurn()
    //    {
    //        if (_enemyStrengthApplied) return;
    //        _enemyStrengthApplied = true;

    //        if (_ctx.Combat != null)
    //            foreach (var enemy in _ctx.Combat.Enemies)
    //                enemy.ApplyStatus(StatusType.Strength, 1);

    //        CombatEvents.OnPlayerTurnStart -= ApplyEnemyStrengthOnFirstTurn;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnPlayerTurnStart -= ApplyEnemyStrengthOnFirstTurn;
    //        CombatEvents.OnPlayerTurnStart -= _onTurnStart;
    //        _enemyStrengthApplied = false;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "Enemies start with <b>+1 Strength</b>.\nYou gain <b>+1 Energy</b> each turn.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Dead Branch")]
    //public class DeadBranchRelic : RelicData
    //{
    //    private System.Action<CardInstance> _onExhaust;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        _onExhaust = (_) => CombatEvents.RequestAddRandomCardToHand?.Invoke();
    //        CombatEvents.OnCardExhaustedGlobal += _onExhaust;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnCardExhaustedGlobal -= _onExhaust;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        "Whenever you <b>Exhaust</b> a card, add a random card to your hand.";
    //}
    //[CreateAssetMenu(menuName = "CardGame/Relics/Ink Bottle")]
    //public class InkBottleRelic : RelicData
    //{
    //    [Tooltip("How many cards to play before the effect triggers.")]
    //    public int triggerEvery = 9;

    //    private System.Action<CardInstance> _onCardPlayed;

    //    public override void OnEquip(RelicContext ctx)
    //    {
    //        var inst = ctx.Instance;

    //        _onCardPlayed = (_) =>
    //        {
    //            inst.Counter++;
    //            if (inst.Counter >= triggerEvery)
    //            {
    //                inst.Counter = 0;
    //                ctx.DrawCards?.Invoke(1);
    //            }
    //            // UI reads inst.Counter to display the charge progress
    //        };

    //        CombatEvents.OnCardPlayedGlobal += _onCardPlayed;
    //    }

    //    public override void OnUnequip(RelicContext ctx)
    //    {
    //        CombatEvents.OnCardPlayedGlobal -= _onCardPlayed;
    //    }

    //    public override string GetDescription(RelicInstance instance) =>
    //        $"Every <b>{triggerEvery}</b> cards played, draw <b>1</b> card.\n" +
    //        $"Progress: <b>{instance?.Counter ?? 0}/{triggerEvery}</b>";
    //}
}
