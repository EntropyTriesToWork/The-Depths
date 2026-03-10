// ============================================================
//  RunState.cs
//  The single data container for everything that belongs to
//  one run. Lives for the duration of a run, then is discarded.
//
//  RunManager owns one of these and mutates it as the run
//  progresses. It's deliberately a plain C# class (not a
//  MonoBehaviour) so it can be serialized to JSON for mid-run
//  saves later without extra work.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    [Serializable]
    public class RunState
    {
        // ----------------------------------------------------------
        // Character
        // ----------------------------------------------------------

        public CharacterClass PlayerClass  { get; private set; }
        public int            MaxHP        { get; private set; }
        public int            CurrentHP    { get; private set; }

        // ----------------------------------------------------------
        // Resources  (Gold resets each run, Essence/Souls persist
        //             between runs — stored in PlayerProfile)
        // ----------------------------------------------------------

        public int Gold    { get; private set; }   // Run-scoped only

        // ----------------------------------------------------------
        // Deck
        // ----------------------------------------------------------

        private List<CardInstance> _deck = new List<CardInstance>();
        public IReadOnlyList<CardInstance> Deck => _deck;

        // ----------------------------------------------------------
        // Relics
        // ----------------------------------------------------------

        private List<RelicInstance> _relics = new List<RelicInstance>();
        public IReadOnlyList<RelicInstance> Relics => _relics;

        // ----------------------------------------------------------
        // Run progression
        // ----------------------------------------------------------

        public int  CurrentFloor    { get; private set; }   // Which floor (set of rooms) we're on
        public int  CurrentRoomIndex { get; private set; }  // How many rooms completed this floor
        public bool IsComplete       { get; private set; }  // True after final boss

        // ----------------------------------------------------------
        // Stats (fed to achievement system at run end)
        // ----------------------------------------------------------

        public int TotalDamageDealt   { get; private set; }
        public int TotalDamageTaken   { get; private set; }
        public int TotalCardsPlayed   { get; private set; }
        public int TotalEnemiesKilled { get; private set; }
        public int TotalGoldEarned    { get; private set; }
        public int RoomsCompleted     { get; private set; }

        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------

        public RunState(CharacterClass playerClass, int maxHP, List<CardInstance> starterDeck)
        {
            PlayerClass = playerClass;
            MaxHP       = maxHP;
            CurrentHP   = maxHP;
            Gold        = 0;

            CurrentFloor     = 1;
            CurrentRoomIndex = 0;
            IsComplete       = false;

            _deck.AddRange(starterDeck);
        }

        // ----------------------------------------------------------
        // HP
        // ----------------------------------------------------------

        public void SetCurrentHP(int hp)
        {
            CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
        }

        public void HealHP(int amount)
        {
            CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
        }

        public void SetMaxHP(int newMax, bool healDifference = false)
        {
            int diff = newMax - MaxHP;
            MaxHP    = newMax;
            if (healDifference && diff > 0)
                CurrentHP = Mathf.Min(CurrentHP + diff, MaxHP);
        }

        // ----------------------------------------------------------
        // Gold
        // ----------------------------------------------------------

        public void AddGold(int amount)
        {
            Gold           = Mathf.Max(0, Gold + amount);
            TotalGoldEarned += Mathf.Max(0, amount);
            GameEvents.OnGoldChanged?.Invoke(Gold);
        }

        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            GameEvents.OnGoldChanged?.Invoke(Gold);
            return true;
        }

        // ----------------------------------------------------------
        // Deck management
        // ----------------------------------------------------------

        public void AddCard(CardInstance card)
        {
            _deck.Add(card);
            GameEvents.OnCardAddedToDeck?.Invoke(card);
        }

        public void RemoveCard(CardInstance card)
        {
            _deck.Remove(card);
            GameEvents.OnCardRemovedFromDeck?.Invoke(card);
        }

        public List<CardInstance> GetDeckCopy() => new List<CardInstance>(_deck);

        // ----------------------------------------------------------
        // Relics
        // ----------------------------------------------------------

        public void AddRelic(RelicInstance relic)
        {
            _relics.Add(relic);
        }

        public bool HasRelic(string relicID)
        {
            foreach (var r in _relics)
                if (r.Data.RelicID == relicID) return true;
            return false;
        }

        // ----------------------------------------------------------
        // Progression
        // ----------------------------------------------------------

        public void AdvanceRoom()
        {
            CurrentRoomIndex++;
            RoomsCompleted++;
        }

        public void AdvanceFloor()
        {
            CurrentFloor++;
            CurrentRoomIndex = 0;
        }

        public void MarkComplete() => IsComplete = true;

        // ----------------------------------------------------------
        // Stat tracking (called by RunManager after each combat)
        // ----------------------------------------------------------

        public void RecordCombatStats(CombatResult result)
        {
            if (result == null) return;
            TotalGoldEarned += result.GoldEarned;
        }

        public void AddDamageDealt(int amount)   => TotalDamageDealt   += amount;
        public void AddDamageTaken(int amount)   => TotalDamageTaken   += amount;
        public void AddCardsPlayed(int amount)   => TotalCardsPlayed   += amount;
        public void AddEnemiesKilled(int amount) => TotalEnemiesKilled += amount;

        /// <summary>
        /// Directly sets the Gold value. Used only by SaveSystem on load.
        /// Do not call from gameplay code — use AddGold/SpendGold instead.
        /// </summary>
        public void RestoreGold(int amount)
        {
            Gold = Mathf.Max(0, amount);
        }

        /// <summary>
        /// Restores floor and room index from a save. Used only by SaveSystem.
        /// </summary>
        public void RestoreProgression(int floor, int roomIndex)
        {
            CurrentFloor = Mathf.Max(1, floor);
            CurrentRoomIndex = Mathf.Max(0, roomIndex);
        }
    }
}
