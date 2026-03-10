// ============================================================
//  EventData.cs
//  ScriptableObject defining a text-based event with choices.
//  Create: Right-click → Create → CardGame → Event Data
// ============================================================

using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Run/Event Data", fileName = "NewEvent")]
    public class EventData : ScriptableObject
    {
        [Header("Identity")]
        public string eventName    = "Strange Encounter";
        public string eventID      = "";
        public string flavourText  = "You come across something unusual...";
        public Sprite illustration;

        [Header("Unlock")]
        [Tooltip("Leave empty for always-available events.")]
        public string unlockID = "";

        [Header("Choices")]
        public EventChoice[] choices;

        // ----------------------------------------------------------
        // Editor validation
        // ----------------------------------------------------------

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(eventID) && !string.IsNullOrEmpty(eventName))
            {
                eventID = eventName.ToLower().Replace(" ", "_");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    // ----------------------------------------------------------
    //  EventChoice — one branch option inside an event.
    //  Plain serializable class, not a ScriptableObject,
    //  because it only exists as part of an EventData asset.
    // ----------------------------------------------------------

    [System.Serializable]
    public class EventChoice
    {
        [Header("Text")]
        public string choiceText  = "Investigate";
        public string outcomeText = "You find something useful.";

        [Header("Costs (player must have these to select)")]
        public int  goldCost          = 0;
        public int  hpCost            = 0;
        public string relicRequirement = "";   // relicID required

        [Header("Rewards")]
        public int  goldReward        = 0;
        public int  hpChange          = 0;     // Positive = heal, negative = damage
        public int  essenceReward     = 0;
        public int  soulsReward       = 0;
        public bool gainRandomCard    = false;
        public bool gainRandomRelic   = false;

        [Header("Deck Manipulation")]
        public bool removeCard        = false; // Player removes a card
        public bool upgradeCard       = false; // Player upgrades a card for free
        public bool transformCard     = false; // Replace a card with a random one

        // ----------------------------------------------------------
        // Validation helpers (used by UI to grey out locked options)
        // ----------------------------------------------------------

        public bool CanSelect(RunState runState)
        {
            if (goldCost > 0 && runState.Gold < goldCost) return false;
            if (hpCost > 0 && runState.CurrentHP <= hpCost) return false;
            if (!string.IsNullOrEmpty(relicRequirement) && !runState.HasRelic(relicRequirement)) return false;
            return true;
        }
    }
}
