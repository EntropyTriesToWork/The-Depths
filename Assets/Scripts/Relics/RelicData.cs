// ============================================================
//  RelicData.cs
//  Abstract ScriptableObject base for all relics.
//
//  A relic is a passive item the player holds for the entire run.
//  It does its work by subscribing to CombatEvents and GameEvents
//  when activated — it never holds a direct reference to any
//  manager or system.
//
//  To create a new relic:
//    1. Subclass RelicData
//    2. Override OnEquip() to subscribe to events
//    3. Override OnUnequip() to unsubscribe (prevents memory leaks)
//    4. Add [CreateAssetMenu] to your subclass
//    5. Create the asset in the editor
// ============================================================

using UnityEngine;

namespace CardGame
{
    public abstract class RelicData : ScriptableObject
    {
        // ----------------------------------------------------------
        // Identity
        // ----------------------------------------------------------

        [Header("Identity")]
        public string relicName    = "Unknown Relic";
        public string relicID      = "";
        public string description  = "";
        public RelicRarity rarity  = RelicRarity.Common;

        [Header("Visuals")]
        public Sprite icon;

        [Header("Unlock")]
        public string unlockID = "";
        public bool IsAlwaysUnlocked => string.IsNullOrEmpty(unlockID);

        [Header("Source")]
        [Tooltip("How this relic can be obtained.")]
        public RelicSource source = RelicSource.Shop;

        // ----------------------------------------------------------
        // Activation state (managed by RelicManager)
        // ----------------------------------------------------------

        // Note: RelicData is a ScriptableObject shared asset.
        // State during a run lives on RelicInstance, not here.

        // ----------------------------------------------------------
        // Core interface — override in subclasses
        // ----------------------------------------------------------

        /// <summary>
        /// Called by RelicManager when this relic is equipped.
        /// Subscribe to all events here.
        /// The RelicContext gives you access to what you need.
        /// </summary>
        public abstract void OnEquip(RelicContext ctx);

        /// <summary>
        /// Called when the relic is removed or the run ends.
        /// ALWAYS unsubscribe every event you subscribed in OnEquip.
        /// </summary>
        public abstract void OnUnequip(RelicContext ctx);

        /// <summary>
        /// Returns the current description (may include dynamic values).
        /// Override for relics that track counters.
        /// </summary>
        public virtual string GetDescription(RelicInstance instance) => description;

        // ----------------------------------------------------------
        // Editor validation
        // ----------------------------------------------------------

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(relicID) && !string.IsNullOrEmpty(relicName))
            {
                relicID = relicName.ToLower().Replace(" ", "_");
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }

    // ----------------------------------------------------------
    //  RelicContext — what relics get access to when equipped
    //  (avoids relics needing direct refs to singletons)
    // ----------------------------------------------------------

    public class RelicContext
    {
        public CombatContext     Combat     { get; set; }   // May be null outside combat
        public RelicInstance     Instance   { get; set; }   // The live instance of this relic

        // Mutation callbacks (relics request changes through these)
        public System.Action<int>   AddGold       { get; set; }
        public System.Action<int>   AddEssence    { get; set; }
        public System.Action<int>   AddSouls      { get; set; }
        public System.Action<int>   ModifyMaxHP   { get; set; }
        public System.Action<int>   DrawCards     { get; set; }
        public System.Action<int>   GainEnergy    { get; set; }
    }

    // ----------------------------------------------------------
    //  RelicInstance — runtime wrapper around RelicData
    //  Tracks per-run state (counters, charges, etc.)
    // ----------------------------------------------------------

    [System.Serializable]
    public class RelicInstance
    {
        public RelicData Data      { get; private set; }
        public int       Counter   { get; set; }    // General-purpose counter for relics that track things
        public bool      IsActive  { get; set; } = true;

        public RelicInstance(RelicData data)
        {
            Data    = data;
            Counter = 0;
        }
    }

    // ----------------------------------------------------------
    //  Supporting enums
    // ----------------------------------------------------------

    public enum RelicRarity
    {
        Common,
        Uncommon,
        Rare,
        Boss,       // Only drops from bosses
        Special     // Story/unlock relics
    }

    public enum RelicSource
    {
        Shop,
        EliteDrop,
        BossDrop,
        EventReward,
        StarterRelic,   // Given at run start based on character
        Unlockable      // Requires achievement to appear in pool
    }
}
