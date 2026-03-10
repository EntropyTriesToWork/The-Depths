using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Gain Mixed Defense")]
    public class GainMixedDefenseEffect : CardEffect
    {
        public int armorAmount = 0;
        public int barrierAmount = 0;

        public override void Execute(CombatContext ctx)
        {
            int armor = armorAmount > 0 ? armorAmount : GetScaledMagnitude(ctx);
            int barrier = barrierAmount > 0 ? barrierAmount : GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                t.GainArmor(armor);
                t.GainBarrier(barrier);
            }
        }

        public override string GetDescription()
        {
            int a = armorAmount > 0 ? armorAmount : baseMagnitude;
            int b = barrierAmount > 0 ? barrierAmount : baseMagnitude;
            return $"Gain <b>{a}</b> <color=#E8A020>Armor</color> and <b>{b}</b> <color=#4A90D9>Barrier</color>.";
        }
    }
}