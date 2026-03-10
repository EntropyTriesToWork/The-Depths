using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Gain Block")]
    public class GainBlockEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            int amount = GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                t.GainBlock(amount);
                if (t.IsPlayer)
                    ctx.TrackBlockGained(amount);
            }
        }

        public override string GetDescription() =>
            $"Gain <b>{baseMagnitude}</b> Block.";
    }
}