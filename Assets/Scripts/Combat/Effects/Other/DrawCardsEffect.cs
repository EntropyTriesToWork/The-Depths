using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Draw Cards")]
    public class DrawCardsEffect : CardEffect
    {
        public override void Execute(CombatContext ctx)
        {
            int count = GetScaledMagnitude(ctx);
            ctx.DeckManager.DrawCards(count);
        }

        public override string GetDescription()
        {
            string plural = baseMagnitude == 1 ? "card" : "cards";
            return $"Draw <b>{baseMagnitude}</b> {plural}.";
        }
    }
}