using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Remove Status")]
    public class RemoveStatusEffect : CardEffect
    {
        [Tooltip("Leave blank to remove ALL debuffs.")]
        public StatusType[] statusTypesToRemove;

        public override void Execute(CombatContext ctx)
        {
            var targets = ctx.ResolveTargets(target);
            foreach (var t in targets)
            {
                if (statusTypesToRemove == null || statusTypesToRemove.Length == 0)
                {
                    // Remove all debuffs
                    t.RemoveStatus(StatusType.Weak);
                    t.RemoveStatus(StatusType.Exposed);
                    t.RemoveStatus(StatusType.Frail);
                    t.RemoveStatus(StatusType.Poison);
                    t.RemoveStatus(StatusType.Burn);
                    t.RemoveStatus(StatusType.Shackled);
                }
                else
                {
                    foreach (var s in statusTypesToRemove)
                        t.RemoveStatus(s);
                }
            }
        }

        public override string GetDescription()
        {
            if (statusTypesToRemove == null || statusTypesToRemove.Length == 0)
                return "Remove all <b>debuffs</b>.";
            return $"Remove <b>{string.Join(", ", statusTypesToRemove)}</b>.";
        }
    }
}