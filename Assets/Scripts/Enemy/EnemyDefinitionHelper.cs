// ============================================================
//  EnemyDefinitionHelper.cs
//
//  Everything needed to define and instantiate enemies in code.
//  No ScriptableObjects in authoring — only created internally
//  when converting to EnemyInstance for combat.
//
//  CONTAINS:
//    EnemyDefinition        — base class for all enemy definitions
//    EnemyActionDefinition  — single action/intent entry
//    EnemyDefinitionHelper  — converts definitions → EnemyData/EnemyAction SOs
//                             and builds ready-to-use EnemyInstances
//
//  USAGE:
//    EnemyData     data = EnemyDefinitionHelper.ToData(new SlimeEnemy());
//    EnemyInstance inst = EnemyDefinitionHelper.ToInstance(new SlimeEnemy(), defenseConfig);
//    List<EnemyData>    = EnemyDefinitionHelper.ToDataList(ConcreteEnemies.All());
//    List<EnemyData>    = EnemyDefinitionHelper.BuildEncounter(pool, count: 2);
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    // ============================================================
    //  DATA TYPES
    // ============================================================

    /// <summary>
    /// Plain C# base class for all enemy definitions.
    /// Replaces EnemyData ScriptableObject for authoring purposes.
    /// </summary>
    public class EnemyDefinition
    {
        public string                      EnemyID   { get; protected set; }
        public string                      EnemyName { get; protected set; }
        public int                         BaseHP    { get; protected set; }
        public bool                        IsElite   { get; protected set; } = false;
        public bool                        IsBoss    { get; protected set; } = false;
        public List<EnemyActionDefinition> Actions   { get; protected set; } = new();
    }

    /// <summary>
    /// Plain C# definition of a single enemy action/intent.
    /// Replaces EnemyAction ScriptableObject for authoring purposes.
    /// </summary>
    public class EnemyActionDefinition
    {
        public string      ActionName  { get; set; }
        public EnemyIntent IntentType  { get; set; }

        // Attack
        public int         Damage      { get; set; }
        public DamageType  DamageType  { get; set; } = DamageType.Physical;
        public int         HitCount    { get; set; } = 1;

        // Defend
        public int         SelfBlock   { get; set; }

        // Status applied to player
        public StatusType  ApplyStatus  { get; set; }
        public int         StatusStacks { get; set; }

        // Status applied to self (buff)
        public StatusType  ApplySelfStatus  { get; set; }
        public int         SelfStatusStacks { get; set; }

        /// <summary>
        /// If non-empty, shown as-is in the intent display instead of
        /// the auto-generated description. Use when inference isn't enough.
        /// Example: "Ritual Ward  +12 Block and +2 Strength"
        /// </summary>
        public string      CustomIntentDescription { get; set; } = "";
    }

    // ============================================================
    //  HELPER — converts definitions to runtime objects
    // ============================================================

    public static class EnemyDefinitionHelper
    {
        // ----------------------------------------------------------
        //  EnemyDefinition → EnemyData SO (used by FloorConfig pools)
        // ----------------------------------------------------------

        public static EnemyData ToData(EnemyDefinition def)
        {
            var data       = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = def.EnemyName;
            data.enemyID   = def.EnemyID;
            data.baseHP    = def.BaseHP;
            data.isElite   = def.IsElite;
            data.isBoss    = def.IsBoss;

            var actions = new List<EnemyAction>();
            foreach (var a in def.Actions)
            {
                var action                     = ScriptableObject.CreateInstance<EnemyAction>();
                action.actionName              = a.ActionName;
                action.intentType              = a.IntentType;
                action.customIntentDescription = a.CustomIntentDescription;
                action.damage                  = a.Damage;
                action.damageType              = a.DamageType;
                action.hitCount                = Mathf.Max(1, a.HitCount);
                action.selfBlock               = a.SelfBlock;
                action.applyStatus             = a.ApplyStatus;
                action.statusStacks            = a.StatusStacks;
                action.appliesStatus           = a.StatusStacks > 0;
                action.applySelfStatus         = a.ApplySelfStatus;
                action.selfStatusStacks        = a.SelfStatusStacks;
                action.appliesSelfStatus       = a.SelfStatusStacks > 0;
                actions.Add(action);
            }

            data.actionPattern = actions.ToArray();
            return data;
        }

        // ----------------------------------------------------------
        //  EnemyDefinition → EnemyInstance (ready for combat)
        // ----------------------------------------------------------

        public static EnemyInstance ToInstance(EnemyDefinition def, DefenseConfig defenseConfig)
        {
            return new EnemyInstance(ToData(def), defenseConfig);
        }

        // ----------------------------------------------------------
        //  List<EnemyDefinition> → List<EnemyData>
        // ----------------------------------------------------------

        public static List<EnemyData> ToDataList(List<EnemyDefinition> defs)
        {
            var list = new List<EnemyData>();
            foreach (var def in defs)
                list.Add(ToData(def));
            return list;
        }

        // ----------------------------------------------------------
        //  Pick one random EnemyData from a pool of definitions
        // ----------------------------------------------------------

        public static EnemyData PickRandom(List<EnemyDefinition> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return ToData(pool[Random.Range(0, pool.Count)]);
        }

        // ----------------------------------------------------------
        //  Build a combat encounter — picks `count` unique enemies
        //  from the pool with no duplicates within the same encounter
        // ----------------------------------------------------------

        public static List<EnemyData> BuildEncounter(List<EnemyDefinition> pool, int count = 1)
        {
            var encounter = new List<EnemyData>();
            var available = new List<EnemyDefinition>(pool);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                encounter.Add(ToData(available[idx]));
                available.RemoveAt(idx);
            }

            return encounter;
        }
    }
}
