using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns everything the player carries between combats:
/// full card collection (master deck), potions, gold, relics,
/// and the canonical current/max health values that persist across rooms.
/// 
/// This is NOT the combat deck — DeckManager draws from a copy of
/// <see cref="masterDeck"/> at the start of each combat.
/// 
/// Persists across scenes via DontDestroyOnLoad.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMaxHealth = 80;
    [SerializeField] private int startingGold      = 99;
    [SerializeField] private int maxPotionSlots    = 3;

    [Header("Starting Deck (ScriptableObjects)")]
    [SerializeField] private List<CardData> startingDeck = new();

    public int MaxHealth     { get; private set; }
    public int CurrentHealth { get; private set; }

    public int Gold { get; private set; }
    public IReadOnlyList<CardData> MasterDeck => masterDeck;
    private readonly List<CardData> masterDeck = new();
    public IReadOnlyList<PotionData> Potions => potions;
    private readonly List<PotionData> potions = new();
    public int MaxPotionSlots => maxPotionSlots;
    public IReadOnlyList<RelicData> Relics => relics;
    private readonly List<RelicData> relics = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void InitializeForNewRun()
    {
        MaxHealth     = startingMaxHealth;
        CurrentHealth = startingMaxHealth;
        Gold          = startingGold;

        masterDeck.Clear();
        masterDeck.AddRange(startingDeck);

        potions.Clear();
        relics.Clear();
    }
    public void SyncHealthFromCombat(int current, int max)
    {
        CurrentHealth = Mathf.Clamp(current, 0, max);
        MaxHealth     = max;
    }
    public void HealOutOfCombat(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
    }
    public void IncreaseMaxHealth(int amount)
    {
        MaxHealth     += amount;
        CurrentHealth += amount;
    }
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        int prev = Gold;
        Gold += amount;
        GameEvents.Raise(new OnGoldChangedEvent(prev, Gold));
    }
    public bool SpendGold(int amount)
    {
        if (amount > Gold) return false;
        int prev = Gold;
        Gold -= amount;
        GameEvents.Raise(new OnGoldChangedEvent(prev, Gold));
        return true;
    }
    public void AddCard(CardData card)
    {
        if (card == null) return;
        masterDeck.Add(card);
        GameEvents.Raise(new OnCardObtainedEvent(card));
    }

    public bool RemoveCard(CardData card)
    {
        if (!masterDeck.Remove(card)) return false;
        GameEvents.Raise(new OnCardRemovedFromDeckEvent(card));
        return true;
    }

    public bool UpgradeCard(CardData card)
    {
        if (card?.upgradedVersion == null) return false;
        int idx = masterDeck.IndexOf(card);
        if (idx < 0) return false;

        masterDeck[idx] = card.upgradedVersion;
        GameEvents.Raise(new OnCardUpgradedEvent(card, card.upgradedVersion));
        return true;
    }
    public List<CardData> GetCombatDeckCopy()
    {
        var copy = new List<CardData>(masterDeck);
        // Fisher-Yates
        for (int i = 0; i < copy.Count; i++)
        {
            int rnd = Random.Range(i, copy.Count);
            (copy[i], copy[rnd]) = (copy[rnd], copy[i]);
        }
        return copy;
    }

    public bool CanPickUpPotion => potions.Count < maxPotionSlots;

    public bool AddPotion(PotionData potion)
    {
        if (potion == null || !CanPickUpPotion) return false;
        potions.Add(potion);
        return true;
    }

    public bool UsePotion(PotionData potion, Entity target)
    {
        if (!potions.Remove(potion)) return false;
        GameEvents.Raise(new OnPotionUsedEvent(potion, target));
        return true;
    }

    public bool DiscardPotion(PotionData potion)
    {
        return potions.Remove(potion);
    }
    public void AddRelic(RelicData relic)
    {
        if (relic == null) return;
        relics.Add(relic);
        GameEvents.Raise(new OnRelicObtainedEvent(relic));
    }

    public bool HasRelic(RelicData relic) => relics.Contains(relic);

    [ContextMenu("Print Inventory Summary")]
    private void PrintSummary()
    {
        Debug.Log($"[PlayerInventory] HP: {CurrentHealth}/{MaxHealth} | Gold: {Gold} | " +
                  $"Deck: {masterDeck.Count} cards | Potions: {potions.Count}/{maxPotionSlots} | " +
                  $"Relics: {relics.Count}");
    }
}
