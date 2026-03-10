using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Effects/Add Card To Hand")]
    public class AddCardToHandEffect : CardEffect
    {
        [Tooltip("The card to add. Leave null to use a random card from the class pool.")]
        public CardData cardToAdd;

        public override void Execute(CombatContext ctx)
        {
            if (cardToAdd == null)
            {
                // Signal to CombatManager to generate a contextual card
                CombatEvents.RequestAddRandomCardToHand?.Invoke();
                return;
            }

            var instance = new CardInstance(cardToAdd);
            ctx.DeckManager.AddToHand(instance);
        }

        public override string GetDescription()
        {
            if (cardToAdd != null)
                return $"Add a <b>{cardToAdd.cardName}</b> to your hand.";
            return "Add a random card to your hand.";
        }
    }
}