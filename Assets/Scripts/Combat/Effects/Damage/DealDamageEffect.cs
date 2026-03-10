// ==========================================================
//  DEAL DAMAGE
// ==========================================================

using CardGame;
using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Deal Damage")]
    public class DealDamageEffect : CardEffect
    {
        [Header("Damage Options")]
        public DamageType damageType = DamageType.Physical;

        [Tooltip("If true, deals damage to all resolved targets independently.")]
        public bool hitsAllTargets = false;

        public override void Execute(CombatContext ctx)
        {
            int magnitude = GetScaledMagnitude(ctx);

            // Apply attacker modifiers
            magnitude = ApplyStrength(magnitude, ctx.Player);
            magnitude = ApplyWeak(magnitude, ctx.Player);

            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                int dealt = t.TakeDamage(magnitude, damageType).TotalDamage;
                ctx.TrackDamageDealt(dealt);
            }
        }

        public override string GetDescription()
        {
            if (scalingMode == ScalingMode.EnergySpent)
                return $"Deal {baseMagnitude}(X) {damageType} damage.";

            string targetStr = target == EffectTarget.AllEnemies ? "ALL enemies" : "an enemy";
            return $"Deal <b>{baseMagnitude}</b> {damageType} damage to {targetStr}.";
        }
    }
}