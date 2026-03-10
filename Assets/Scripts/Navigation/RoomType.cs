// ============================================================
//  RoomType.cs
//  All room-related enums. Standalone file.
// ============================================================

namespace CardGame
{
    public enum RoomType
    {
        NormalCombat,   // Standard enemy encounter
        EliteCombat,    // Harder enemy, better rewards + Essence chance
        Boss,           // Floor boss — always the final room
        Shop,           // Spend Gold / Essence
        Mystery,        // Hidden — resolves into Event, NormalCombat, or Shop on entry
        Rest,           // Heal HP or upgrade a card
        Treasure        // Free card or relic, no combat
    }

    /// <summary>
    /// What a Mystery room resolves into when the player enters.
    /// Determined at entry, not at generation.
    /// </summary>
    public enum MysteryOutcome
    {
        Event,
        NormalCombat,
        Shop
    }
}
