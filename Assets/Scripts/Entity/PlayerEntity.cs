using UnityEngine;

/// <summary>
/// Concrete <see cref="Entity"/> for the player character.
/// 
/// Responsibilities:
///   - Bridges <see cref="PlayerInventory"/> (persistent run data) and
///     <see cref="CombatManager"/> (per-combat state).
///   - Signals the UI that it is the player's turn via <see cref="OnPlayerTurnStartedEvent"/>.
///   - Syncs health back to <see cref="PlayerInventory"/> when combat ends.
///   - Exposes <see cref="EndTurn"/> for the UI end-turn button.
///
/// AI / card-play logic lives in the UI layer and <see cref="CombatManager"/>,
/// not here.
/// </summary>
public class PlayerEntity : Entity
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Character Class")]
    [SerializeField] private CharacterClass characterClass;

    // ──────────────────────────────────────────────
    //  Public accessors
    // ──────────────────────────────────────────────
    public CharacterClass CharacterClass => characterClass;

    // ──────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────
    protected override void Awake()
    {
        base.Awake();

        if (characterClass != null)
            entityName = characterClass.className;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        GameEvents.Subscribe<OnCombatEndedEvent>(HandleCombatEnded);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.Unsubscribe<OnCombatEndedEvent>(HandleCombatEnded);
    }

    // ──────────────────────────────────────────────
    //  Entity implementation
    // ──────────────────────────────────────────────

    /// <summary>
    /// The player's "turn" is driven by UI input, not an AI loop.
    /// Raising <see cref="OnPlayerTurnStartedEvent"/> lets the UI layer
    /// enable cards, show the end-turn button, etc.
    /// </summary>
    public override void TakeTurn()
    {
        GameEvents.Raise(new OnPlayerTurnStartedEvent());
    }

    protected override void OnTurnStart()
    {
        // Base already cleared block. CombatManager handles mana refill and card draw
        // via its own subscription to OnCombatTurnStartEvent.
    }

    protected override void OnDeath()
    {
        base.OnDeath();
        GameManager.Instance?.EndRun(playerWon: false);
    }

    // ──────────────────────────────────────────────
    //  Combat actions (called by UI)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Called by the end-turn UI button.
    /// Discards the hand and signals the combat system to proceed to enemy turns.
    /// </summary>
    public void EndTurn()
    {
        CombatManager.Instance?.EndPlayerTurn();
        GameEvents.Raise(new OnPlayerTurnEndedEvent());
    }

    /// <summary>
    /// Uses a potion from inventory, applying its effect to <paramref name="target"/>.
    /// Target defaults to self if null.
    /// </summary>
    public void UsePotion(PotionData potion, Entity target = null)
    {
        PlayerInventory.Instance?.UsePotion(potion, target ?? this);
    }

    // ──────────────────────────────────────────────
    //  Event handlers
    // ──────────────────────────────────────────────

    private void HandleCombatEnded(OnCombatEndedEvent evt)
    {
        // Sync current HP back to PlayerInventory so it persists to the next room
        PlayerInventory.Instance?.SyncHealthFromCombat(Health.CurrentHealth, Health.MaxHealth);
    }
}
