using UnityEngine;

namespace CardGame
{
    public class EnemyInstance
    {
        public EnemyData Data { get; private set; }
        public CombatEntity Entity { get; private set; }
        public EnemyAction CurrentIntent { get; private set; }
        public EnemyAction PreviousIntent { get; private set; }

        private int _patternIndex = 0;

        public EnemyInstance(EnemyData data, DefenseConfig defenseConfig = null)
        {
            Data = data;
            Entity = new CombatEntity(data.enemyName, data.baseHP, isPlayer: false, defenseConfig);
        }

        /// <summary>
        /// Selects and stores the next action this enemy will take.
        /// Call at the START of the player's turn so the intent is
        /// visible to the player before they make decisions.
        /// </summary>
        public void SelectNextIntent()
        {
            if (Data.actionPattern == null || Data.actionPattern.Length == 0)
                return;

            float hpPercent = (float)Entity.CurrentHealth / Entity.MaxHealth;

            // Cycle through the pattern in order, skipping actions
            // that fail their HP condition
            int attempts = 0;
            while (attempts < Data.actionPattern.Length)
            {
                var candidate = Data.actionPattern[_patternIndex % Data.actionPattern.Length];
                _patternIndex++;
                attempts++;

                if (candidate != null && candidate.IsValidAt(hpPercent))
                {
                    PreviousIntent = CurrentIntent;
                    CurrentIntent = candidate;
                    return;
                }
            }

            // All actions failed their HP condition — keep previous intent
        }

        /// <summary>
        /// Executes the stored intent against the given context.
        /// Call during the enemy's turn.
        /// </summary>
        public void ExecuteIntent(CombatContext ctx)
        {
            if (CurrentIntent == null || Entity.IsDead) return;

            // 1. Self block (enemy defends before acting)
            if (CurrentIntent.selfBlock > 0)
                Entity.GainBlock(CurrentIntent.selfBlock);

            // 2. Self buff
            if (CurrentIntent.appliesSelfStatus && CurrentIntent.selfStatusStacks > 0)
                Entity.ApplyStatus(CurrentIntent.applySelfStatus, CurrentIntent.selfStatusStacks);

            // 3. Damage (multi-hit)
            if (CurrentIntent.damage > 0)
            {
                int dmg = CurrentIntent.damage;

                // Apply enemy Strength
                dmg += Entity.GetStatusStacks(StatusType.Strength);

                // Apply enemy Weak
                if (Entity.HasStatus(StatusType.Weak))
                    dmg = Mathf.FloorToInt(dmg * 0.75f);

                for (int i = 0; i < CurrentIntent.hitCount; i++)
                {
                    if (ctx.Player.IsDead) break;

                    // Player Intangible: cap damage at 1
                    int finalDmg = ctx.Player.HasStatus(StatusType.Intangible) ? 1 : dmg;
                    ctx.Player.TakeDamage(finalDmg, DamageType.Physical);

                    // Thorns: reflect damage back to enemy
                    int thorns = ctx.Player.GetStatusStacks(StatusType.Thorns);
                    if (thorns > 0)
                        Entity.TakeDamage(thorns, DamageType.True);
                }
            }

            // 4. Debuff applied to player
            if (CurrentIntent.appliesStatus && CurrentIntent.statusStacks > 0)
                ctx.Player.ApplyStatus(CurrentIntent.applyStatus, CurrentIntent.statusStacks);
        }

        public bool IsDead => Entity.IsDead;

        /// <summary>
        /// Returns a human-readable intent description for IMGUI display.
        /// Uses customIntentDescription if set, otherwise infers from fields.
        /// </summary>
        public string GetIntentDescription()
        {
            if (CurrentIntent == null) return "???";

            if (!string.IsNullOrEmpty(CurrentIntent.customIntentDescription))
                return CurrentIntent.customIntentDescription;

            string desc = "";

            if (CurrentIntent.damage > 0)
            {
                desc += CurrentIntent.hitCount > 1
                    ? $"Attack {CurrentIntent.damage} x{CurrentIntent.hitCount} = {CurrentIntent.TotalDamage}"
                    : $"Attack {CurrentIntent.TotalDamage}";
            }

            if (CurrentIntent.selfBlock > 0)
                desc += $"{(desc.Length > 0 ? ", " : "")}Block {CurrentIntent.selfBlock}";

            if (CurrentIntent.appliesStatus && CurrentIntent.statusStacks > 0)
                desc += $"{(desc.Length > 0 ? ", " : "")}{CurrentIntent.applyStatus} {CurrentIntent.statusStacks}";

            if (CurrentIntent.appliesSelfStatus && CurrentIntent.selfStatusStacks > 0)
                desc += $"{(desc.Length > 0 ? ", " : "")}+{CurrentIntent.selfStatusStacks} {CurrentIntent.applySelfStatus}";

            return desc.Length > 0 ? desc : CurrentIntent.actionName;
        }
    }
}