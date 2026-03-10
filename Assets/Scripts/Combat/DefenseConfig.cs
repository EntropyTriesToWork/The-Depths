// ============================================================
//  DefenseConfig.cs  [UPDATED — Block added as 4th defense layer]
//
//  CHANGES FROM PREVIOUS VERSION:
//    • DefenseLayer enum gains Block
//    • GetRoutingOrder() now puts Block first for ALL damage types
//    • Block has no resistances/weaknesses — flat 1x absorber
//    • Poison/Dark/True still bypass Block (they go straight to Health)
//      Toggle this with poisonBypassesBlock if you want a different feel.
// ============================================================

using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Defense Config", fileName = "DefenseConfig")]
    public class DefenseConfig : ScriptableObject
    {
        [Header("─── Physical Damage Multipliers ───────────────────")]
        public float physicalVsBlock = 1.0f;   // Block is neutral to all types
        public float physicalVsBarrier = 1.5f;
        public float physicalVsArmor = 0.5f;
        public float physicalVsHealth = 1.0f;

        [Header("─── Magic Damage Multipliers ──────────────────────")]
        public float magicVsBlock = 1.0f;
        public float magicVsArmor = 1.5f;
        public float magicVsBarrier = 0.5f;
        public float magicVsHealth = 1.0f;

        [Header("─── Fire Damage Multipliers ───────────────────────")]
        public float fireVsBlock = 1.0f;
        public float fireVsArmor = 1.25f;
        public float fireVsBarrier = 0.75f;
        public float fireVsHealth = 1.0f;

        [Header("─── DoT / Bypass Rules ─────────────────────────────")]
        [Tooltip("Poison bypasses Block and shields, going straight to Health.")]
        public bool poisonBypassesBlock = true;

        [Tooltip("Burn bypasses Block and shields, going straight to Health.")]
        public bool burnBypassesBlock = true;

        [Tooltip("Dark damage bypasses Block and shields.")]
        public bool darkBypassesBlock = true;

        // ----------------------------------------------------------
        // Multiplier lookup
        // ----------------------------------------------------------

        public float GetMultiplier(DamageType damageType, DefenseLayer layer)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    return layer == DefenseLayer.Block ? physicalVsBlock
                         : layer == DefenseLayer.Armor ? physicalVsArmor
                         : layer == DefenseLayer.Barrier ? physicalVsBarrier
                                                         : physicalVsHealth;

                case DamageType.Magic:
                    return layer == DefenseLayer.Block ? magicVsBlock
                         : layer == DefenseLayer.Armor ? magicVsArmor
                         : layer == DefenseLayer.Barrier ? magicVsBarrier
                                                         : magicVsHealth;

                case DamageType.Fire:
                    return layer == DefenseLayer.Block ? fireVsBlock
                         : layer == DefenseLayer.Armor ? fireVsArmor
                         : layer == DefenseLayer.Barrier ? fireVsBarrier
                                                         : fireVsHealth;

                default:
                    return 1.0f;
            }
        }

        // ----------------------------------------------------------
        // Routing order lookup
        // ----------------------------------------------------------

        public DefenseLayer[] GetRoutingOrder(DamageType damageType)
        {
            switch (damageType)
            {
                case DamageType.Physical:
                    // Block absorbs first → Barrier (weak to physical) → Armor (resists) → Health
                    return new[] { DefenseLayer.Block, DefenseLayer.Barrier, DefenseLayer.Armor, DefenseLayer.Health };

                case DamageType.Magic:
                    // Block absorbs first → Armor (weak to magic) → Barrier (resists) → Health
                    return new[] { DefenseLayer.Block, DefenseLayer.Armor, DefenseLayer.Barrier, DefenseLayer.Health };

                case DamageType.Fire:
                    return new[] { DefenseLayer.Block, DefenseLayer.Armor, DefenseLayer.Barrier, DefenseLayer.Health };

                case DamageType.Poison:
                    return poisonBypassesBlock
                        ? new[] { DefenseLayer.Health }
                        : new[] { DefenseLayer.Block, DefenseLayer.Health };

                case DamageType.Dark:
                    return darkBypassesBlock
                        ? new[] { DefenseLayer.Health }
                        : new[] { DefenseLayer.Block, DefenseLayer.Health };

                case DamageType.True:
                    return new[] { DefenseLayer.Health };

                default:
                    return new[] { DefenseLayer.Block, DefenseLayer.Barrier, DefenseLayer.Armor, DefenseLayer.Health };
            }
        }
    }

    // ----------------------------------------------------------
    //  DefenseLayer — now 4 layers
    // ----------------------------------------------------------

    public enum DefenseLayer
    {
        Health,     // Red  — actual HP
        Armor,      // Gold — resists physical, weak to magic, persists between turns
        Barrier,    // Blue — resists magic, weak to physical, persists between turns
        Block       // Grey/White — neutral absorber, cleared at start of your turn
    }
}