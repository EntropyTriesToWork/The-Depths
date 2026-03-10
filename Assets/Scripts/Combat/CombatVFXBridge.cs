// ============================================================
//  CombatVFXBridge.cs
//  Two lightweight bridge classes for the optional additions:
//
//    StatusVFXBridge  — Translates status events into VFX requests
//    IntentDisplay    — Packages enemy intent data for UI rendering
//
//  These sit between the logic layer and the visual layer.
//  UI/VFX scripts subscribe to these bridges, NOT to CombatEntity
//  directly. This keeps the separation clean.
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    // ==========================================================
    //  STATUS VFX BRIDGE
    //  Subscribe to entity status events, translate to VFX calls.
    //
    //  Usage:
    //    var bridge = new StatusVFXBridge();
    //    bridge.Register(playerEntity);
    //    bridge.OnVFXRequested += myParticleSystem.PlayEffect;
    // ==========================================================

    public class StatusVFXBridge
    {
        // ----------------------------------------------------------
        // VFX request event — UI/VFX layer subscribes to this
        // ----------------------------------------------------------

        public event Action<StatusVFXRequest> OnVFXRequested;
        public event Action<DamageVFXRequest> OnDamageVFXRequested;

        // ----------------------------------------------------------
        // Registered entities (we watch all of them)
        // ----------------------------------------------------------

        private List<CombatEntity> _watchedEntities = new List<CombatEntity>();

        // ----------------------------------------------------------
        // Registration
        // ----------------------------------------------------------

        public void Register(CombatEntity entity)
        {
            if (_watchedEntities.Contains(entity)) return;
            _watchedEntities.Add(entity);

            entity.OnStatusApplied += (type, stacks) =>
                OnVFXRequested?.Invoke(new StatusVFXRequest(entity, type, stacks, StatusVFXType.Applied));

            entity.OnStatusRemoved += (type) =>
                OnVFXRequested?.Invoke(new StatusVFXRequest(entity, type, 0, StatusVFXType.Removed));

            entity.OnDamageTaken += (breakdown) =>
                OnDamageVFXRequested?.Invoke(new DamageVFXRequest(entity, breakdown));

            entity.OnHealed += (amount) =>
                OnVFXRequested?.Invoke(new StatusVFXRequest(entity, StatusType.Regeneration, amount, StatusVFXType.Heal));

            entity.OnDefenseGained += (amount, layer) =>
                OnDamageVFXRequested?.Invoke(new DamageVFXRequest(entity, amount, layer));
        }

        public void UnregisterAll()
        {
            // Entities handle their own event cleanup on death/end
            _watchedEntities.Clear();
        }
    }

    // ----------------------------------------------------------
    //  VFX Request data containers
    // ----------------------------------------------------------

    public class StatusVFXRequest
    {
        public CombatEntity Entity    { get; }
        public StatusType   Status    { get; }
        public int          Stacks    { get; }
        public StatusVFXType VFXType  { get; }

        public StatusVFXRequest(CombatEntity entity, StatusType status, int stacks, StatusVFXType type)
        {
            Entity  = entity;
            Status  = status;
            Stacks  = stacks;
            VFXType = type;
        }
    }

    public class DamageVFXRequest
    {
        public CombatEntity    Entity         { get; }
        public DamageReport Breakdown      { get; }
        public int             DefenseAmount  { get; }
        public DefenseLayer    DefenseLayer   { get; }
        public bool            IsDefenseGain  { get; }

        // Damage constructor
        public DamageVFXRequest(CombatEntity entity, DamageReport breakdown)
        {
            Entity        = entity;
            Breakdown     = breakdown;
            IsDefenseGain = false;
        }

        // Defense gain constructor
        public DamageVFXRequest(CombatEntity entity, int amount, DefenseLayer layer)
        {
            Entity        = entity;
            DefenseAmount = amount;
            DefenseLayer  = layer;
            IsDefenseGain = true;
        }
    }

    public enum StatusVFXType
    {
        Applied,
        Removed,
        Tick,       // Damage from status (poison/burn) processing
        Heal
    }


    // ==========================================================
    //  INTENT DISPLAY SYSTEM
    //  Converts EnemyInstance intent into display-ready data.
    //  UI reads IntentDisplayData — never the raw EnemyInstance.
    // ==========================================================

    public static class IntentDisplaySystem
    {
        /// <summary>
        /// Builds display data for a list of enemies.
        /// Call at the start of each player turn after SelectNextIntent().
        /// </summary>
        public static List<IntentDisplayData> BuildIntentDisplay(List<EnemyInstance> enemies)
        {
            var result = new List<IntentDisplayData>();
            foreach (var enemy in enemies)
            {
                if (enemy.IsDead) continue;
                result.Add(BuildSingle(enemy));
            }
            return result;
        }

        public static IntentDisplayData BuildSingle(EnemyInstance enemy)
        {
            var action = enemy.CurrentIntent;
            if (action == null)
                return new IntentDisplayData(enemy.Data.enemyName, EnemyIntent.Unknown, "???", 0, 0);

            return new IntentDisplayData(
                enemyName:       enemy.Data.enemyName,
                intentType:      action.intentType,
                description:     enemy.GetIntentDescription(),
                totalDamage:     action.TotalDamage,
                hitCount:        action.hitCount,
                statusToApply:   action.appliesStatus ? action.applyStatus : (StatusType?)null,
                statusStacks:    action.statusStacks,
                selfBlock:       action.selfBlock
            );
        }
    }

    public class IntentDisplayData
    {
        public string         EnemyName       { get; }
        public EnemyIntent    IntentType      { get; }
        public string         Description     { get; }
        public int            TotalDamage     { get; }
        public int            HitCount        { get; }
        public StatusType?    StatusToApply   { get; }
        public int            StatusStacks    { get; }
        public int            SelfBlock       { get; }

        // Derived helpers for UI
        public bool IsAttack     => IntentType == EnemyIntent.Attack
                                 || IntentType == EnemyIntent.AttackAndDebuff
                                 || IntentType == EnemyIntent.AttackAndBuff;
        public bool IsMultiHit   => HitCount > 1;
        public string HitPattern => IsMultiHit ? $"{HitCount}×{TotalDamage / HitCount}" : $"{TotalDamage}";

        public IntentDisplayData(
            string enemyName,
            EnemyIntent intentType,
            string description,
            int totalDamage,
            int hitCount,
            StatusType? statusToApply = null,
            int statusStacks = 0,
            int selfBlock = 0)
        {
            EnemyName     = enemyName;
            IntentType    = intentType;
            Description   = description;
            TotalDamage   = totalDamage;
            HitCount      = Mathf.Max(1, hitCount);
            StatusToApply = statusToApply;
            StatusStacks  = statusStacks;
            SelfBlock     = selfBlock;
        }
    }
}
