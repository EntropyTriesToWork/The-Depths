using UnityEngine;

/// <summary>
/// Static utility that computes final damage values, accounting for
/// Strength, Weak, Vulnerable, and any other modifiers you add later.
/// 
/// Usage:
///   int finalDamage = DamageCalculator.Calculate(baseDamage, attacker, target);
///   target.GetComponent&lt;HealthComponent&gt;().TakeDamage(finalDamage, attacker);
/// </summary>
public static class DamageCalculator
{
    // Multipliers matching Slay the Spire values
    private const float VulnerableMultiplier = 1.5f;
    private const float WeakMultiplier       = 0.75f;

    /// <summary>
    /// Calculates final damage from <paramref name="attacker"/> to <paramref name="target"/>.
    /// Pass <c>null</c> for either if there is no Entity involved (e.g. environmental damage).
    /// </summary>
    /// <param name="baseDamage">Raw damage before modifiers.</param>
    /// <param name="attacker">The entity dealing the damage (used for Strength / Weak).</param>
    /// <param name="target">The entity receiving the damage (used for Vulnerable).</param>
    /// <returns>Final damage value, minimum 0.</returns>
    public static int Calculate(int baseDamage, Entity attacker, Entity target)
    {
        float damage = baseDamage;

        // ── Attacker modifiers ──────────────────────────────────────────────
        if (attacker != null)
        {
            var attackerHealth = attacker.GetComponent<HealthComponent>();
            if (attackerHealth != null)
            {
                // Strength adds flat damage
                int strength = attackerHealth.GetStatusStacks(StatusType.Strength);
                damage += strength;

                // Weak reduces outgoing damage
                if (attackerHealth.HasStatus(StatusType.Weak))
                    damage *= WeakMultiplier;
            }
        }

        // ── Target modifiers ────────────────────────────────────────────────
        if (target != null)
        {
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                // Vulnerable increases incoming damage
                if (targetHealth.HasStatus(StatusType.Vulnerable))
                    damage *= VulnerableMultiplier;
            }
        }

        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    /// <summary>
    /// Convenience overload: calculates damage with no entity context
    /// (e.g. a trap that deals a fixed amount).
    /// </summary>
    public static int Calculate(int baseDamage) => Calculate(baseDamage, null, null);

    /// <summary>
    /// Calculates the total damage a multi-hit attack will deal.
    /// Each hit is calculated independently (same as StS).
    /// </summary>
    /// <param name="damagePerHit">Base damage per individual hit.</param>
    /// <param name="hits">Number of hits.</param>
    public static int CalculateMultiHit(int damagePerHit, int hits, Entity attacker, Entity target)
    {
        int perHit = Calculate(damagePerHit, attacker, target);
        return perHit * hits;
    }
}
