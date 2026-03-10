using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class CombatEntity
    {
        public string EntityName { get; private set; }
        public bool IsPlayer { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        /// <summary>Red — actual life. Game over when this hits 0.</summary>
        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }

        private int _block;
        private int _armor;
        private int _barrier;

        public int CurrentBlock => _block;
        public int CurrentArmor => _armor;
        public int CurrentBarrier => _barrier;

        private static DefenseConfig _defenseConfig;
        public static void SetDefenseConfig(DefenseConfig config) => _defenseConfig = config;

        private Dictionary<StatusType, int> _statusStacks = new Dictionary<StatusType, int>();

        public event Action<DamageReport> OnDamageTaken;
        public event Action<int, DefenseLayer> OnDefenseGained;
        public event Action<int> OnHealed;
        public event Action OnDeath;
        public event Action<StatusType, int> OnStatusApplied;
        public event Action<StatusType> OnStatusRemoved;

        public CombatEntity(string name, int maxHealth, bool isPlayer = false, DefenseConfig config = null)
        {
            EntityName = name;
            IsPlayer = isPlayer;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            if (config != null) { _defenseConfig = config; }
        }

        public DamageReport TakeDamage(int rawAmount, DamageType damageType = DamageType.Physical, CombatEntity attacker = null)
        {
            if (IsDead) return DamageReport.Zero;

            var report = new DamageReport(damageType);
            int remaining = rawAmount;

            if (HasStatus(StatusType.Exposed))
                remaining = Mathf.CeilToInt(remaining * 1.25f);

            if (HasStatus(StatusType.Intangible))
                remaining = Mathf.Min(remaining, 1);

            if (attacker != null)
            {
                remaining += attacker.GetStatusStacks(StatusType.Strength);
                if (attacker.HasStatus(StatusType.Weak))
                    remaining = Mathf.FloorToInt(remaining * 0.75f);
                remaining = Mathf.Max(0, remaining);
            }

            if (_defenseConfig == null)
            {
                int hpDmg = Mathf.Min(remaining, CurrentHealth);
                CurrentHealth -= hpDmg;
                report.HealthDamage = hpDmg;
            }
            else
            {
                DefenseLayer[] order = _defenseConfig.GetRoutingOrder(damageType);
                Debug.Log($"[TakeDamage] Config={_defenseConfig != null}, Layers={order?.Length ?? -1}, Block={_block}");

                foreach (var layer in order)
                {
                    if (remaining <= 0) break;

                    float mult = _defenseConfig.GetMultiplier(damageType, layer);
                    switch (layer)
                    {
                        case DefenseLayer.Block:
                            remaining = DrainLayer(remaining, ref _block, mult, out int blockHit);
                            report.BlockDamage += blockHit;
                            break;

                        case DefenseLayer.Barrier:
                            remaining = DrainLayer(remaining, ref _barrier, mult, out int barrierHit);
                            report.BarrierDamage += barrierHit;
                            break;

                        case DefenseLayer.Armor:
                            remaining = DrainLayer(remaining, ref _armor, mult, out int armorHit);
                            report.ArmorDamage += armorHit;
                            break;

                        case DefenseLayer.Health:
                            int healthHit = Mathf.Min(remaining, CurrentHealth);
                            CurrentHealth -= healthHit;
                            report.HealthDamage += healthHit;
                            remaining -= healthHit;
                            break;
                    }
                }
            }

            OnDamageTaken?.Invoke(report);
            if (IsDead) OnDeath?.Invoke();
            return report;
        }

        private int DrainLayer(int rawDamage, ref int pool, float multiplier, out int poolPointsDrained)
        {
            if (pool <= 0) { poolPointsDrained = 0; return rawDamage; }

            float effectiveDamage = rawDamage * multiplier;

            if (effectiveDamage >= pool)
            {
                poolPointsDrained = pool;
                int rawAbsorbed = Mathf.CeilToInt(pool / multiplier);
                pool = 0;
                return Mathf.Max(0, rawDamage - rawAbsorbed);
            }
            else
            {
                poolPointsDrained = Mathf.FloorToInt(effectiveDamage);
                pool -= poolPointsDrained;
                return 0;
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            int actual = Mathf.Min(amount, MaxHealth - CurrentHealth);
            CurrentHealth += actual;
            if (actual > 0) OnHealed?.Invoke(actual);
        }

        public void SetMaxHealth(int newMax, bool healToFull = false)
        {
            MaxHealth = newMax;
            CurrentHealth = healToFull ? MaxHealth : Mathf.Min(CurrentHealth, MaxHealth);
        }

        /// <summary>
        /// Grants Block — the temporary white bar. Cleared each turn start.
        /// Neutral to all damage types. Respects Frail and Dexterity.
        /// </summary>
        public void GainBlock(int amount)
        {
            amount = ApplyDefenseModifiers(amount);
            if (amount <= 0) return;
            _block += amount;
            OnDefenseGained?.Invoke(amount, DefenseLayer.Block);
        }

        /// <summary>
        /// Grants Armor — the persistent gold bar. Resists physical, weak to magic.
        /// </summary>
        public void GainArmor(int amount)
        {
            amount = ApplyDefenseModifiers(amount);
            if (amount <= 0) return;
            _armor += amount;
            OnDefenseGained?.Invoke(amount, DefenseLayer.Armor);
        }

        /// <summary>
        /// Grants Barrier — the persistent blue bar. Resists magic, weak to physical.
        /// </summary>
        public void GainBarrier(int amount)
        {
            amount = ApplyDefenseModifiers(amount);
            if (amount <= 0) return;
            _barrier += amount;
            OnDefenseGained?.Invoke(amount, DefenseLayer.Barrier);
        }

        private int ApplyDefenseModifiers(int amount)
        {
            if (HasStatus(StatusType.Frail))
                amount = Mathf.FloorToInt(amount * 0.75f);
            amount += GetStatusStacks(StatusType.Dexterity);
            return Mathf.Max(0, amount);
        }

        /// <summary>
        /// Clears Block at the start of the entity's turn.
        /// Skipped if the Barricade status is active.
        /// Armor and Barrier are NOT cleared here — they are permanent.
        /// </summary>
        public void ClearBlock()
        {
            if (!HasStatus(StatusType.Barricade))
                _block = 0;
        }

        public void ClearBarrier(int amount = int.MaxValue) =>
            _barrier = Mathf.Max(0, CurrentBarrier - amount);

        public void StripBlock(int amount = int.MaxValue)
        {
            int stripped = Mathf.Min(amount, CurrentBlock);
            _block = Mathf.Max(0, CurrentBlock - stripped);
            if (stripped > 0)
            {
                var bd = new DamageReport(DamageType.True);
                bd.BlockDamage = stripped;
                OnDamageTaken?.Invoke(bd);
            }
        }

        public void StripArmor(int amount = int.MaxValue)
        {
            int stripped = Mathf.Min(amount, CurrentArmor);
            _armor = Mathf.Max(0, CurrentArmor - stripped);
            if (stripped > 0)
            {
                var bd = new DamageReport(DamageType.True);
                bd.ArmorDamage = stripped;
                OnDamageTaken?.Invoke(bd);
            }
        }

        public void StripBarrier(int amount = int.MaxValue)
        {
            int stripped = Mathf.Min(amount, CurrentBarrier);
            _barrier = Mathf.Max(0, CurrentBarrier - stripped);
            if (stripped > 0)
            {
                var bd = new DamageReport(DamageType.True);
                bd.BarrierDamage = stripped;
                OnDamageTaken?.Invoke(bd);
            }
        }
        public void ApplyStatus(StatusType type, int stacks,
                                StatusStackBehavior behavior = StatusStackBehavior.Additive)
        {
            if (!_statusStacks.ContainsKey(type)) _statusStacks[type] = 0;

            switch (behavior)
            {
                case StatusStackBehavior.Additive: _statusStacks[type] += stacks; break;
                case StatusStackBehavior.Refresh: _statusStacks[type] = stacks; break;
                case StatusStackBehavior.Max: _statusStacks[type] = Mathf.Max(_statusStacks[type], stacks); break;
            }

            OnStatusApplied?.Invoke(type, _statusStacks[type]);
        }

        public void RemoveStatus(StatusType type, int stacks = int.MaxValue)
        {
            if (!_statusStacks.ContainsKey(type)) return;
            _statusStacks[type] = Mathf.Max(0, _statusStacks[type] - stacks);
            if (_statusStacks[type] == 0)
            {
                _statusStacks.Remove(type);
                OnStatusRemoved?.Invoke(type);
            }
        }

        public void ClearAllStatuses() => _statusStacks.Clear();
        public bool HasStatus(StatusType type) => _statusStacks.ContainsKey(type) && _statusStacks[type] > 0;
        public int GetStatusStacks(StatusType type) =>
            _statusStacks.TryGetValue(type, out int val) ? val : 0;
        public IReadOnlyDictionary<StatusType, int> GetAllStatuses() => _statusStacks;

        public void ProcessStartOfTurnStatuses()
        {
            ClearBlock();           // ← Block expires here (unless Barricade)
                                    //   Armor and Barrier do NOT clear

            if (HasStatus(StatusType.Regeneration))
                Heal(GetStatusStacks(StatusType.Regeneration));

            RemoveStatus(StatusType.Intangible);
            RemoveStatus(StatusType.Shackled);
        }

        public int ProcessEndOfTurnStatuses()
        {
            int totalHPLost = 0;

            if (HasStatus(StatusType.Poison))
            {
                var bd = TakeDamage(GetStatusStacks(StatusType.Poison), DamageType.True);
                totalHPLost += bd.HealthDamage;
                RemoveStatus(StatusType.Poison, 1);
            }

            if (HasStatus(StatusType.Burn))
            {
                var bd = TakeDamage(GetStatusStacks(StatusType.Burn) * 2, DamageType.True);
                totalHPLost += bd.HealthDamage;
            }

            if (HasStatus(StatusType.Ritual))
                ApplyStatus(StatusType.Strength, GetStatusStacks(StatusType.Ritual));

            return totalHPLost;
        }

        // ----------------------------------------------------------
        // Convenience
        // ----------------------------------------------------------

        public int TotalDefense => CurrentBlock + CurrentArmor + CurrentBarrier;
        public bool HasAnyDefense => TotalDefense > 0;
    }

    // ----------------------------------------------------------
    //  DamageBreakdown — updated with BlockDamage field
    // ----------------------------------------------------------

    public struct DamageReport
    {
        public DamageType DamageType;
        public int HealthDamage;
        public int ArmorDamage;
        public int BarrierDamage;
        public int BlockDamage;         // ← NEW

        public int TotalDamage => HealthDamage + ArmorDamage + BarrierDamage + BlockDamage;
        public bool AnyDamageDealt => TotalDamage > 0;

        public DamageReport(DamageType type)
        {
            DamageType = type;
            HealthDamage = 0;
            ArmorDamage = 0;
            BarrierDamage = 0;
            BlockDamage = 0;
        }

        public static DamageReport Zero => new DamageReport(DamageType.Physical);
    }
}