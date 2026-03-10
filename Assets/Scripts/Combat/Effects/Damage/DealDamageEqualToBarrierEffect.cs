using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Damage Equal To Barrier")]
    public class DamageEqualToBarrierEffect : CardEffect
    {
        public DamageType damageType = DamageType.Magic;

        public override void Execute(CombatContext ctx)
        {
            int damage = ctx.Player.CurrentBarrier;
            //damage += ctx.Player.GetStatusStacks(StatusType.Strength);
            //if (ctx.Player.HasStatus(StatusType.Weak))
            //    damage = Mathf.FloorToInt(damage * 0.75f);

            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                var bd = t.TakeDamage(damage, damageType);
                ctx.TrackDamageDealt(bd.TotalDamage);
            }
        }

        public override string GetDescription() =>
            "Deal damage equal to your current <color=#4A90D9>Barrier</color>.";
    }
}
