using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the starting stats and loadout for a playable character class.
/// Create via: Assets > Create > Characters > CharacterClass
/// </summary>
[CreateAssetMenu(menuName = "Characters/CharacterClass", fileName = "NewCharacterClass")]
public class CharacterClass : ScriptableObject
{
    [Header("Identity")]
    public string className = "Unknown";
    public Sprite portrait;

    [Header("Base Stats")]
    public int startingMaxHealth = 80;
    public int startingMaxMana   = 3;
    public int startingGold      = 99;
    public int startingHandSize  = 5;

    [Header("Starting Deck")]
    public List<CardData> startingDeck = new();
}
