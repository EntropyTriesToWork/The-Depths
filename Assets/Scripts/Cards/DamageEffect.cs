public class DamageEffect : Effect
{
    public override void Apply(EffectContext context, EffectParameters parameters)
    {
        foreach (var target in context.targets)
        {
            var health = target.GetComponent<HealthComponent>();
            if (health != null)
            {
                // Let the HealthComponent handle the actual damage application
                health.TakeDamage(parameters.intValue, context.source);
            }
        }
    }
}