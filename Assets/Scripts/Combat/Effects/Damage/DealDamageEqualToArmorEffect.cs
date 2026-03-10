using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Damage Equal To Armor")]
    public class DamageEqualToArmorEffect : CardEffect
    {
        public DamageType damageType = DamageType.Physical;

        public override void Execute(CombatContext ctx)
        {
            int damage = ctx.Player.CurrentArmor;
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
            "Deal damage equal to your current <color=#E8A020>Armor</color>.";
    }
}
