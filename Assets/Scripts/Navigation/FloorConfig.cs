// ============================================================
//  FloorConfig.cs
//  ScriptableObject defining one floor's layout and content.
//  Create: Right-click → Create → CardGame → Run → Floor Config
// ============================================================

using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Run/Floor Config", fileName = "FloorConfig")]
    public class FloorConfig : ScriptableObject
    {
        // ----------------------------------------------------------
        // Identity
        // ----------------------------------------------------------

        [Header("Identity")]
        public string floorName   = "Floor 1";
        public int    floorNumber = 1;

        // ----------------------------------------------------------
        // Room count
        // ----------------------------------------------------------

        [Header("Room Count")]
        [Tooltip("Minimum rooms before the boss (inclusive of the boss room).")]
        [Range(10, 15)]
        public int minRooms = 10;

        [Tooltip("Maximum rooms before the boss (inclusive of the boss room).")]
        [Range(10, 15)]
        public int maxRooms = 15;

        [Tooltip("Choices offered to the player after clearing each room.")]
        [Range(1, 3)]
        public int minChoices = 2;

        [Range(1, 3)]
        public int maxChoices = 3;

        // ----------------------------------------------------------
        // Room type weights (non-mystery pool)
        // These control what appears as a named room in the choices.
        // ----------------------------------------------------------

        [Header("Room Type Weights")]
        [Tooltip("Probability of a NormalCombat room appearing as a named choice.")]
        public float normalCombatWeight = 40f;

        [Tooltip("Probability of an EliteCombat room appearing.")]
        public float eliteWeight        = 10f;

        [Tooltip("Probability of a Shop appearing.")]
        public float shopWeight         = 15f;

        [Tooltip("Probability of a Mystery room appearing.")]
        public float mysteryWeight      = 25f;

        [Tooltip("Probability of a Rest site appearing.")]
        public float restWeight         = 8f;

        [Tooltip("Probability of a Treasure room appearing.")]
        public float treasureWeight     = 2f;

        // ----------------------------------------------------------
        // Mystery room weights
        // These control what a Mystery room resolves into on entry.
        // ----------------------------------------------------------

        [Header("Mystery Room Resolution Weights")]
        [Tooltip("Chance a Mystery resolves to an Event. Should be highest.")]
        public float mysteryEventWeight  = 60f;

        [Tooltip("Chance a Mystery resolves to a NormalCombat encounter.")]
        public float mysteryCombatWeight = 30f;

        [Tooltip("Chance a Mystery resolves to a Shop. Should be lowest.")]
        public float mysteryShopWeight   = 10f;

        // ----------------------------------------------------------
        // Placement rules
        // ----------------------------------------------------------

        [Header("Placement Rules")]
        [Tooltip("Elites cannot appear before this room index (0-based).")]
        public int eliteUnlockAfterRoom = 2;

        [Tooltip("A guaranteed Shop appears at this room index. -1 = disabled.")]
        public int guaranteedShopAtRoom = 4;

        [Tooltip("Minimum rooms between two Elite encounters.")]
        public int minRoomsBetweenElites = 2;

        // ----------------------------------------------------------
        // Content pools
        // ----------------------------------------------------------

        [Header("Enemy Pools")]
        public EnemyData[] normalEnemyPool;
        public EnemyData[] eliteEnemyPool;
        public EnemyData[] bossEnemies;

        [Header("Event Pool")]
        [Tooltip("Events that can appear inside Mystery rooms on this floor.")]
        public EventData[] eventPool;

        // ----------------------------------------------------------
        // Computed helpers
        // ----------------------------------------------------------

        public float TotalNonMysteryWeight =>
            normalCombatWeight + eliteWeight + shopWeight + mysteryWeight + restWeight + treasureWeight;

        public float TotalMysteryWeight =>
            mysteryEventWeight + mysteryCombatWeight + mysteryShopWeight;

        /// <summary>
        /// Rolls a total room count for this floor (10–15 range).
        /// The boss always occupies the final slot.
        /// </summary>
        public int RollRoomCount() => Random.Range(minRooms, maxRooms + 1);

        /// <summary>
        /// Rolls a weighted RoomType for normal navigation choices.
        /// Elites respect placement rules.
        /// </summary>
        public RoomType RollRoomType(int roomsCompletedThisFloor, int roomsSinceLastElite)
        {
            bool eliteAllowed = roomsCompletedThisFloor >= eliteUnlockAfterRoom
                             && roomsSinceLastElite     >= minRoomsBetweenElites;

            float eliteW = eliteAllowed ? eliteWeight : 0f;
            float total  = TotalNonMysteryWeight - eliteWeight + eliteW;
            float roll   = Random.Range(0f, total);
            float acc    = 0f;

            acc += normalCombatWeight; if (roll < acc) return RoomType.NormalCombat;
            acc += eliteW;             if (roll < acc) return RoomType.EliteCombat;
            acc += shopWeight;         if (roll < acc) return RoomType.Shop;
            acc += mysteryWeight;      if (roll < acc) return RoomType.Mystery;
            acc += restWeight;         if (roll < acc) return RoomType.Rest;
            return RoomType.Treasure;
        }

        /// <summary>
        /// Rolls what a Mystery room actually contains when the player enters.
        /// Event > Combat > Shop (configurable via weights above).
        /// </summary>
        public MysteryOutcome RollMysteryOutcome()
        {
            float roll = Random.Range(0f, TotalMysteryWeight);
            float acc  = 0f;

            acc += mysteryEventWeight;  if (roll < acc) return MysteryOutcome.Event;
            acc += mysteryCombatWeight; if (roll < acc) return MysteryOutcome.NormalCombat;
            return MysteryOutcome.Shop;
        }

        // ----------------------------------------------------------
        // Content rollers
        // ----------------------------------------------------------

        public System.Collections.Generic.List<EnemyData> RollNormalEnemies()
        {
            var result = new System.Collections.Generic.List<EnemyData>();
            if (normalEnemyPool == null || normalEnemyPool.Length == 0) return result;

            int count = Random.Range(1, 3);
            for (int i = 0; i < count; i++)
                result.Add(normalEnemyPool[Random.Range(0, normalEnemyPool.Length)]);

            return result;
        }

        public System.Collections.Generic.List<EnemyData> RollEliteEnemies()
        {
            var result = new System.Collections.Generic.List<EnemyData>();
            if (eliteEnemyPool == null || eliteEnemyPool.Length == 0) return result;
            result.Add(eliteEnemyPool[Random.Range(0, eliteEnemyPool.Length)]);
            return result;
        }

        public System.Collections.Generic.List<EnemyData> GetBossEnemies()
        {
            return bossEnemies != null
                ? new System.Collections.Generic.List<EnemyData>(bossEnemies)
                : new System.Collections.Generic.List<EnemyData>();
        }

        public EventData RollEvent()
        {
            if (eventPool == null || eventPool.Length == 0) return null;
            return eventPool[Random.Range(0, eventPool.Length)];
        }
    }
}
