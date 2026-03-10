// ============================================================
//  Enums.cs
//  All shared enumerations for the Card System.
//  Keep this file as the single source of truth for enum types.
// ============================================================

namespace CardGame
{
    // ----------------------------------------------------------
    // Card fundamentals
    // ----------------------------------------------------------

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Special       // Starter cards, boss rewards, etc.
    }

    public enum CardType
    {
        Attack,
        Skill,
        Power,        // Persistent effects that stay in play
        Curse         // Negative cards added to deck by enemies/events
    }

    public enum UpgradeTier
    {
        Base  = 0,
        Tier1 = 1,
        Tier2 = 2
    }

    public enum CharacterClass
    {
        Neutral,      // Available to all characters
        Orin,      // The Wall
        Kaelen,      // Rune Smith
        Anya        //
    }

    // ----------------------------------------------------------
    // Effect targeting
    // ----------------------------------------------------------

    public enum EffectTarget
    {
        Self,
        SingleEnemy,        // Player-selected target
        AllEnemies,
        RandomEnemy,
        AllCharacters       // Self + all enemies (e.g. a curse card)
    }

    // ----------------------------------------------------------
    // Effect triggers (when the effect fires)
    // ----------------------------------------------------------

    public enum EffectTrigger
    {
        OnPlay,             // Default — fires when the card is played
        OnDraw,             // Fires when the card enters the hand
        OnDiscard,          // Fires when the card is discarded (manually or end of turn)
        OnExhaust,          // Fires when the card is exhausted
        OnRetain,           // Fires if the card survives to the next turn in hand
        StartOfTurn,        // Powers that fire at the start of player's turn
        EndOfTurn           // Powers that fire at the end of player's turn
    }

    // ----------------------------------------------------------
    // Damage types (for resistance / weakness systems later)
    // ----------------------------------------------------------

    public enum DamageType
    {
        Physical,
        Magic,
        Fire,
        Poison,
        Dark,
        True              // Bypasses block
    }

    // ----------------------------------------------------------
    // Status effect types
    // ----------------------------------------------------------

    public enum StatusType
    {
        // Debuffs
        Weak,             // Target deals 25% less attack damage
        Exposed,       // Target takes 25% more damage
        Frail,            // Target gains 25% less block
        Poison,           // Lose HP equal to stacks at end of turn, stacks decrease by 1
        Burn,             // Lose 2 HP at end of turn per stack, does not decrease
        Shackled,         // Cannot play Attack cards next turn

        // Buffs
        Strength,         // Each stack adds +1 to all attack damage
        Dexterity,        // Each stack adds +1 to all block gained
        Thorns,           // Reflect N damage when hit
        Regeneration,     // Heal N HP at start of turn
        Ritual,           // Gain +1 Strength at end of each turn (permanent-ish)

        // Neutral / Mechanical
        Intangible,       // Reduce all damage taken to 1 this turn
        Barricade,        // Block does not expire at start of turn
        Energized         // Gain +1 energy next turn
    }

    public enum StatusStackBehavior
    {
        Additive,         // Each application adds to the stack count
        Refresh,          // Resets duration to the new value (non-stacking)
        Max               // Takes the higher of current vs applied value
    }

    // ----------------------------------------------------------
    // Scaling modes (for variable-magnitude effects)
    // ----------------------------------------------------------

    public enum ScalingMode
    {
        Flat,             // Fixed value — most cards
        EnergySpent,      // Value scales with energy spent (e.g. Whirlwind)
        CardsInHand,      // Value scales with cards currently in hand
        StatusStacks,     // Value scales with a specific status stack count
        TimesPlayed,      // Value scales with how many times this card has been played this run
        CurrentHP,        // Value based on current HP (missing or current)
        EnemyCount        // Value based on number of living enemies
    }
}
