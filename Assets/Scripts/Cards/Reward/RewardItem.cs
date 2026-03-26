/// <summary>
/// Describes a single reward entry displayed on the RewardScreen.
/// Build these with RewardScreenFactory and pass a list to RewardScreen.Open().
/// </summary>
[System.Serializable]
public class RewardItem
{
    #region Types

    public enum RewardType { Gold, Card, Relic }

    #endregion

    #region Data

    public RewardType type;
    public bool       canSkip; // gold is always false; cards and relics default to true

    public int        goldAmount; // used when type == Gold
    public CardData   card;       // used when type == Card
    public RelicData  relic;      // used when type == Relic

    #endregion

    #region Factories

    /// <summary>
    /// Gold reward — added to PlayerInventory immediately on display;
    /// canSkip is always false.
    /// </summary>
    public static RewardItem Gold(int amount) => new()
    {
        type       = RewardType.Gold,
        goldAmount = amount,
        canSkip    = false
    };

    public static RewardItem Card(CardData card, bool canSkip = true) => new()
    {
        type    = RewardType.Card,
        card    = card,
        canSkip = canSkip
    };

    public static RewardItem Relic(RelicData relic, bool canSkip = true) => new()
    {
        type    = RewardType.Relic,
        relic   = relic,
        canSkip = canSkip
    };

    #endregion
}
