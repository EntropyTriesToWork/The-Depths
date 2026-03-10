using UnityEngine;

namespace CardGame
{
    public abstract class CardEffect : ScriptableObject
    {
        [Header("Trigger")]
        [Tooltip("When during the card's lifecycle this effect fires.")]
        public EffectTrigger trigger = EffectTrigger.OnPlay;

        [Header("Targeting")]
        [Tooltip("Who this effect targets when it fires.")]
        public EffectTarget target = EffectTarget.SingleEnemy;

        [Header("Scaling")]
        [Tooltip("How the magnitude of this effect is calculated.")]
        public ScalingMode scalingMode = ScalingMode.Flat;

        [Tooltip("Base magnitude. Meaning depends on the effect type.")]
        public int baseMagnitude = 1;

        [Tooltip("Multiplier applied on top of base when scaling is active.")]
        public float scalingMultiplier = 1f;

        /// <summary>
        /// Execute this effect. Called by CombatManager when the
        /// card is played (or on the relevant trigger event).
        /// </summary>
        public abstract void Execute(CombatContext ctx);

        /// <summary>
        /// Returns a human-readable description for the card tooltip.
        /// Use {value} as a placeholder and call GetScaledMagnitude to
        /// resolve it when showing the actual number.
        /// </summary>
        public abstract string GetDescription();

        /// <summary>
        /// Calculates the effect's magnitude based on current scaling mode.
        /// Call this inside Execute() to get the final number.
        /// </summary>
        protected int GetScaledMagnitude(CombatContext ctx)
        {
            switch (scalingMode)
            {
                case ScalingMode.Flat:
                    return baseMagnitude;

                case ScalingMode.EnergySpent:
                    // Magnitude per energy point, multiplied by energy used so far this turn
                    return Mathf.RoundToInt(baseMagnitude * (ctx.MaxEnergy - ctx.CurrentEnergy) * scalingMultiplier);

                case ScalingMode.CardsInHand:
                    return Mathf.RoundToInt(baseMagnitude * ctx.DeckManager.HandCount * scalingMultiplier);

                case ScalingMode.StatusStacks:
                    // Subclass should set which status to scale off — override this case there
                    return baseMagnitude;

                case ScalingMode.EnemyCount:
                    return Mathf.RoundToInt(baseMagnitude * ctx.Enemies.Count * scalingMultiplier);

                case ScalingMode.CurrentHP:
                    return Mathf.RoundToInt(baseMagnitude * ctx.Player.CurrentHealth * scalingMultiplier);

                case ScalingMode.TimesPlayed:
                    // Tracked by CardInstance — subclass handles this
                    return baseMagnitude;

                default:
                    return baseMagnitude;
            }
        }

        /// <summary>
        /// Applies Strength modifier to a damage value for the given entity.
        /// </summary>
        protected int ApplyStrength(int baseDamage, CombatEntity attacker)
        {
            int strength = attacker.GetStatusStacks(StatusType.Strength);
            return Mathf.Max(0, baseDamage + strength);
        }

        /// <summary>
        /// Applies Weak modifier to a damage value for the given attacker.
        /// </summary>
        protected int ApplyWeak(int damage, CombatEntity attacker)
        {
            if (attacker.HasStatus(StatusType.Weak))
                return Mathf.FloorToInt(damage * 0.75f);
            return damage;
        }
    }
}
