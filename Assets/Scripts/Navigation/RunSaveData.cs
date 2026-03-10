// ============================================================
//  RunSaveData.cs
//  Serializable snapshot of a run at the navigation screen.
//
//  SAVE POLICY (enforced by RunManager):
//    ✓ Save AFTER a room is cleared (navigation screen is shown)
//    ✗ Do NOT save when a choice is selected (entering a room)
//
//  This means on reload the player always lands on the navigation
//  screen, never mid-combat or mid-event. A player can reload to
//  re-roll their choices — this is intentional per design.
//
//  Storage: JSON written to Application.persistentDataPath.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    [Serializable]
    public class RunSaveData
    {
        // ----------------------------------------------------------
        // Meta
        // ----------------------------------------------------------

        public string saveVersion   = "1.0";
        public string savedAtUTC    = "";

        // ----------------------------------------------------------
        // Character
        // ----------------------------------------------------------

        public int    characterIndex;
        public string playerClass;

        // ----------------------------------------------------------
        // HP
        // ----------------------------------------------------------

        public int currentHP;
        public int maxHP;

        // ----------------------------------------------------------
        // Resources
        // ----------------------------------------------------------

        public int gold;

        // ----------------------------------------------------------
        // Progression
        // ----------------------------------------------------------

        public int currentFloor;
        public int currentRoomIndex;    // Rooms cleared this floor
        public int totalRoomsThisFloor; // Rolled at floor start — must persist

        // ----------------------------------------------------------
        // Deck
        // ----------------------------------------------------------

        public List<CardSaveEntry> deck = new List<CardSaveEntry>();

        // ----------------------------------------------------------
        // Relics
        // ----------------------------------------------------------

        public List<string> relicIDs = new List<string>();

        // ----------------------------------------------------------
        // Run stats (for end-of-run summary)
        // ----------------------------------------------------------

        public int totalDamageDealt;
        public int totalDamageTaken;
        public int totalCardsPlayed;
        public int totalEnemiesKilled;
        public int totalGoldEarned;
        public int roomsCompleted;
    }

    // ----------------------------------------------------------
    //  Card save entry — enough to reconstruct a CardInstance
    // ----------------------------------------------------------

    [Serializable]
    public class CardSaveEntry
    {
        public string cardID;
        public int    tier;             // UpgradeTier as int (0, 1, 2)
        public int    timesPlayedRun;
    }
}
