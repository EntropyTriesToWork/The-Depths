using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Gain Armor")]
    public class GainArmorEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            int amount = GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
                t.GainArmor(amount);
        }

        public override string GetDescription() =>
            $"Gain <b>{baseMagnitude}</b> <color=#E8A020>Armor</color>.";
    }
}