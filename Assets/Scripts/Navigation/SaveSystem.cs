// ============================================================
//  SaveSystem.cs
//  Reads and writes RunSaveData to disk as JSON.
//
//  Location: Application.persistentDataPath/save_run.json
//  This path is platform-safe on PC, Mac, mobile, and consoles.
//
//  Usage:
//    SaveSystem.SaveRun(saveData);
//    RunSaveData data = SaveSystem.LoadRun();
//    bool exists = SaveSystem.HasSave();
//    SaveSystem.DeleteSave();
// ============================================================

using System;
using System.IO;
using UnityEngine;

namespace CardGame
{
    public static class SaveSystem
    {
        private const string SAVE_FILE_NAME = "save_run.json";

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        // ----------------------------------------------------------
        // Write
        // ----------------------------------------------------------

        /// <summary>
        /// Serializes RunSaveData to JSON and writes it to disk.
        /// Call ONLY after a room is fully cleared.
        /// </summary>
        public static void SaveRun(RunSaveData data)
        {
            try
            {
                data.savedAtUTC = DateTime.UtcNow.ToString("o");
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Run saved → {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        // ----------------------------------------------------------
        // Read
        // ----------------------------------------------------------

        /// <summary>
        /// Loads and deserializes the run save. Returns null if no save exists.
        /// </summary>
        public static RunSaveData LoadRun()
        {
            if (!HasSave()) return null;

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<RunSaveData>(json);
                Debug.Log($"[SaveSystem] Run loaded (saved {data.savedAtUTC})");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load: {e.Message}");
                return null;
            }
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------

        public static bool HasSave() => File.Exists(SavePath);

        /// <summary>
        /// Deletes the save. Call on run end (victory or death).
        /// </summary>
        public static void DeleteSave()
        {
            if (!HasSave()) return;

            try
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save: {e.Message}");
            }
        }

        // ----------------------------------------------------------
        // Conversion helpers (RunState ↔ RunSaveData)
        // ----------------------------------------------------------

        /// <summary>
        /// Builds a RunSaveData snapshot from the current RunState.
        /// Call this in RunManager right before writing to disk.
        /// </summary>
        public static RunSaveData BuildSaveData(
            RunState      runState,
            int           characterIndex,
            int           totalRoomsThisFloor)
        {
            var save = new RunSaveData
            {
                characterIndex      = characterIndex,
                playerClass         = runState.PlayerClass.ToString(),
                currentHP           = runState.CurrentHP,
                maxHP               = runState.MaxHP,
                gold                = runState.Gold,
                currentFloor        = runState.CurrentFloor,
                currentRoomIndex    = runState.CurrentRoomIndex,
                totalRoomsThisFloor = totalRoomsThisFloor,

                totalDamageDealt    = runState.TotalDamageDealt,
                totalDamageTaken    = runState.TotalDamageTaken,
                totalCardsPlayed    = runState.TotalCardsPlayed,
                totalEnemiesKilled  = runState.TotalEnemiesKilled,
                totalGoldEarned     = runState.TotalGoldEarned,
                roomsCompleted      = runState.RoomsCompleted
            };

            // Deck
            foreach (var card in runState.Deck)
                save.deck.Add(new CardSaveEntry
                {
                    cardID         = card.Data.cardID,
                    tier           = (int)card.CurrentTier,
                    timesPlayedRun = card.TimesPlayedThisRun
                });

            // Relics
            foreach (var relic in runState.Relics)
                save.relicIDs.Add(relic.Data.relicID);

            return save;
        }

        /// <summary>
        /// Reconstructs a RunState from save data.
        /// Requires the card and relic lookup tables to resolve IDs back to assets.
        /// </summary>
        public static RunState RestoreRunState(
            RunSaveData                               save,
            System.Collections.Generic.Dictionary<string, CardData>  cardLookup,
            System.Collections.Generic.Dictionary<string, RelicData> relicLookup,
            System.Collections.Generic.Dictionary<string, CharacterData> characterLookup)
        {
            // Resolve character
            var playerClass = (CharacterClass)Enum.Parse(typeof(CharacterClass), save.playerClass);

            // Reconstruct deck
            var deck = new System.Collections.Generic.List<CardInstance>();
            foreach (var entry in save.deck)
            {
                if (!cardLookup.TryGetValue(entry.cardID, out CardData baseCard))
                {
                    Debug.LogWarning($"[SaveSystem] Card not found: {entry.cardID} — skipping.");
                    continue;
                }

                // Walk the upgrade chain to reach the saved tier
                CardData cardAtTier = baseCard;
                for (int i = 0; i < entry.tier && cardAtTier != null; i++)
                    cardAtTier = cardAtTier.GetNextTier();

                if (cardAtTier == null)
                {
                    Debug.LogWarning($"[SaveSystem] Could not resolve tier {entry.tier} for {entry.cardID}.");
                    cardAtTier = baseCard;
                }

                deck.Add(new CardInstance(cardAtTier));
            }

            // Get character max HP from data
            int maxHP = save.maxHP;
            if (characterLookup.TryGetValue(save.playerClass, out CharacterData charData))
                maxHP = save.maxHP; // Use saved value (may have been modified by relics/events)

            // Build RunState
            var runState = new RunState(playerClass, maxHP, deck);

            // Restore HP and gold
            runState.SetCurrentHP(save.currentHP);
            for (int i = 0; i < save.gold; i++) { /* RunState needs a direct gold setter */ }
            runState.RestoreGold(save.gold);    // See note below
            runState.RestoreProgression(save.currentFloor, save.currentRoomIndex);

            // Restore relics
            foreach (var relicID in save.relicIDs)
            {
                if (relicLookup.TryGetValue(relicID, out RelicData relicData))
                    runState.AddRelic(new RelicInstance(relicData));
                else
                    Debug.LogWarning($"[SaveSystem] Relic not found: {relicID}");
            }

            return runState;
        }
    }
}
