using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Strip Defense")]
    public class StripDefenseEffect : CardEffect
    {
        [Tooltip("Which defense pool to strip.")]
        public DefenseLayer layerToStrip = DefenseLayer.Armor;

        public override void Execute(CombatContext ctx)
        {
            int amount = GetScaledMagnitude(ctx);
            var targets = ctx.ResolveTargets(target);

            foreach (var t in targets)
            {
                switch (layerToStrip)
                {
                    case DefenseLayer.Block:
                        // Block is public — we can strip it by dealing true damage
                        // equal to min(amount, currentBlock) then clearing the rest manually
                        // Simplest clean approach: just zero out block directly via TakeDamage bypass
                        // For now, use a targeted clear (no HP damage)
                        int blockStrip = Mathf.Min(amount, t.CurrentBlock);
                        // Block doesn't have a direct setter — deal True damage to deplete it
                        // Since True goes to Health, we have to route carefully.
                        // Best: expose a StripBlock helper on CombatEntity
                        t.StripBlock(blockStrip);
                        break;

                    case DefenseLayer.Barrier:
                        t.ClearBarrier(amount);
                        break;

                    case DefenseLayer.Armor:
                        t.StripArmor(amount);
                        break;
                }
            }
        }

        public override string GetDescription()
        {
            string layer = layerToStrip switch
            {
                DefenseLayer.Block => "Block",
                DefenseLayer.Armor => "<color=#E8A020>Armor</color>",
                DefenseLayer.Barrier => "<color=#4A90D9>Barrier</color>",
                _ => "Defense"
            };
            return $"Remove <b>{baseMagnitude}</b> enemy {layer}.";
        }
    }
}