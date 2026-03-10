// ============================================================
//  RoomChoice.cs
//  A single navigation option shown to the player after
//  clearing a room. 1–3 of these are generated per clear.
//
//  Mystery rooms intentionally hide their content — the
//  MysteryOutcome is not set until the player enters.
// ============================================================

using System.Collections.Generic;

namespace CardGame
{
    public class RoomChoice
    {
        // ----------------------------------------------------------
        // Visible to player before entering
        // ----------------------------------------------------------

        public RoomType        RoomType    { get; private set; }

        /// <summary>Display label, e.g. "Monster Room" or "?"</summary>
        public string          Label       { get; private set; }

        /// <summary>
        /// Hint shown under the label.
        /// Empty for Mystery rooms — revealing the hint would defeat the purpose.
        /// </summary>
        public string          Hint        { get; private set; }

        // ----------------------------------------------------------
        // Pre-rolled content (determined at generation, not at entry)
        // ----------------------------------------------------------

        /// <summary>Enemies for combat rooms. Empty for non-combat rooms.</summary>
        public List<EnemyData> Enemies     { get; private set; }

        /// <summary>
        /// For Mystery rooms: the actual outcome, resolved when player enters.
        /// Null until MysteryRoomResolver.Resolve() is called.
        /// </summary>
        public MysteryOutcome? MysteryOutcome { get; set; }

        /// <summary>
        /// The event to run if this is an Event room or a Mystery that resolved to Event.
        /// </summary>
        public EventData       Event       { get; set; }

        // ----------------------------------------------------------
        // Constructor
        // ----------------------------------------------------------

        public RoomChoice(RoomType type, string label, string hint,
                          List<EnemyData> enemies = null,
                          EventData eventData     = null)
        {
            RoomType = type;
            Label    = label;
            Hint     = hint;
            Enemies  = enemies    ?? new List<EnemyData>();
            Event    = eventData;
        }

        // ----------------------------------------------------------
        // Static label helpers
        // ----------------------------------------------------------

        public static string LabelFor(RoomType type) => type switch
        {
            RoomType.NormalCombat => "Monster Room",
            RoomType.EliteCombat  => "Elite",
            RoomType.Boss         => "BOSS",
            RoomType.Shop         => "Shop",
            RoomType.Mystery      => "?",
            RoomType.Rest         => "Rest Site",
            RoomType.Treasure     => "Treasure",
            _                     => "???"
        };

        public static string HintFor(RoomType type) => type switch
        {
            RoomType.NormalCombat => "Monster encounter",
            RoomType.EliteCombat  => "Dangerous — better rewards",
            RoomType.Boss         => "Floor boss — defeat to advance",
            RoomType.Shop         => "Buy cards, relics, and more",
            RoomType.Mystery      => "Unknown — anything could be inside",
            RoomType.Rest         => "Heal or upgrade a card",
            RoomType.Treasure     => "A free reward awaits",
            _                     => ""
        };
    }
}
