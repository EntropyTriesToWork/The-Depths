// ============================================================
//  CombatContext.cs
//  The "world state" snapshot passed into every CardEffect.Execute().
//  Think of this as the read/write interface between a card and
//  the combat simulation. Effects should ONLY read/mutate game
//  state through this object — never by direct reference.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    /// <summary>
    /// Passed to every CardEffect.Execute() call.
    /// Gives effects clean access to all mutable combat state.
    /// </summary>
    public class CombatContext
    {
        // ----------------------------------------------------------
        // Participants
        // ----------------------------------------------------------

        public CombatEntity Player { get; private set; }
        public List<CombatEntity> Enemies { get; private set; }

        // ----------------------------------------------------------
        // Deck / Hand state
        // ----------------------------------------------------------

        public DeckManager DeckManager { get; private set; }

        // ----------------------------------------------------------
        // Energy
        // ----------------------------------------------------------

        public int CurrentEnergy { get; private set; }
        public int MaxEnergy { get; private set; }

        // ----------------------------------------------------------
        // The card being played right now (for self-referential effects)
        // ----------------------------------------------------------

        public CardInstance ActiveCard { get; set; }

        // ----------------------------------------------------------
        // The player-chosen or system-chosen primary target
        // ----------------------------------------------------------

        public CombatEntity PrimaryTarget { get; set; }

        // ----------------------------------------------------------
        // Run-level resources (readable here, mutations go via RunManager)
        // ----------------------------------------------------------

        public int CurrentGold { get; private set; }

        // ----------------------------------------------------------
        // Stat tracking (fed into achievement system)
        // ----------------------------------------------------------

        public int DamageDealtThisTurn { get; private set; }
        public int BlockGainedThisTurn { get; private set; }
        public int CardsPlayedThisTurn { get; private set; }

        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------

        public CombatContext(
            CombatEntity player,
            List<CombatEntity> enemies,
            DeckManager deckManager,
            int currentEnergy,
            int maxEnergy,
            int currentGold)
        {
            Player         = player;
            Enemies        = enemies;
            DeckManager    = deckManager;
            CurrentEnergy  = currentEnergy;
            MaxEnergy      = maxEnergy;
            CurrentGold    = currentGold;
        }

        // ----------------------------------------------------------
        // Energy mutations
        // ----------------------------------------------------------

        public bool SpendEnergy(int amount)
        {
            if (CurrentEnergy < amount) return false;
            CurrentEnergy -= amount;
            return true;
        }

        public void GainEnergy(int amount)
        {
            CurrentEnergy = Mathf.Min(CurrentEnergy + amount, MaxEnergy);
        }

        // ----------------------------------------------------------
        // Convenience: resolve targets from an EffectTarget enum
        // ----------------------------------------------------------

        public List<CombatEntity> ResolveTargets(EffectTarget targetType)
        {
            var targets = new List<CombatEntity>();

            switch (targetType)
            {
                case EffectTarget.Self:
                    targets.Add(Player);
                    break;

                case EffectTarget.SingleEnemy:
                    if (PrimaryTarget != null)
                        targets.Add(PrimaryTarget);
                    else if (Enemies.Count > 0)
                        targets.Add(Enemies[0]);           // Fallback: first enemy
                    break;

                case EffectTarget.AllEnemies:
                    targets.AddRange(Enemies);
                    break;

                case EffectTarget.RandomEnemy:
                    if (Enemies.Count > 0)
                        targets.Add(Enemies[Random.Range(0, Enemies.Count)]);
                    break;

                case EffectTarget.AllCharacters:
                    targets.Add(Player);
                    targets.AddRange(Enemies);
                    break;
            }

            return targets;
        }

        // ----------------------------------------------------------
        // Stat tracking helpers (called internally by effect helpers)
        // ----------------------------------------------------------

        public void TrackDamageDealt(int amount)
        {
            DamageDealtThisTurn += amount;
        }

        public void TrackBlockGained(int amount)
        {
            BlockGainedThisTurn += amount;
        }

        public void TrackCardPlayed()
        {
            CardsPlayedThisTurn++;
        }

        public void ResetTurnTracking()
        {
            DamageDealtThisTurn = 0;
            BlockGainedThisTurn = 0;
            CardsPlayedThisTurn = 0;
        }
    }
}
