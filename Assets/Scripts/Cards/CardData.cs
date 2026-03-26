using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/CardData", fileName = "NewCard")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite art;
    public string cardDescription;
    public int energyCost;
    public int manaCost;
    public CardType cardType;
    public Rarity rarity;
    public List<EffectEntry> effects;

    // Optionally, upgrade version reference
    public CardData upgradedVersion;
}

[System.Serializable]
public class EffectEntry
{
    public Effect effect;
    public EffectParameters parameters;
}

public enum CardType
{
    Attack, 
    Skill,
    Power,
    Curse,
}

public enum Rarity
{
    Crude,
    Basic,
    Refined,
    Perfect
}