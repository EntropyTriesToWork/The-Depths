using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Global card pool ScriptableObject. Holds every card that can appear
/// as a reward offer. RewardScreenFactory draws from this at runtime.
/// Create via: Assets > Create > Cards > CardPool
/// </summary>
[CreateAssetMenu(menuName = "Cards/CardPool", fileName = "CardPool")]
public class CardPool : ScriptableObject
{
    #region Data

    [Tooltip("Every card that can appear as a reward offer.")]
    public List<CardData> allCards = new();

    #endregion

    #region Public API

    /// <summary>
    /// Returns <paramref name="count"/> unique random cards from the pool,
    /// excluding any cards already in <paramref name="exclude"/>.
    /// Returns fewer than <paramref name="count"/> if the pool is too small.
    /// </summary>
    public List<CardData> GetRandom(int count, ICollection<CardData> exclude = null)
    {
        List<CardData> candidates = new();
        foreach (CardData card in allCards)
        {
            if (exclude == null || !exclude.Contains(card))
                candidates.Add(card);
        }

        List<CardData> result = new();
        while (result.Count < count && candidates.Count > 0)
        {
            int idx = Random.Range(0, candidates.Count);
            result.Add(candidates[idx]);
            candidates.RemoveAt(idx); // no duplicates in one offer
        }

        return result;
    }

    #endregion
}
