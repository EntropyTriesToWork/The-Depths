// ============================================================
//  RelicManager.cs
//  Manages the player's active relics for the duration of a run.
//
//  Responsibilities:
//    • Holds the list of equipped RelicInstances
//    • Calls OnEquip/OnUnequip with a RelicContext
//    • Updates RelicContext.Combat when a new combat starts
//    • Provides the shop/reward pool of available relics
//
//  Attach to a persistent run-level GameObject (not the combat scene).
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public class RelicManager : MonoBehaviour
    {
        // ----------------------------------------------------------
        // Active relics
        // ----------------------------------------------------------

        private List<RelicInstance> _equippedRelics = new List<RelicInstance>();
        public IReadOnlyList<RelicInstance> EquippedRelics => _equippedRelics;

        // ----------------------------------------------------------
        // Available pool (all relic assets loaded, filtered by unlock)
        // ----------------------------------------------------------

        [Header("All Relic Assets")]
        [Tooltip("Drag all RelicData assets here. RelicManager filters by unlock state.")]
        public RelicData[] allRelics;

        // ----------------------------------------------------------
        // Context (shared, updated each combat)
        // ----------------------------------------------------------

        private RelicContext _relicContext;

        // ----------------------------------------------------------
        // Run references (set by RunManager)
        // ----------------------------------------------------------

        private System.Action<int> _addGoldCallback;
        private System.Action<int> _addEssenceCallback;
        private System.Action<int> _addSoulsCallback;
        private System.Action<int> _modifyMaxHPCallback;

        // ----------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------

        /// <summary>
        /// Call at the start of a run to set up the relic context callbacks.
        /// </summary>
        public void InitializeRun(
            System.Action<int> addGold,
            System.Action<int> addEssence,
            System.Action<int> addSouls,
            System.Action<int> modifyMaxHP,
            System.Action<int> drawCards,
            System.Action<int> gainEnergy)
        {
            _addGoldCallback    = addGold;
            _addEssenceCallback = addEssence;
            _addSoulsCallback   = addSouls;
            _modifyMaxHPCallback = modifyMaxHP;

            _relicContext = new RelicContext
            {
                AddGold      = addGold,
                AddEssence   = addEssence,
                AddSouls     = addSouls,
                ModifyMaxHP  = modifyMaxHP,
                DrawCards    = drawCards,
                GainEnergy   = gainEnergy
            };

            _equippedRelics.Clear();
        }

        // ----------------------------------------------------------
        // Equipping / Removing
        // ----------------------------------------------------------

        public void EquipRelic(RelicData data)
        {
            var instance = new RelicInstance(data);
            _equippedRelics.Add(instance);

            _relicContext.Instance = instance;
            data.OnEquip(_relicContext);

            GameEvents.OnRelicEquipped?.Invoke(instance);
        }

        public void UnequipRelic(RelicInstance instance)
        {
            if (!_equippedRelics.Contains(instance)) return;

            _relicContext.Instance = instance;
            instance.Data.OnUnequip(_relicContext);
            _equippedRelics.Remove(instance);
        }

        // ----------------------------------------------------------
        // Combat lifecycle hooks
        // ----------------------------------------------------------

        /// <summary>
        /// Called by CombatManager/RunManager when combat begins.
        /// Updates all relics' context so they can fire combat events.
        /// </summary>
        public void OnCombatStart(CombatContext combatContext)
        {
            _relicContext.Combat = combatContext;

            // Re-equip combat-specific subscriptions
            foreach (var instance in _equippedRelics)
            {
                _relicContext.Instance = instance;
                instance.Data.OnUnequip(_relicContext);     // Unsubscribe stale
                instance.Data.OnEquip(_relicContext);       // Re-subscribe fresh
            }
        }

        /// <summary>
        /// Called when combat ends. Nulls out the combat reference.
        /// </summary>
        public void OnCombatEnd()
        {
            _relicContext.Combat = null;
        }

        /// <summary>
        /// Called at full run end. Cleans up all relics.
        /// </summary>
        public void OnRunEnd()
        {
            foreach (var instance in _equippedRelics)
            {
                _relicContext.Instance = instance;
                instance.Data.OnUnequip(_relicContext);
            }
            _equippedRelics.Clear();
        }

        // ----------------------------------------------------------
        // Pool / Shop helpers
        // ----------------------------------------------------------

        public bool HasRelic(string relicID)
        {
            foreach (var r in _equippedRelics)
                if (r.Data.RelicID == relicID) return true;
            return false;
        }

        public RelicInstance GetRelic(string relicID)
        {
            foreach (var r in _equippedRelics)
                if (r.Data.RelicID == relicID) return r;
            return null;
        }

        /// <summary>
        /// Returns relics available in the shop/reward pool,
        /// filtered by unlock state and not already owned.
        /// </summary>
        public List<RelicData> GetAvailableRelics(HashSet<string> unlockedIDs, RelicRarity? rarityFilter = null)
        {
            var result = new List<RelicData>();
            foreach (var relic in allRelics)
            {
                if (relic == null) continue;
                if (HasRelic(relic.RelicID)) continue;
                if (!relic.IsAlwaysUnlocked && !unlockedIDs.Contains(relic.unlockID)) continue;
                if (rarityFilter.HasValue && relic.Rarity != rarityFilter.Value) continue;
                result.Add(relic);
            }
            return result;
        }
    }
}
