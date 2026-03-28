using UnityEngine;

[CreateAssetMenu(menuName = "Effects/Block", fileName = "BlockEffect")]
public class BlockEffect : Effect
{
    public override void Apply(EffectContext context, EffectParameters parameters)
    {
        foreach (Entity target in context.targets)
        {
            int amount = parameters.intValue;

            var health = target.Health;
            if (health != null)
                amount += health.GetStatusStacks(StatusType.Dexterity); // dexterity adds flat block

            target.Health.AddBlock(amount);
        }
    }
}
