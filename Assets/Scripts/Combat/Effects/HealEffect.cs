using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Heal")]
    public class HealEffect : CardEffect
    {
        [Tooltip("If true, heal is calculated as a % of max HP instead of a flat amount.")]
        public bool isPercentage = false;

        public override void Execute(CombatContext ctx)
        {
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                int healAmount = isPercentage
                    ? Mathf.RoundToInt(t.MaxHealth * (baseMagnitude / 100f))
                    : GetScaledMagnitude(ctx);
                t.Heal(healAmount);
            }
        }

        public override string GetDescription()
        {
            if (isPercentage)
                return $"Heal <b>{baseMagnitude}%</b> of Max HP.";
            return $"Heal <b>{baseMagnitude}</b> HP.";
        }
    }
}