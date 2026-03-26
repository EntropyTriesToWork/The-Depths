using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the state of an active reward screen — which rewards are pending,
/// which have been claimed or skipped, and when the screen is fully resolved.
///
/// Does NOT handle UI rendering — a presenter/view layer subscribes to the
/// events below. Does NOT decide what comes after closing — raises
/// OnRewardScreenClosed and lets the caller respond.
///
/// Typical flow:
///   1. Build modifiers: apply relic/curse effects to a RewardModifiers instance.
///   2. Build items:     RewardScreenFactory.CombatReward(pool, modifiers, gold).
///   3. Open screen:     RewardScreen.Instance.Open(items, modifiers).
///   4. Gold is claimed automatically; remaining items appear in PendingRewards.
///   5. Player clicks a card/relic entry → Claim(item).
///   6. Player clicks skip on an entry   → Skip(item)  [if canSkip is true].
///   7. Player clicks Proceed            → Proceed()   [only valid when CanProceed].
///   8. OnRewardScreenClosed fires — caller drives the next state.
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

    public bool IsOpen        { get; private set; }
    public bool ForcePickupAll { get; private set; }
    public bool AutoClaimGold { get; private set; }
    public bool CanProceed // True when every pending reward has canSkip == true (or the list is empty / everything was picked up).
    {
        get
        {
            foreach (RewardItem item in pendingRewards)
                if (!item.canSkip) return false;
            return true;
        }
    }
    #endregion

    #region Events

    public event System.Action<IReadOnlyList<RewardItem>> OnRewardScreenOpened; // after gold auto-claimed
    public event System.Action<RewardItem>                OnRewardClaimed;
    public event System.Action<RewardItem>                OnRewardSkipped;
    public event System.Action                            OnRewardScreenClosed;  // caller decides next state

    #endregion

    #region Public API
    public void Open(List<RewardItem> items, RewardModifiers modifiers = null)
    {
        pendingRewards.Clear();
        IsOpen         = true;
        ForcePickupAll = modifiers?.forcePickupAll ?? false;
        AutoClaimGold = modifiers.autoClaimAllGold;

        foreach (RewardItem item in items)
        {
            if (item.type == RewardItem.RewardType.Gold && AutoClaimGold)
            {
                ClaimGoldImmediately(item); // gold never enters the pending list
                continue;
            }

            if (ForcePickupAll)
                item.canSkip = false; // curse/relic forces all rewards to be taken

            pendingRewards.Add(item);
        }

        OnRewardScreenOpened?.Invoke(PendingRewards);
    }
    public void Claim(RewardItem item)
    {
        if (!pendingRewards.Contains(item)) return;

        switch (item.type)
        {
            case RewardItem.RewardType.Card:
                PlayerInventory.Instance?.AddCard(item.card);
                RemoveAllCardsFromPending(); // one card pick dismisses the whole group
                break;

            case RewardItem.RewardType.Relic:
                PlayerInventory.Instance?.AddRelic(item.relic);
                pendingRewards.Remove(item);
                break;
        }

        OnRewardClaimed?.Invoke(item);
        GameEvents.Raise(new OnRewardClaimedEvent(item));
    }
    public void Skip(RewardItem item)
    {
        if (!pendingRewards.Contains(item)) return;

        if (!item.canSkip)
        {
            Debug.LogWarning($"[RewardScreen] Cannot skip a forced reward: {item.type}");
            return;
        }

        if (item.type == RewardItem.RewardType.Card)
            RemoveAllCardsFromPending(); // skipping one card dismisses the whole group
        else
            pendingRewards.Remove(item);

        OnRewardSkipped?.Invoke(item);
        GameEvents.Raise(new OnRewardSkippedEvent(item));
    }
    public void Proceed()
    {
        if (!CanProceed)
        {
            Debug.LogWarning("[RewardScreen] Proceed called with mandatory rewards still pending.");
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
        GameEvents.Raise(new OnRewardClaimedEvent(item)); // UI can show a visual beat on this
    }

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
