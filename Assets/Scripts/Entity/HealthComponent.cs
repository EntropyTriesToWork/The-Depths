using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles current/max health, block, and all status effects for any Entity.
/// Attach to the same GameObject as your Entity component.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private int maxHealth = 80;
    [SerializeField] private int currentHealth;

    // ──────────────────────────────────────────────
    //  Runtime state
    // ──────────────────────────────────────────────
    private int block;
    private Entity owner;

    /// <summary>Key = StatusType, Value = stacks / duration.</summary>
    private readonly Dictionary<StatusType, int> statusEffects = new();

    // ──────────────────────────────────────────────
    //  Public accessors
    // ──────────────────────────────────────────────
    public int MaxHealth    => maxHealth;
    public int CurrentHealth => currentHealth;
    public int Block        => block;
    public bool IsDead      => currentHealth <= 0;

    // ──────────────────────────────────────────────
    //  Unity lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        owner = GetComponent<Entity>();
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        GameEvents.Subscribe<OnCombatTurnEndEvent>(HandleTurnEnd);
    }

    private void OnDisable()
    {
        GameEvents.Unsubscribe<OnCombatTurnEndEvent>(HandleTurnEnd);
    }

    // ──────────────────────────────────────────────
    //  Health
    // ──────────────────────────────────────────────

    /// <summary>
    /// Applies incoming damage after block and Vulnerable/Weak modifiers are resolved
    /// by DamageCalculator. Call DamageCalculator.Calculate first, then pass the
    /// result here.
    /// </summary>
    public void TakeDamage(int amount, Entity source)
    {
        if (amount <= 0) return;

        int absorbed = Mathf.Min(block, amount);
        block -= absorbed;
        int remaining = amount - absorbed;

        if (remaining > 0)
        {
            currentHealth = Mathf.Max(0, currentHealth - remaining);
            GameEvents.Raise(new OnEntityDamagedEvent(owner, source, remaining));

            if (IsDead)
                GameEvents.Raise(new OnEntityDiedEvent(owner));
        }
    }

    /// <summary>Heals the entity, capped at max health.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        int before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        int actual = currentHealth - before;
        if (actual > 0)
            GameEvents.Raise(new OnEntityHealedEvent(owner, actual));
    }

    /// <summary>Permanently raises max health (e.g. from relics / boss rewards).</summary>
    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount; // match Slay the Spire behaviour
    }

    // ──────────────────────────────────────────────
    //  Block
    // ──────────────────────────────────────────────

    public void AddBlock(int amount)
    {
        if (amount <= 0) return;
        block += amount;
        GameEvents.Raise(new OnBlockGainedEvent(owner, amount));
    }

    /// <summary>Removes all block (called at start of entity's turn).</summary>
    public void ClearBlock()
    {
        block = 0;
    }

    // ──────────────────────────────────────────────
    //  Status effects
    // ──────────────────────────────────────────────

    public void ApplyStatus(StatusType status, int stacks)
    {
        if (stacks == 0) return;

        if (statusEffects.ContainsKey(status))
            statusEffects[status] += stacks;
        else
            statusEffects[status] = stacks;

        // Cap negative stacks at 0 – let effects remove themselves cleanly
        if (statusEffects[status] <= 0)
            statusEffects.Remove(status);

        GameEvents.Raise(new OnStatusAppliedEvent(owner, status, stacks));
    }

    public void RemoveStatus(StatusType status)
    {
        if (statusEffects.Remove(status))
            GameEvents.Raise(new OnStatusRemovedEvent(owner, status));
    }

    public bool HasStatus(StatusType status) => statusEffects.ContainsKey(status);

    public int GetStatusStacks(StatusType status) =>
        statusEffects.TryGetValue(status, out int stacks) ? stacks : 0;

    // ──────────────────────────────────────────────
    //  Turn-end processing (poison tick, buff decay, etc.)
    // ──────────────────────────────────────────────

    private void HandleTurnEnd(OnCombatTurnEndEvent evt)
    {
        if (evt.entity != owner) return;

        ProcessPoison();
        DecayStatuses();
    }

    private void ProcessPoison()
    {
        int stacks = GetStatusStacks(StatusType.Poison);
        if (stacks <= 0) return;

        // Poison deals stacks as damage (bypasses block in StS, replicating that here)
        currentHealth = Mathf.Max(0, currentHealth - stacks);
        GameEvents.Raise(new OnEntityDamagedEvent(owner, null, stacks));

        // Reduce by 1 each turn
        ApplyStatus(StatusType.Poison, -1);

        if (IsDead)
            GameEvents.Raise(new OnEntityDiedEvent(owner));
    }

    private void DecayStatuses()
    {
        // Weak and Vulnerable lose 1 stack each turn
        ApplyStatus(StatusType.Weak, -1);
        ApplyStatus(StatusType.Vulnerable, -1);
    }
}

// ──────────────────────────────────────────────────────────────────────────────
//  Events raised by HealthComponent
// ──────────────────────────────────────────────────────────────────────────────

public struct OnEntityDamagedEvent
{
    public Entity target;
    public Entity source; // null for environmental damage (e.g. poison)
    public int amount;
    public OnEntityDamagedEvent(Entity t, Entity s, int a) { target = t; source = s; amount = a; }
}

public struct OnEntityHealedEvent
{
    public Entity target;
    public int amount;
    public OnEntityHealedEvent(Entity t, int a) { target = t; amount = a; }
}

public struct OnEntityDiedEvent
{
    public Entity entity;
    public OnEntityDiedEvent(Entity e) => entity = e;
}

public struct OnBlockGainedEvent
{
    public Entity entity;
    public int amount;
    public OnBlockGainedEvent(Entity e, int a) { entity = e; amount = a; }
}

public struct OnStatusAppliedEvent
{
    public Entity entity;
    public StatusType status;
    public int stacks;
    public OnStatusAppliedEvent(Entity e, StatusType s, int st) { entity = e; status = s; stacks = st; }
}

public struct OnStatusRemovedEvent
{
    public Entity entity;
    public StatusType status;
    public OnStatusRemovedEvent(Entity e, StatusType s) { entity = e; status = s; }
}
