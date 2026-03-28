using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Attack", fileName = "AttackEffect")]
public class AttackEffect : Effect
{
    public override void Apply(EffectContext context, EffectParameters parameters)
    {
        foreach (Entity target in context.targets)
        {
            int finalDamage = DamageCalculator.Calculate(parameters.intValue, context.source, target);
            target.Health.TakeDamage(finalDamage, context.source);
        }
    }
}
