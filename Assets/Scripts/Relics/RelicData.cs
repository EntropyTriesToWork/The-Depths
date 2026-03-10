using UnityEngine;

namespace CardGame
{
    public abstract class RelicData : ScriptableObject
    {
        [Header("Identity")]
        public abstract string RelicName { get; }
        public abstract string RelicID { get; }
        public abstract string Description { get;}
        public abstract RelicRarity Rarity { get; }

        [Header("Unlock")]
        public abstract string unlockID { get; }
        public bool IsAlwaysUnlocked => string.IsNullOrEmpty(unlockID);

        [Header("Source")]
        [Tooltip("How this relic can be obtained.")]
        public abstract RelicSource Source {  get; }

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
        public virtual string GetDescription(RelicInstance instance) => Description;
    }
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
        Chest      // Requires achievement to appear in pool
    }
}
