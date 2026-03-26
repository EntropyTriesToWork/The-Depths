using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static factory that builds RewardItem lists for common reward scenarios.
/// Call the appropriate method, then pass the result to RewardScreen.Open().
///
/// Examples:
///   RewardScreen.Instance.Open(RewardScreenFactory.CombatReward(cardPool, 3, goldAmount));
///   RewardScreen.Instance.Open(RewardScreenFactory.BossReward(cardPool, 3, relic, goldAmount));
///   RewardScreen.Instance.Open(RewardScreenFactory.RelicOnly(relic));
/// </summary>
public static class RewardScreenFactory
{
    #region Common Scenarios

    /// <summary>
    /// Standard post-combat reward: gold (instant) + a card choice.
    /// </summary>
    /// <param name="pool">Global card pool to draw choices from.</param>
    /// <param name="cardCount">Number of cards to offer.</param>
    /// <param name="gold">Gold to award instantly. Pass 0 to omit.</param>
    public static List<RewardItem> CombatReward(CardPool pool, int cardCount, int gold = 0)
    {
        List<RewardItem> items = new();

        if (gold > 0)
            items.Add(RewardItem.Gold(gold));

        items.AddRange(BuildCardOffers(pool, cardCount));

        return items;
    }

    /// <summary>
    /// Boss reward: gold (instant) + a card choice + a relic pickup.
    /// </summary>
    public static List<RewardItem> BossReward(CardPool pool, int cardCount, RelicData relic, int gold = 0)
    {
        List<RewardItem> items = new();

        if (gold > 0)
            items.Add(RewardItem.Gold(gold));

        items.AddRange(BuildCardOffers(pool, cardCount));

        if (relic != null)
            items.Add(RewardItem.Relic(relic, canSkip: true));

        return items;
    }

    /// <summary>
    /// Treasure room or event: a single relic, skippable.
    /// </summary>
    public static List<RewardItem> RelicOnly(RelicData relic, bool canSkip = true)
    {
        return new List<RewardItem> { RewardItem.Relic(relic, canSkip) };
    }

    /// <summary>
    /// Gold-only reward (e.g. some events). Claimed instantly on Open().
    /// </summary>
    public static List<RewardItem> GoldOnly(int gold)
    {
        return new List<RewardItem> { RewardItem.Gold(gold) };
    }

    /// <summary>
    /// Fully custom reward list — supply any combination of items directly.
    /// </summary>
    public static List<RewardItem> Custom(params RewardItem[] items)
    {
        return new List<RewardItem>(items);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Draws <paramref name="count"/> unique random cards from <paramref name="pool"/>
    /// and wraps each one as a skippable RewardItem.Card entry.
    /// All cards in one call share the same "offer group" — claiming or skipping
    /// any one of them removes the rest via RewardScreen.
    /// </summary>
    private static List<RewardItem> BuildCardOffers(CardPool pool, int count)
    {
        List<RewardItem> offers = new();

        if (pool == null)
        {
            Debug.LogWarning("[RewardScreenFactory] CardPool is null — no card offers generated.");
            return offers;
        }

        List<CardData> drawn = pool.GetRandom(count);
        foreach (CardData card in drawn)
            offers.Add(RewardItem.Card(card, canSkip: true));

        return offers;
    }

    #endregion
}
