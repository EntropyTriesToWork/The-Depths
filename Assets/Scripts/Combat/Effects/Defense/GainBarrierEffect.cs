using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Gain Barrier")]
    public class GainBarrierEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            int amount = GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
                t.GainBarrier(amount);
        }

        public override string GetDescription() =>
            $"Gain <b>{baseMagnitude}</b> <color=#4A90D9>Barrier</color>.";
    }

}
