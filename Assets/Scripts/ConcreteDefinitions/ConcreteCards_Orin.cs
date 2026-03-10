// ============================================================
//  ConcreteCards_Orin.cs
//  All of Orin's cards hardcoded as factory methods.
//  No ScriptableObjects. No sprites.
//
//  Each method returns a CardInstance ready to add to a deck.
//  CardData is still used as the data container but created
//  entirely in code via ScriptableObject.CreateInstance<>
//  (required because CardEffect subclasses are SOs).
//
//  Usage:
//    CardInstance strike = ConcreteCards_Orin.Strike();
//    List<CardInstance> deck = ConcreteCards_Orin.StarterDeck();
//    List<CardData> pool = ConcreteCards_Orin.CardPool();
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    public static class ConcreteCards_Orin
    {
        // ============================================================
        // STARTER CARDS
        // ============================================================

        public static CardInstance Strike() => Build(
            id: "strike",
            name: "Strike",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(6, DamageType.Physical)
            }
        );

        public static CardInstance Defend() => Build(
            id: "defend",
            name: "Defend",
            type: CardType.Skill,
            cost: 1,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Block(5)
            }
        );

        public static CardInstance Bash() => Build(
            id: "bash",
            name: "Bash",
            type: CardType.Attack,
            cost: 2,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(8, DamageType.Physical),
                Status(StatusType.Exposed, 2, EffectTarget.SingleEnemy)
            }
        );
        public static CardInstance HeavyStrike() => Build(
            id: "heavy_strike",
            name: "Heavy Strike",
            type: CardType.Attack,
            cost: 2,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(14, DamageType.Physical)
            }
        );

        public static CardInstance IronWave() => Build(
            id: "iron_wave",
            name: "Iron Wave",
            type: CardType.Skill,
            cost: 1,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(5, DamageType.Physical),
                Block(5)
            }
        );

        public static CardInstance Cleave() => Build(
            id: "cleave",
            name: "Cleave",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(8, DamageType.Physical, EffectTarget.AllEnemies)
            }
        );

        public static CardInstance Acrobatics() => Build(
            id: "acrobatics",
            name: "Acrobatics",
            type: CardType.Skill,
            cost: 0,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Draw(2)
            }
        );

        public static CardInstance Flex() => Build(
            id: "flex",
            name: "Flex",
            type: CardType.Skill,
            cost: 0,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Status(StatusType.Strength, 2, EffectTarget.Self)
                // TODO: end-of-turn Strength removal via a trigger effect
            }
        );

        public static CardInstance VenomBlade() => Build(
            id: "venom_blade",
            name: "Venom Blade",
            type: CardType.Attack,
            cost: 2,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                Damage(5, DamageType.Physical),
                Status(StatusType.Poison, 3, EffectTarget.SingleEnemy)
            }
        );

        public static CardInstance BodySlam() => Build(
            id: "body_slam",
            name: "Body Slam",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Common,
            effects: new CardEffect[]
            {
                DamageEqualToBlock()
            }
        );

        // ============================================================
        // UNCOMMON CARDS
        // ============================================================

        public static CardInstance Whirlwind() => Build(
            id: "whirlwind",
            name: "Whirlwind",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Damage(5, DamageType.Physical, EffectTarget.AllEnemies),
                Damage(5, DamageType.Physical, EffectTarget.AllEnemies)
            }
        );

        public static CardInstance Pummel() => Build(
            id: "pummel",
            name: "Pummel",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Damage(2, DamageType.Physical),
                Damage(2, DamageType.Physical),
                Damage(2, DamageType.Physical),
                Damage(2, DamageType.Physical)
            }
        );

        public static CardInstance Barricade() => Build(
            id: "barricade",
            name: "Barricade",
            type: CardType.Power,
            cost: 3,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Status(StatusType.Barricade, 1, EffectTarget.Self)
            }
        );

        public static CardInstance Hatchet() => Build(
            id: "hatchet",
            name: "Hatchet",
            type: CardType.Attack,
            cost: 1,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Damage(9, DamageType.Physical),
                Draw(1)
            }
        );

        public static CardInstance RecklessCharge() => Build(
            id: "reckless_charge",
            name: "Reckless Charge",
            type: CardType.Attack,
            cost: 0,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Damage(7, DamageType.Physical)
                // TODO: add Dazed to discard via AddCardToDiscardEffect
            }
        );

        public static CardInstance IronShell() => Build(
            id: "iron_shell",
            name: "Iron Shell",
            type: CardType.Skill,
            cost: 1,
            rarity: CardRarity.Uncommon,
            effects: new CardEffect[]
            {
                Armor(7)
            }
        );

        // ============================================================
        // RARE CARDS
        // ============================================================

        public static CardInstance LimitBreak() => Build(
            id: "limit_break",
            name: "Limit Break",
            type: CardType.Skill,
            cost: 1,
            rarity: CardRarity.Rare,
            exhaust: true,
            effects: new CardEffect[]
            {
                // TODO: DoubleStatusEffect for Strength
                Status(StatusType.Strength, 0, EffectTarget.Self) // placeholder
            }
        );

        public static CardInstance Reaper() => Build(
            id: "reaper",
            name: "Reaper",
            type: CardType.Attack,
            cost: 2,
            rarity: CardRarity.Rare,
            effects: new CardEffect[]
            {
                Damage(4, DamageType.Physical, EffectTarget.AllEnemies)
                // TODO: LifestealEffect
            }
        );

        public static CardInstance Corruption() => Build(
            id: "corruption",
            name: "Corruption",
            type: CardType.Power,
            cost: 3,
            rarity: CardRarity.Rare,
            effects: new CardEffect[]
            {
                Status(StatusType.Corruption, 1, EffectTarget.Self)
            }
        );

        // ============================================================
        // STARTER DECK + FULL POOL
        // ============================================================

        /// <summary>
        /// Returns the starter deck instances for Orin.
        /// Call once per run — creates fresh instances every time.
        /// </summary>
        public static List<CardInstance> StarterDeck() => new List<CardInstance>
        {
            Strike(), Strike(), Strike(), Strike(), Strike(),
            Defend(), Defend(), Defend(), Defend(),
            Bash()
        };

        /// <summary>
        /// Returns the full pool of cards that can appear as rewards and in shops.
        /// Returns CardData (not instances) since the pool is shared across many copies.
        /// </summary>
        public static List<CardData> CardPool()
        {
            // Each method returns an instance; we grab its Data
            var pool = new List<CardData>();
            void Add(CardInstance c) { pool.Add(c.Data); }

            // Common
            Add(Strike()); Add(Defend()); Add(Bash());
            Add(HeavyStrike()); Add(IronWave()); Add(Cleave());
            Add(Acrobatics()); Add(Flex()); Add(VenomBlade()); Add(BodySlam());

            // Uncommon
            Add(Whirlwind()); Add(Pummel()); Add(Barricade());
            Add(Hatchet()); Add(RecklessCharge()); Add(IronShell());

            // Rare
            Add(LimitBreak()); Add(Reaper()); Add(Corruption());

            return pool;
        }

        /// <summary>
        /// Resolves a card by ID — used by CharacterDefinition.StarterDeckIDs.
        /// </summary>
        public static CardInstance GetByID(string id) => id switch
        {
            "strike" => Strike(),
            "defend" => Defend(),
            "bash" => Bash(),
            "heavy_strike" => HeavyStrike(),
            "iron_wave" => IronWave(),
            "cleave" => Cleave(),
            "acrobatics" => Acrobatics(),
            "flex" => Flex(),
            "venom_blade" => VenomBlade(),
            "body_slam" => BodySlam(),
            "whirlwind" => Whirlwind(),
            "pummel" => Pummel(),
            "barricade" => Barricade(),
            "hatchet" => Hatchet(),
            "reckless_charge" => RecklessCharge(),
            "iron_shell" => IronShell(),
            "limit_break" => LimitBreak(),
            "reaper" => Reaper(),
            "corruption" => Corruption(),
            _ => Strike()   // Fallback
        };

        // ============================================================
        // PRIVATE FACTORY HELPERS
        // Keep the card definitions above readable.
        // ============================================================

        private static CardInstance Build(
            string id,
            string name,
            CardType type,
            int cost,
            CardRarity rarity,
            CardEffect[] effects,
            bool exhaust = false)
        {
            var data = ScriptableObject.CreateInstance<CardData>();
            data.cardID = id;
            data.cardName = name;
            data.cardType = type;
            data.energyCost = cost;
            data.rarity = rarity;
            data.owner = CharacterClass.Orin;
            data.tier = UpgradeTier.Base;
            data.exhaust = exhaust;
            data.effects = effects;
            return new CardInstance(data);
        }

        // Effect shorthand helpers

        private static DealDamageEffect Damage(
            int magnitude,
            DamageType type = DamageType.Physical,
            EffectTarget target = EffectTarget.SingleEnemy)
        {
            var fx = ScriptableObject.CreateInstance<DealDamageEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = target;
            fx.damageType = type;
            fx.baseMagnitude = magnitude;
            return fx;
        }

        private static GainBlockEffect Block(int magnitude)
        {
            var fx = ScriptableObject.CreateInstance<GainBlockEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = EffectTarget.Self;
            fx.baseMagnitude = magnitude;
            return fx;
        }

        private static GainArmorEffect Armor(int magnitude)
        {
            var fx = ScriptableObject.CreateInstance<GainArmorEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = EffectTarget.Self;
            fx.baseMagnitude = magnitude;
            return fx;
        }

        private static GainBarrierEffect Barrier(int magnitude)
        {
            var fx = ScriptableObject.CreateInstance<GainBarrierEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = EffectTarget.Self;
            fx.baseMagnitude = magnitude;
            return fx;
        }

        private static ApplyStatusEffect Status(
            StatusType type,
            int stacks,
            EffectTarget target)
        {
            var fx = ScriptableObject.CreateInstance<ApplyStatusEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = target;
            fx.statusType = type;
            fx.baseMagnitude = stacks;
            return fx;
        }

        private static DrawCardsEffect Draw(int count)
        {
            var fx = ScriptableObject.CreateInstance<DrawCardsEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.baseMagnitude = count;
            return fx;
        }

        private static DamageEqualToBlockEffect DamageEqualToBlock()
        {
            var fx = ScriptableObject.CreateInstance<DamageEqualToBlockEffect>();
            fx.trigger = EffectTrigger.OnPlay;
            fx.target = EffectTarget.SingleEnemy;
            return fx;
        }
    }
}