using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Exhaust Card")]
    public class ExhaustCardFromHandEffect : CardEffect
    {
        [Tooltip("If true, player chooses which card. If false, a random card is exhausted.")]
        public bool playerChooses = true;

        public override void Execute(CombatContext ctx)
        {
            if (ctx.DeckManager.HandCount == 0) return;

            if (playerChooses)
            {
                CombatEvents.RequestExhaustSelection?.Invoke(1);
            }
            else
            {
                var hand = new System.Collections.Generic.List<CardInstance>(ctx.DeckManager.Hand);
                int idx = Random.Range(0, hand.Count);
                ctx.DeckManager.ExhaustCard(hand[idx]);
            }
        }

        public override string GetDescription()
        {
            return playerChooses
                ? "Choose a card in your hand to <b>Exhaust</b>."
                : "Exhaust a random card in your hand.";
        }
    }
}