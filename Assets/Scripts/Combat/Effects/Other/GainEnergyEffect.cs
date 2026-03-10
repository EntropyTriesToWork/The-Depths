using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Gain Energy")]
    public class GainEnergyEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            ctx.GainEnergy(baseMagnitude);
        }

        public override string GetDescription()
        {
            return $"Gain <b>{baseMagnitude}</b> Energy.";
        }
    }
}