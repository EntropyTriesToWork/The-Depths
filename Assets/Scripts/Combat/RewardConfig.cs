using System.Collections.Generic;
using UnityEngine;

namespace CardGame
{
    [CreateAssetMenu(menuName = "CardGame/Reward Config", fileName = "RewardConfig")]
    public class RewardConfig : ScriptableObject
    {
        [Header("Gold Rewards")]
        public Vector2Int normalCombatGold = new Vector2Int(10, 20);
        public Vector2Int eliteCombatGold = new Vector2Int(25, 35);
        public Vector2Int bossCombatGold = new Vector2Int(50, 65);

        [Header("Card Choices")]
        [Tooltip("How many card options the player sees after a normal combat.")]
        public int normalCardChoices = 3;
        public int eliteCardChoices = 3;
        public int bossCardChoices = 3; // Boss often gives rarer cards

        [Header("Rarity Weights — Normal Combat")]
        [Tooltip("Relative probability weights for Common/Uncommon/Rare.")]
        public Vector3 normalRarityWeights = new Vector3(60f, 37f, 3f);

        [Header("Rarity Weights — Elite Combat")]
        public Vector3 eliteRarityWeights = new Vector3(0f, 60f, 40f);

        [Header("Rarity Weights — Boss Combat")]
        public Vector3 bossRarityWeights = new Vector3(0f, 33f, 67f);

        [Header("Essence")]
        [Tooltip("% chance to include Essence in an elite reward.")]
        [Range(0, 100)]
        public int eliteEssenceChance = 50;

        [Tooltip("Amount of Essence from elite rewards.")]
        public Vector2Int eliteEssenceAmount = new Vector2Int(1, 2);
    }
    public class CombatRewardSet
    {
        public int GoldReward    { get; private set; }

        // Card reward: player picks 0 or 1 cards from this list
        public List<CardData> CardChoices { get; private set; }

        public bool       IncludesEssence    { get; private set; }
        public int        EssenceAmount      { get; private set; }
        public bool       IncludesSoulShard  { get; private set; }

        public bool IsEliteReward { get; private set; }
        public bool IsBossReward  { get; private set; }

        public CombatRewardSet(
            int gold,
            List<CardData> cardChoices,
            bool isElite = false,
            bool isBoss  = false)
        {
            GoldReward   = gold;
            CardChoices  = cardChoices ?? new List<CardData>();
            IsEliteReward = isElite;
            IsBossReward  = isBoss;
        }

        public void AddEssence(int amount)
        {
            IncludesEssence = true;
            EssenceAmount   = amount;
        }

        public void AddSoulShard()
        {
            IncludesSoulShard = true;
        }
    }
}
