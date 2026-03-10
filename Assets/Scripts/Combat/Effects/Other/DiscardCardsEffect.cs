using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Discard Cards")]
    public class DiscardCardsEffect : CardEffect
    {
        [Tooltip("If true, player chooses which cards to discard. If false, random.")]
        public bool playerChooses = false;

        public override void Execute(CombatContext ctx)
        {
            int count = Mathf.Min(baseMagnitude, ctx.DeckManager.HandCount);
            if (count <= 0) return;

            if (playerChooses)
            {
                // Signal to UI that a discard selection is needed.
                // The actual discard is handled by CombatManager after player picks.
                // We use a lightweight event pattern here.
                CombatEvents.RequestDiscardSelection?.Invoke(count);
            }
            else
            {
                // Discard random cards
                var hand = new System.Collections.Generic.List<CardInstance>(ctx.DeckManager.Hand);
                for (int i = 0; i < count && hand.Count > 0; i++)
                {
                    int idx = Random.Range(0, hand.Count);
                    ctx.DeckManager.DiscardCard(hand[idx]);
                    hand.RemoveAt(idx);
                }
            }
        }

        public override string GetDescription()
        {
            string how = playerChooses ? "Choose" : "Discard";
            string plural = baseMagnitude == 1 ? "card" : "cards";
            return $"{how} and discard <b>{baseMagnitude}</b> {plural}.";
        }
    }
}