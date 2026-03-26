using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static factory that builds RewardItem lists for common reward scenarios.
/// Always pass a RewardModifiers instance so relic/curse effects are applied.
///
/// Examples:
///   var mods = BuildModifiers(); // apply relic/curse effects to mods first
///   RewardScreen.Instance.Open(RewardScreenFactory.CombatReward(pool, mods, gold: 75), mods);
///   RewardScreen.Instance.Open(RewardScreenFactory.BossReward(pool, mods, relic, gold: 100), mods);
/// </summary>
public static class RewardScreenFactory
{
    #region Base Card Count

    public const int BaseCardCount = 3; // default before modifiers

    #endregion

    #region Common Scenarios

    /// <summary>
    /// Standard post-combat reward: instant gold + a card offer group.
    /// Card count = BaseCardCount + modifiers.cardCountBonus, minimum 1.
    /// </summary>
    public static List<RewardItem> CombatReward(CardPool pool, RewardModifiers modifiers, int gold = 0)
    {
        List<RewardItem> items = new();

        if (gold > 0)
            items.Add(RewardItem.Gold(gold));

        items.AddRange(BuildCardOffers(pool, modifiers));

        return items;
    }

    /// <summary>
    /// Boss reward: instant gold + a card offer group + a relic entry.
    /// </summary>
    public static List<RewardItem> BossReward(CardPool pool, RewardModifiers modifiers, RelicData relic, int gold = 0)
    {
        List<RewardItem> items = new();

        if (gold > 0)
            items.Add(RewardItem.Gold(gold));

        items.AddRange(BuildCardOffers(pool, modifiers));

        if (relic != null)
            items.Add(RewardItem.Relic(relic));

        return items;
    }

    /// <summary>
    /// Treasure room / event: a single relic entry.
    /// </summary>
    public static List<RewardItem> RelicOnly(RelicData relic)
    {
        return new List<RewardItem> { RewardItem.Relic(relic) };
    }

    /// <summary>
    /// Gold-only reward (some events). Claimed instantly when the screen opens.
    /// </summary>
    public static List<RewardItem> GoldOnly(int gold)
    {
        return new List<RewardItem> { RewardItem.Gold(gold) };
    }

    /// <summary>
    /// Fully custom reward list — supply any combination of RewardItems directly.
    /// </summary>
    public static List<RewardItem> Custom(params RewardItem[] items)
    {
        return new List<RewardItem>(items);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Draws cards from the pool using the modifier-adjusted count and wraps
    /// each one as a RewardItem.Card. All cards from one call form a single
    /// offer group — claiming or skipping any one removes the rest.
    /// </summary>
    private static List<RewardItem> BuildCardOffers(CardPool pool, RewardModifiers modifiers)
    {
        List<RewardItem> offers = new();

        if (pool == null)
        {
            Debug.LogWarning("[RewardScreenFactory] CardPool is null — skipping card offers.");
            return offers;
        }

        int count = modifiers?.ApplyToCardCount(BaseCardCount) ?? BaseCardCount;
        List<CardData> drawn = pool.GetRandom(count);

        foreach (CardData card in drawn)
            offers.Add(RewardItem.Card(card));

        return offers;
    }

    #endregion
}
