/// <summary>
/// Describes a single reward entry displayed on the RewardScreen.
/// Build these with RewardScreenFactory and pass a list to RewardScreen.Open().

/// RewardScreen.Open() overrides canSkip to false on all entries when
/// RewardModifiers.forcePickupAll is set.
/// </summary>
[System.Serializable]
public class RewardItem
{
    #region Types

    public enum RewardType { Gold, Card, Relic }

    #endregion

    #region Data

    public RewardType type;
    public bool       canSkip;

    public int        goldAmount;
    public CardData   card;
    public RelicData  relic;

    #endregion

    #region Factories

    public static RewardItem Gold(int amount) => new()
    {
        type       = RewardType.Gold,
        goldAmount = amount,
        canSkip    = true
    };

    public static RewardItem Card(CardData card) => new()
    {
        type    = RewardType.Card,
        card    = card,
        canSkip = true 
    };

    public static RewardItem Relic(RelicData relic) => new()
    {
        type    = RewardType.Relic,
        relic   = relic,
        canSkip = true
    };

    #endregion
}
