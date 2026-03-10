using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Apply Status")]
    public class ApplyStatusEffect : CardEffect
    {
        [Header("Status")]
        public StatusType statusType;
        public StatusStackBehavior stackBehavior = StatusStackBehavior.Additive;

        public override void Execute(CombatContext ctx)
        {
            int stacks = GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);

            foreach (var t in targets)
                t.ApplyStatus(statusType, stacks, stackBehavior);
        }

        public override string GetDescription()
        {
            string targetStr = target == EffectTarget.Self ? "Gain" : "Apply";
            string stackStr = baseMagnitude > 1 ? $" {baseMagnitude}" : "";
            return $"{targetStr} <b>{statusType}{stackStr}</b>.";
        }
    }
}