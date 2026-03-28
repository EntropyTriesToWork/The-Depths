using UnityEngine;

[CreateAssetMenu(menuName = "Effects/ApplyStatus", fileName = "ApplyStatusEffect")]
public class ApplyStatusEffect : Effect
{
    public override void Apply(EffectContext context, EffectParameters parameters)
    {
        foreach (Entity target in context.targets)
            target.Health.ApplyStatus(parameters.statusType, parameters.intValue);
    }
}
