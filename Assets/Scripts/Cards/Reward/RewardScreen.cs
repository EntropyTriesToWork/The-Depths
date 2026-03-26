using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of an active reward screen — which rewards are pending,
/// which have been claimed or skipped, and when the screen is fully resolved.
///
/// Does NOT handle UI rendering; that is left to a presenter / view layer
/// that subscribes to the events below. Does NOT decide what happens after
/// the screen closes — raise OnRewardScreenClosed and let the caller respond.
///
/// Typical flow:
///   1. RewardScreenFactory.Create(...) builds a List of RewardItems.
///   2. Caller calls RewardScreen.Open(items) to begin the session.
///   3. Gold rewards are claimed automatically in Open().
///   4. Player clicks a card/relic → Claim(item) is called.
///   5. Player clicks skip on a card/relic → Skip(item) is called.
///   6. Once all non-skippable items are resolved, Proceed() becomes valid.
///   7. Player clicks Proceed → OnRewardScreenClosed fires.
/// </summary>
public class RewardScreen : MonoBehaviour
{
    #region Singleton

    public static RewardScreen Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    #region State

    public IReadOnlyList<RewardItem> PendingRewards => pendingRewards;
    private readonly List<RewardItem> pendingRewards = new();

    public bool IsOpen { get; private set; }

    /// <summary>True once every non-skippable reward has been resolved.</summary>
    public bool CanProceed
    {
        get
        {
            foreach (RewardItem item in pendingRewards)
                if (!item.canSkip) return false; // a mandatory item is still pending
            return true;
        }
    }

    #endregion

    #region Events

    /// <summary>Fired after Open() processes the full item list (including auto-gold).</summary>
    public event System.Action<IReadOnlyList<RewardItem>> OnRewardScreenOpened;

    /// <summary>Fired when the player claims a reward. UI should remove the entry.</summary>
    public event System.Action<RewardItem> OnRewardClaimed;

    /// <summary>Fired when the player skips a skippable reward. UI should remove the entry.</summary>
    public event System.Action<RewardItem> OnRewardSkipped;

    /// <summary>
    /// Fired when the player clicks Proceed and all mandatory rewards are resolved.
    /// The caller (room system, GameManager, etc.) decides what scene/state comes next.
    /// </summary>
    public event System.Action OnRewardScreenClosed;

    #endregion

    #region Public API

    /// <summary>
    /// Opens the reward screen with the supplied items.
    /// Gold rewards are claimed immediately — they never sit in the pending list.
    /// </summary>
    public void Open(List<RewardItem> items)
    {
        pendingRewards.Clear();
        IsOpen = true;

        foreach (RewardItem item in items)
        {
            if (item.type == RewardItem.RewardType.Gold)
                ClaimGoldImmediately(item); // gold is instant, never shown as a choice
            else
                pendingRewards.Add(item);
        }

        OnRewardScreenOpened?.Invoke(PendingRewards);
    }

    /// <summary>
    /// Claims a reward — adds it to the player's inventory and removes it from
    /// the pending list. Only one card from a card reward group may be claimed.
    /// </summary>
    public void Claim(RewardItem item)
    {
        if (!pendingRewards.Contains(item)) return;

        switch (item.type)
        {
            case RewardItem.RewardType.Card:
                PlayerInventory.Instance?.AddCard(item.card);
                RemoveAllCardsFromPending(); // claiming one card removes all card offers
                break;

            case RewardItem.RewardType.Relic:
                PlayerInventory.Instance?.AddRelic(item.relic);
                pendingRewards.Remove(item);
                break;
        }

        OnRewardClaimed?.Invoke(item);
        GameEvents.Raise(new OnRewardClaimedEvent(item));
    }

    /// <summary>
    /// Skips a skippable reward, removing it from the pending list without granting it.
    /// Skipping a card offer removes all card choices for that reward group.
    /// </summary>
    public void Skip(RewardItem item)
    {
        if (!pendingRewards.Contains(item)) return;
        if (!item.canSkip)
        {
            Debug.LogWarning($"[RewardScreen] Attempted to skip a non-skippable reward: {item.type}");
            return;
        }

        if (item.type == RewardItem.RewardType.Card)
            RemoveAllCardsFromPending(); // skipping the card group removes all its choices
        else
            pendingRewards.Remove(item);

        OnRewardSkipped?.Invoke(item);
        GameEvents.Raise(new OnRewardSkippedEvent(item));
    }

    /// <summary>
    /// Called when the player clicks the Proceed / Continue button.
    /// Does nothing if mandatory rewards are still unresolved.
    /// </summary>
    public void Proceed()
    {
        if (!CanProceed)
        {
            Debug.LogWarning("[RewardScreen] Proceed called but mandatory rewards are still pending.");
            return;
        }

        IsOpen = false;
        pendingRewards.Clear();
        OnRewardScreenClosed?.Invoke();
        GameEvents.Raise(new OnRewardScreenClosedEvent());
    }

    #endregion

    #region Private Helpers

    private void ClaimGoldImmediately(RewardItem item)
    {
        PlayerInventory.Instance?.AddGold(item.goldAmount);
        GameEvents.Raise(new OnRewardClaimedEvent(item)); // notify UI for a visual beat if desired
    }

    /// <summary>Removes all Card-type entries from pending (one offer group at a time).</summary>
    private void RemoveAllCardsFromPending()
    {
        pendingRewards.RemoveAll(r => r.type == RewardItem.RewardType.Card);
    }

    #endregion
}

#region Reward Events

public struct OnRewardClaimedEvent
{
    public RewardItem item;
    public OnRewardClaimedEvent(RewardItem i) => item = i;
}

public struct OnRewardSkippedEvent
{
    public RewardItem item;
    public OnRewardSkippedEvent(RewardItem i) => item = i;
}

public struct OnRewardScreenClosedEvent { }

#endregion
