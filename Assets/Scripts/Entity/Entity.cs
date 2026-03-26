using UnityEngine;

/// <summary>
/// Abstract base for every combatant in the game (Player, enemies, bosses, etc.).
/// Requires a <see cref="HealthComponent"/> on the same GameObject.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public abstract class Entity : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Identity")]
    public string entityName = "Unknown Entity";

    [Header("Combat")]
    [SerializeField] protected int baseStrength = 0;

    // ──────────────────────────────────────────────
    //  Components (cached for performance)
    // ──────────────────────────────────────────────
    public HealthComponent Health { get; private set; }

    // ──────────────────────────────────────────────
    //  State
    // ──────────────────────────────────────────────
    public bool IsAlive     => Health != null && !Health.IsDead;
    public bool IsPlayer    => this is PlayerEntity;
    public bool IsEnemy     => !IsPlayer;

    // ──────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────
    protected virtual void Awake()
    {
        Health = GetComponent<HealthComponent>();
    }

    protected virtual void OnEnable()
    {
        GameEvents.Subscribe<OnEntityDiedEvent>(HandleEntityDied);
        GameEvents.Subscribe<OnCombatTurnStartEvent>(HandleTurnStart);
    }

    protected virtual void OnDisable()
    {
        GameEvents.Unsubscribe<OnEntityDiedEvent>(HandleEntityDied);
        GameEvents.Unsubscribe<OnCombatTurnStartEvent>(HandleTurnStart);
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Entry point for dealing damage TO this entity from <paramref name="source"/>.
    /// Runs through DamageCalculator before forwarding to HealthComponent.
    /// </summary>
    public void ReceiveDamage(int baseDamage, Entity source)
    {
        int finalDamage = DamageCalculator.Calculate(baseDamage, source, this);
        Health.TakeDamage(finalDamage, source);
    }

    /// <summary>Called each turn to perform this entity's action (attack AI, etc.).</summary>
    public abstract void TakeTurn();

    // ──────────────────────────────────────────────
    //  Event handlers
    // ──────────────────────────────────────────────

    private void HandleEntityDied(OnEntityDiedEvent evt)
    {
        if (evt.entity == this)
            OnDeath();
    }

    private void HandleTurnStart(OnCombatTurnStartEvent evt)
    {
        if (evt.entity != this) return;

        // Block resets at the start of this entity's turn (StS behaviour)
        Health.ClearBlock();
        OnTurnStart();
    }

    // ──────────────────────────────────────────────
    //  Overridable hooks
    // ──────────────────────────────────────────────

    /// <summary>Called when this entity's health reaches 0.</summary>
    protected virtual void OnDeath()
    {
        Debug.Log($"{entityName} has died.");
    }

    /// <summary>Called at the start of this entity's turn (after block is cleared).</summary>
    protected virtual void OnTurnStart() { }
}

// ──────────────────────────────────────────────────────────────────────────────
//  Entity-specific combat events
// ──────────────────────────────────────────────────────────────────────────────

public struct OnCombatTurnStartEvent
{
    public Entity entity;
    public int turnNumber;
    public OnCombatTurnStartEvent(Entity e, int t) { entity = e; turnNumber = t; }
}

public struct OnCombatTurnEndEvent
{
    public Entity entity;
    public int turnNumber;
    public OnCombatTurnEndEvent(Entity e, int t) { entity = e; turnNumber = t; }
}
