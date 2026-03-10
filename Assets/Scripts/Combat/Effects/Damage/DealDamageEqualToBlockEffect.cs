using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Damage Equal To Block")]
    public class DamageEqualToBlockEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            int damage = ctx.Player.CurrentBlock;
            //damage += ctx.Player.GetStatusStacks(StatusType.Strength);
            //if (ctx.Player.HasStatus(StatusType.Weak))
            //    damage = Mathf.FloorToInt(damage * 0.75f);

            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                var bd = t.TakeDamage(damage, DamageType.Physical);
                ctx.TrackDamageDealt(bd.TotalDamage);
            }
        }

        public override string GetDescription() =>
            "Deal damage equal to your current <b>Block</b>.";
    }
}