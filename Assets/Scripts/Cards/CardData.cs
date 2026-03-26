using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cards/CardData", fileName = "NewCard")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite art;
    public int manaCost;
    public List<EffectEntry> effects;

    // Optionally, upgrade version reference
    public CardData upgradedVersion;
}

[System.Serializable]
public class EffectEntry
{
    public Effect effect;               // The ScriptableObject effect
    public EffectParameters parameters; // Values to pass at runtime
}

public enum CardTypes
{
    Attack, 
    Skill,
    Power,
    Curse,
}