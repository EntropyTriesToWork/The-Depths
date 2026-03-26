using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    #region Singleton

    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region Inspector
    [Header("Combat Defaults")]
    [SerializeField] private int defaultMaxEnergy = 3;
    [SerializeField] private int defaultMaxMana = 5;
    [SerializeField] private int defaultHandSize = 5;
    #endregion

    #region Deck State
    public List<CardData> DrawPile { get; private set; } = new();
    public List<CardData> Hand { get; private set; } = new();
    public List<CardData> DiscardPile { get; private set; } = new();
    public int HandCount => Hand.Count;
    public int TotalCardCount => DrawPile.Count + Hand.Count + DiscardPile.Count;

    #endregion

    #region Energy

    public int CurrentEnergy { get; private set; }
    public int MaxEnergy { get; private set; }

    #endregion

    #region Mana / Overdrive

    public int CurrentMana { get; private set; }
    public int MaxMana { get; private set; }
    public bool IsOverdrive => CurrentMana >= MaxMana;

    #endregion

    #region Turn Tracking

    public int TurnNumber { get; private set; }
    public bool IsPlayerTurn { get; private set; }

    private PlayerEntity player; // injected via InitializeCombat

    #endregion

    #region Local Events

    public event System.Action<CardData> OnCardDrawn;
    public event System.Action<CardData> OnCardPlayed;
    public event System.Action<CardData> OnCardDiscarded;
    public event System.Action<int, int> OnEnergyChanged; // (current, max)
    public event System.Action<int, int, bool> OnManaChanged;   // (current, max, isOverdrive)

    #endregion

    #region Initialisation

    /// <summary>
    /// Resets all combat state and prepares for a new fight.
    /// Pass -1 for any optional value to fall back to the inspector default.
    /// </summary>
    /// <param name="deck">Shuffled copy from PlayerInventory.GetCombatDeckCopy().</param>
    /// <param name="playerEntity">The PlayerEntity present in the combat scene.</param>
    /// <param name="maxEnergy">Override from CharacterClass; -1 uses inspector default.</param>
    /// <param name="maxMana">Override from CharacterClass; -1 uses inspector default.</param>
    /// <param name="handSize">Cards drawn at the start of each player turn.</param>
    public void InitializeCombat(
        List<CardData> deck,
        PlayerEntity playerEntity,
        int maxEnergy = -1,
        int maxMana = -1,
        int handSize = -1)
    {
        player = playerEntity;
        TurnNumber = 0;
        IsPlayerTurn = false;

        DrawPile = new List<CardData>(deck);
        Hand = new List<CardData>();
        DiscardPile = new List<CardData>();

        MaxEnergy = maxEnergy > 0 ? maxEnergy : defaultMaxEnergy;
        CurrentEnergy = MaxEnergy;

        MaxMana = maxMana > 0 ? maxMana : defaultMaxMana;
        CurrentMana = 0; // mana is earned, never gifted at combat start

        defaultHandSize = handSize > 0 ? handSize : defaultHandSize;

        GameEvents.Raise(new OnCombatStartedEvent(null)); // enemies supplied by the encounter system
    }

    #endregion

    #region Turn Flow

    /// <summary>Starts the player's turn: decay mana first, then refill energy and draw.</summary>
    public void StartPlayerTurn()
    {
        TurnNumber++;
        IsPlayerTurn = true;

        DecayMana();    // mana decays before anything else so the player sees their opening state
        RefillEnergy();
        DrawCards(defaultHandSize);

        if (player != null)
            GameEvents.Raise(new OnCombatTurnStartEvent(player, TurnNumber)); // lets Entity clear block

        player?.TakeTurn(); // raises OnPlayerTurnStartedEvent so UI can enable input
    }
    /// <summary>Called by PlayerEntity.EndTurn() when the player clicks the end-turn button.</summary>
    public void EndPlayerTurn()
    {
        IsPlayerTurn = false;
        if (player != null)
            GameEvents.Raise(new OnCombatTurnEndEvent(player, TurnNumber));
        GameEvents.Raise(new OnPlayerTurnEndedEvent());
        DiscardHand(); //Discard hand goes after end turn events (in-case of effects that effect cards still in hand) 
    }

    #endregion

    #region Energy Management

    private void RefillEnergy()
    {
        CurrentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    private bool TrySpendEnergy(int amount)
    {
        if (CurrentEnergy < amount) return false;
        CurrentEnergy -= amount;
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
        return true;
    }

    public void AddEnergy(int amount)
    {
        CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    public void IncreaseMaxEnergy(int amount)
    {
        MaxEnergy += amount;
        CurrentEnergy += amount;
        OnEnergyChanged?.Invoke(CurrentEnergy, MaxEnergy);
    }

    #endregion

    #region Mana Management

    private void AddMana(int amount)
    {
        if (amount <= 0) return;
        bool wasOverdrive = IsOverdrive;
        CurrentMana = Mathf.Min(MaxMana, CurrentMana + amount);
        OnManaChanged?.Invoke(CurrentMana, MaxMana, IsOverdrive);
        if (!wasOverdrive && IsOverdrive)
            GameEvents.Raise(new OnOverdriveActivatedEvent(player)); // first time crossing threshold this turn
    }

    private void DecayMana()
    {
        int decay = Mathf.FloorToInt(MaxMana / 2f); // lose half max mana each turn start
        CurrentMana = Mathf.Max(0, CurrentMana - decay);
        OnManaChanged?.Invoke(CurrentMana, MaxMana, IsOverdrive);
    }

    public void IncreaseMaxMana(int amount)
    {
        MaxMana += amount;
        OnManaChanged?.Invoke(CurrentMana, MaxMana, IsOverdrive);
    }

    #endregion

    #region Card Drawing

    public void DrawCards(int amount)
    {
        for (int i = 0; i < amount; i++) DrawCard();
    }

    private void DrawCard()
    {
        if (DrawPile.Count == 0)
        {
            if (DiscardPile.Count == 0) return;
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
        }

        CardData card = DrawPile[0];
        DrawPile.RemoveAt(0);
        Hand.Add(card);

        OnCardDrawn?.Invoke(card);
        GameEvents.Raise(new OnCardDrawnEvent(card));
    }

    #endregion

    #region Card Playing
    public bool TryPlayCard(CardData card, EffectContext context)
    {
        if (!Hand.Contains(card)) return false;
        if (!TrySpendEnergy(card.manaCost)) return false; // energy is always required

        if (IsOverdrive && player != null)
        {
            int hpCost = CurrentMana; // deeper in overdrive = more dangerous
            player.Health.TakeDamage(hpCost, null); // bypasses block intentionally
            GameEvents.Raise(new OnOverdriveDamageTakenEvent(player, card, hpCost));
        }

        Hand.Remove(card); // remove before effects fire to prevent re-play exploits

        foreach (var entry in card.effects)
            entry.effect.Apply(context, entry.parameters);

        int totalManaGain = 0;
        foreach (var entry in card.effects)
            totalManaGain += entry.parameters.manaGain;
        AddMana(totalManaGain); // mana gain applied after effects resolve

        bool exhausted = card.effects.Exists(e => e.parameters.isExhaust);
        if (!exhausted)
            DiscardPile.Add(card); // exhausted cards leave combat entirely

        OnCardPlayed?.Invoke(card);
        GameEvents.Raise(new OnCardPlayedEvent(card, context.targets));
        return true;
    }

    #endregion

    #region Discarding
    public void DiscardCard(CardData card)
    {
        if (!Hand.Remove(card)) return;
        DiscardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
        GameEvents.Raise(new OnCardDiscardedEvent(card));
    }

    public void DiscardHand()
    {
        for (int i = Hand.Count - 1; i >= 0; i--)
            DiscardCard(Hand[i]);
    }

    #endregion

    #region Helpers

    private void Shuffle(List<CardData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
    #endregion
}

#region Overdrive Events
/// <summary>Raised the first moment mana crosses the max threshold in a given turn.</summary>
public struct OnOverdriveActivatedEvent
{
    public PlayerEntity player;
    public OnOverdriveActivatedEvent(PlayerEntity p) => player = p;
}
/// <summary>Raised each time an overdrive card deals HP damage to the player.</summary>
public struct OnOverdriveDamageTakenEvent
{
    public PlayerEntity player;
    public CardData card;
    public int damage;
    public OnOverdriveDamageTakenEvent(PlayerEntity p, CardData c, int d) { player = p; card = c; damage = d; }
}

#endregion