using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Character Data", fileName = "NewCharacter")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string         characterName  = "Orin, The Wall";
        public CharacterClass characterClass = CharacterClass.Orin;
        public string         description    = "An unwavering warrior that can withstand any attack thrown at him.";

        [Header("Stats")]
        public int baseHP       = 80;
        public int baseEnergy   = 3;
        public int startingGold = 50;

        [Header("Starter Deck")]
        [Tooltip("These cards are given to the player at the start of every run.")]
        public List<CardData> starterDeck;

        [Header("Starter Relic")]
        [Tooltip("The relic this character starts every run with. Can be null.")]
        public RelicData starterRelic;

        [Header("Card Pool")]
        [Tooltip("Cards specific to this character that can appear in rewards and shops." +
                 " Neutral cards are always included automatically.")]
        public List<CardData> classCardPool;

        [Header("Visuals")]
        public Sprite portraitSprite;
        public Sprite idleSprite;
        public Sprite attackSprite;
        public Sprite hurtSprite;
        public Color  accentColor = Color.white;
    }
}
