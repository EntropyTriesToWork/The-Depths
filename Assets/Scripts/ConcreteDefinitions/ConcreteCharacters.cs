// ============================================================
//  ConcreteCharacters.cs
//  All playable character definitions. No ScriptableObjects.
//
//  CharacterDefinition replaces CharacterData SO entirely.
//  Starter deck is built directly by calling ConcreteCards_Orin
//  rather than resolving from ID strings.
//
//  Usage:
//    var def  = ConcreteCharacters.Get(CharacterClass.Orin);
//    var deck = def.BuildStarterDeck();
// ============================================================

using System.Collections.Generic;

namespace CardGame
{
    public class CharacterDefinition
    {
        public CharacterClass  Class          { get; set; }
        public string          CharacterName  { get; set; }
        public string          Description    { get; set; }
        public int             BaseHP         { get; set; }
        public int             BaseEnergy     { get; set; }
        public int             CardsDrawn     { get; set; }

        // Called once per run to produce a fresh deck
        public System.Func<List<CardInstance>> BuildStarterDeck { get; set; }

        // Returns the relic instance equipped at run start
        public System.Func<RelicData>          StarterRelic     { get; set; }
    }

    public static class ConcreteCharacters
    {
        public static CharacterDefinition Orin() => new CharacterDefinition
        {
            Class         = CharacterClass.Orin,
            CharacterName = "Orin",
            Description   = "A battle-hardened fighter who turns iron into victory.",
            BaseHP        = 80,
            BaseEnergy    = 3,
            CardsDrawn    = 5,

            BuildStarterDeck = () => ConcreteCards_Orin.StarterDeck(),
            StarterRelic     = () => new BurningBloodRelic()
        };

        // ----------------------------------------------------------
        //  Add characters here as they are designed, e.g.:
        //
        //  public static CharacterDefinition Hex() => new CharacterDefinition
        //  {
        //      Class         = CharacterClass.Hex,
        //      CharacterName = "Hex",
        //      Description   = "A cursed spellblade who weaponises suffering.",
        //      BaseHP        = 70,
        //      BaseEnergy    = 3,
        //      CardsDrawn    = 5,
        //      BuildStarterDeck = () => ConcreteCards_Hex.StarterDeck(),
        //      StarterRelic     = () => new ArcaneFocusRelic()
        //  };
        // ----------------------------------------------------------

        public static CharacterDefinition Get(CharacterClass cls) => cls switch
        {
            CharacterClass.Orin => Orin(),
            _                   => Orin()
        };

        public static List<CardData> GetCardPool(CharacterClass cls) => cls switch
        {
            CharacterClass.Orin => ConcreteCards_Orin.CardPool(),
            _                   => ConcreteCards_Orin.CardPool()
        };
    }
}
