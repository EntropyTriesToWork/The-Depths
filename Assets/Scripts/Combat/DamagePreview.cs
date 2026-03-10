// ============================================================
//  DamagePreview.cs
//  Calculates estimated damage for a card without executing it.
//  Used by UI to show damage numbers on card hover/selection.
//
//  Usage:
//    var preview = DamagePreview.Calculate(card, ctx, target);
//    Debug.Log($"Estimated damage: {preview.TotalDamage}");
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public static class DamagePreview
    {
        // ----------------------------------------------------------
        // Result container
        // ----------------------------------------------------------

        public class PreviewResult
        {
            /// <summary>Total damage after all modifiers, before target defense.</summary>
            public int TotalDamage   { get; set; }

            /// <summary>Number of hits (multi-hit cards).</summary>
            public int HitCount      { get; set; } = 1;

            /// <summary>Damage per individual hit.</summary>
            public int DamagePerHit  => HitCount > 0 ? TotalDamage / HitCount : 0;

            /// <summary>True if Strength/Weak/Vulnerable changed the raw value.</summary>
            public bool IsModified   { get; set; }

            /// <summary>True if damage scales with something variable (X-cost etc.).</summary>
            public bool IsVariable   { get; set; }

            /// <summary>The damage type (for tooltip colouring).</summary>
            public DamageType DamageType { get; set; }

            /// <summary>Block/Armor/Barrier on target — lets UI show overkill.</summary>
            public int TargetBlock   { get; set; }
            public int TargetArmor   { get; set; }
            public int TargetBarrier { get; set; }

            public static PreviewResult None => new PreviewResult { TotalDamage = 0 };
        }

        // ----------------------------------------------------------
        // Main entry point
        // ----------------------------------------------------------

        /// <summary>
        /// Returns a damage estimate for the given card against the given target.
        /// Pass null for target to use default (first living enemy).
        /// Returns PreviewResult.None if the card deals no damage.
        /// </summary>
        public static PreviewResult Calculate(
            CardInstance  card,
            CombatContext ctx,
            CombatEntity  target = null)
        {
            if (card == null || ctx == null) return PreviewResult.None;

            target ??= GetDefaultTarget(ctx);

            // Find the first DealDamageEffect on this card
            DealDamageEffect dmgEffect = null;
            foreach (var effect in card.Data.GetOnPlayEffects())
            {
                if (effect is DealDamageEffect d) { dmgEffect = d; break; }
            }

            if (dmgEffect == null) return PreviewResult.None;

            // --- Base magnitude ---
            int baseDmg = EstimateMagnitude(dmgEffect, ctx);

            // --- Attacker modifiers ---
            int strength  = ctx.Player.GetStatusStacks(StatusType.Strength);
            bool isWeak   = ctx.Player.HasStatus(StatusType.Weak);
            bool isModified = strength != 0 || isWeak;

            int dmg = baseDmg + strength;
            if (isWeak) dmg = Mathf.FloorToInt(dmg * 0.75f);
            dmg = Mathf.Max(0, dmg);

            // --- Hit count (some damage effects have a hitCount field) ---
            int hitCount = 1;
            // DealDamageEffect doesn't currently expose hitCount directly —
            // enemies do. For player cards, hitCount=1 unless you add it.
            // Wire this up if you add multi-hit player cards later.

            var result = new PreviewResult
            {
                TotalDamage  = dmg * hitCount,
                HitCount     = hitCount,
                IsModified   = isModified,
                IsVariable   = dmgEffect.scalingMode != ScalingMode.Flat,
                DamageType   = dmgEffect.damageType
            };

            // --- Target defense snapshot (for UI display only, not deducted) ---
            if (target != null)
            {
                result.TargetBlock   = target.CurrentBlock;
                result.TargetArmor   = target.CurrentArmor;
                result.TargetBarrier = target.CurrentBarrier;
            }

            return result;
        }

        // ----------------------------------------------------------
        // Block/defense preview (for Defend-type cards)
        // ----------------------------------------------------------

        /// <summary>
        /// Returns how much Block/Armor/Barrier a card will grant,
        /// including Dexterity and Frail modifiers.
        /// </summary>
        public static (int block, int armor, int barrier) CalculateDefenseGain(
            CardInstance  card,
            CombatContext ctx)
        {
            int block = 0, armor = 0, barrier = 0;

            foreach (var effect in card.Data.GetOnPlayEffects())
            {
                int amount = EstimateMagnitude(effect as CardEffect, ctx);

                // Apply Frail
                if (ctx.Player.HasStatus(StatusType.Frail))
                    amount = Mathf.FloorToInt(amount * 0.75f);

                // Apply Dexterity
                amount += ctx.Player.GetStatusStacks(StatusType.Dexterity);
                amount  = Mathf.Max(0, amount);

                if (effect is GainBlockEffect)   block   += amount;
                if (effect is GainArmorEffect)   armor   += amount;
                if (effect is GainBarrierEffect) barrier += amount;
            }

            return (block, armor, barrier);
        }

        // ----------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------

        private static int EstimateMagnitude(CardEffect effect, CombatContext ctx)
        {
            if (effect == null) return 0;

            switch (effect.scalingMode)
            {
                case ScalingMode.Flat:
                    return effect.baseMagnitude;

                case ScalingMode.EnergySpent:
                    // Estimate: assume player spends ALL remaining energy on this card
                    int energyAfterPlay = Mathf.Max(0, ctx.CurrentEnergy - (effect.baseMagnitude));
                    int spent = ctx.MaxEnergy - energyAfterPlay;
                    return Mathf.RoundToInt(effect.baseMagnitude * spent * effect.scalingMultiplier);

                case ScalingMode.CardsInHand:
                    return Mathf.RoundToInt(
                        effect.baseMagnitude * ctx.DeckManager.HandCount * effect.scalingMultiplier);

                case ScalingMode.EnemyCount:
                    return Mathf.RoundToInt(
                        effect.baseMagnitude * ctx.Enemies.Count * effect.scalingMultiplier);

                case ScalingMode.CurrentHP:
                    return Mathf.RoundToInt(
                        effect.baseMagnitude * ctx.Player.CurrentHealth * effect.scalingMultiplier);

                default:
                    return effect.baseMagnitude;
            }
        }

        private static CombatEntity GetDefaultTarget(CombatContext ctx)
        {
            foreach (var e in ctx.Enemies)
                if (!e.IsDead) return e;
            return null;
        }
    }
}
