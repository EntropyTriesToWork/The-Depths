public class RewardModifiers
{
    #region Fields
    public int cardRewardBonus = 0;
    public bool forcePickupAll = false; //if true player must pickup everything before they can proceed
    public bool autoClaimAllGold = false;

    #endregion

    #region Helpers
    public int ApplyToCardCount(int baseCount) => UnityEngine.Mathf.Max(1, baseCount + cardRewardBonus);

    #endregion
}
