// ============================================================
//  NavigationSystem.cs
//  Generates the 1–3 room choices shown after clearing a room.
//
//  Rules enforced:
//    • Total rooms per floor: 10–15 (rolled once at floor start)
//    • Boss always occupies the final room slot (no choice offered)
//    • Mystery rooms hide their content until entered
//    • No duplicate room types in the same choice set
//    • Elites respect min spacing and unlock floor
//    • Guaranteed shop at configured index
//    • Low HP nudges choices toward Rest sites
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class NavigationSystem
    {
        // ----------------------------------------------------------
        // State
        // ----------------------------------------------------------

        private FloorConfig _config;

        /// <summary>Total rooms this floor including the boss. Rolled once per floor.</summary>
        public int TotalRoomsThisFloor  { get; set; }

        /// <summary>Index of the boss room (always the last room, 0-based).</summary>
        public int BossRoomIndex        => TotalRoomsThisFloor - 1;

        /// <summary>How many rooms since the last Elite was generated.</summary>
        private int _roomsSinceLastElite = 99;

        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------

        public NavigationSystem(FloorConfig config)
        {
            SetConfig(config);
        }

        // ----------------------------------------------------------
        // Floor setup
        // ----------------------------------------------------------

        /// <summary>
        /// Call at the start of each floor to roll the room count
        /// and reset elite spacing tracking.
        /// </summary>
        public void InitializeFloor(FloorConfig config)
        {
            SetConfig(config);
            TotalRoomsThisFloor  = config.RollRoomCount();
            _roomsSinceLastElite = 99;

            Debug.Log($"[Navigation] Floor {config.floorNumber}: {TotalRoomsThisFloor} rooms " +
                      $"(boss at room {BossRoomIndex + 1})");
        }

        public void SetConfig(FloorConfig config)
        {
            _config = config;
            if (TotalRoomsThisFloor == 0)
                TotalRoomsThisFloor = config.RollRoomCount();
        }

        // ----------------------------------------------------------
        // Core: generate choices after a room is cleared
        // ----------------------------------------------------------

        /// <summary>
        /// Call after the player clears any room.
        /// Returns a list of RoomChoice objects for the player to pick from.
        /// Returns a single mandatory Boss choice when the player is on
        /// the second-to-last room.
        /// </summary>
        public List<RoomChoice> GenerateChoices(RunState runState)
        {
            int roomsCompleted = runState.CurrentRoomIndex;  // 0-based after clear

            // ── Next room is the boss ──────────────────────────────
            if (roomsCompleted >= BossRoomIndex)
                return new List<RoomChoice> { BuildBossChoice() };

            // ── Roll choice count ──────────────────────────────────
            int count = Random.Range(_config.minChoices, _config.maxChoices + 1);

            // ── Forced room override ───────────────────────────────
            RoomType? forced = GetForcedRoomType(roomsCompleted);

            var choices   = new List<RoomChoice>();
            var usedTypes = new HashSet<RoomType>();

            if (forced.HasValue)
            {
                choices.Add(BuildChoice(forced.Value, runState));
                usedTypes.Add(forced.Value);
            }

            // ── Fill remaining slots ───────────────────────────────
            int attempts = 0;
            int target   = forced.HasValue ? count + 1 : count;

            while (choices.Count < target && attempts < 30)
            {
                attempts++;

                RoomType rolled = _config.RollRoomType(roomsCompleted, _roomsSinceLastElite);

                // No duplicates in the same choice set
                if (usedTypes.Contains(rolled)) continue;

                // Low HP nudge: swap combat for rest below 25% HP
                float hpPct = (float)runState.CurrentHP / runState.MaxHP;
                if (rolled == RoomType.NormalCombat && hpPct < 0.25f)
                    rolled = RoomType.Rest;

                if (usedTypes.Contains(rolled)) continue;

                choices.Add(BuildChoice(rolled, runState));
                usedTypes.Add(rolled);

                if (rolled == RoomType.EliteCombat)
                    _roomsSinceLastElite = 0;
                else
                    _roomsSinceLastElite++;
            }

            return choices;
        }

        // ----------------------------------------------------------
        // Forced room logic
        // ----------------------------------------------------------

        private RoomType? GetForcedRoomType(int roomsCompleted)
        {
            if (_config.guaranteedShopAtRoom >= 0
                && roomsCompleted == _config.guaranteedShopAtRoom)
                return RoomType.Shop;

            return null;
        }

        // ----------------------------------------------------------
        // Choice builders
        // ----------------------------------------------------------

        private RoomChoice BuildChoice(RoomType type, RunState runState)
        {
            switch (type)
            {
                case RoomType.NormalCombat:
                {
                    var enemies = _config.RollNormalEnemies();
                    return new RoomChoice(
                        type,
                        RoomChoice.LabelFor(type),
                        BuildCombatHint(enemies),
                        enemies);
                }

                case RoomType.EliteCombat:
                {
                    var enemies = _config.RollEliteEnemies();
                    return new RoomChoice(
                        type,
                        RoomChoice.LabelFor(type),
                        "Dangerous — better rewards",
                        enemies);
                }

                case RoomType.Mystery:
                    // Content is intentionally hidden — no hint, no pre-rolled content.
                    // MysteryRoomResolver.Resolve() is called by RunManager on entry.
                    return new RoomChoice(
                        type,
                        RoomChoice.LabelFor(type),
                        RoomChoice.HintFor(type));

                case RoomType.Shop:
                    return new RoomChoice(type,
                        RoomChoice.LabelFor(type),
                        "Buy cards, relics, and remove cards");

                case RoomType.Rest:
                    return new RoomChoice(type,
                        RoomChoice.LabelFor(type),
                        "Heal 30% HP or upgrade a card");

                case RoomType.Treasure:
                    return new RoomChoice(type,
                        RoomChoice.LabelFor(type),
                        "A free reward awaits");

                default:
                    return new RoomChoice(type, RoomChoice.LabelFor(type), "");
            }
        }

        private RoomChoice BuildBossChoice()
        {
            var enemies = _config.GetBossEnemies();
            return new RoomChoice(
                RoomType.Boss,
                RoomChoice.LabelFor(RoomType.Boss),
                "Defeat the floor boss to advance",
                enemies);
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------

        private string BuildCombatHint(List<EnemyData> enemies)
        {
            if (enemies == null || enemies.Count == 0) return "Enemies ahead";
            if (enemies.Count == 1) return enemies[0].enemyName;
            return $"{enemies.Count} enemies";
        }

        /// <summary>
        /// Returns a display string like "Room 3 / 12" for UI.
        /// </summary>
        public string GetProgressLabel(int roomsCompleted)
        {
            return $"Room {roomsCompleted + 1} / {TotalRoomsThisFloor}";
        }

        /// <summary>
        /// Returns a 0–1 float representing floor completion for a progress bar.
        /// </summary>
        public float GetFloorProgress(int roomsCompleted)
        {
            return TotalRoomsThisFloor > 0
                ? (float)roomsCompleted / TotalRoomsThisFloor
                : 0f;
        }
    }
}
