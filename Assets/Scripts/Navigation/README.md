# Phase 3: Navigation & Level Selection

## File Map

```
Phase3Nav/
  Core/
    RunManager.cs             ← Updated: save hooks, mystery resolver, deck review
  Rooms/
    RoomType.cs               ← NEW standalone enum file
    EventData.cs              ← SPLIT OUT: was bundled in RoomData.cs before
    RoomChoice.cs             ← SPLIT OUT: its own file
    FloorConfig.cs            ← UPDATED: 10-15 rooms, mystery weights, placement rules
    MysteryRoomResolver.cs    ← NEW: resolves mystery rooms on entry
  Navigation/
    NavigationSystem.cs       ← UPDATED: boss on final room, elite spacing
  Save/
    RunSaveData.cs            ← NEW: serializable run snapshot
    SaveSystem.cs             ← NEW: JSON read/write + RunState conversion
    RunState_SavePatch.cs     ← PATCH: paste RestoreGold/RestoreProgression into RunState.cs
  Review/
    DeckReviewProvider.cs     ← NEW: read-only deck views for navigation UI
```

---

## Save Policy

| Event                   | Save? |
|-------------------------|-------|
| Room cleared (combat, shop, event, rest, treasure) | ✅ YES |
| Player selects a room choice | ❌ NO |
| Player opens deck review | ❌ NO |
| Run ends (victory or death) | ❌ NO — save deleted |

On load, the player **always lands on the navigation screen** with fresh choices generated. This means choices re-roll on reload (intentional — designer specified this is acceptable).

The total room count for the floor is saved so the boss still appears at the correct position after a reload.

---

## Mystery Room Weights (Default)

```
mysteryEventWeight:  60%   ← Events are most likely
mysteryCombatWeight: 30%   ← Normal combat is second
mysteryShopWeight:   10%   ← Shop is rarest
```

Tune these per floor in the FloorConfig asset. Floor 3 could increase combat weight to ramp pressure. All three values are visible and editable in the Inspector with no code changes.

---

## Room Count Per Floor

Each floor rolls a random room count between `minRooms` and `maxRooms` (defaults: 10–15). The boss always occupies the final slot. So a 12-room roll means 11 normal rooms and the boss at room 12.

The rolled count is saved in `RunSaveData.totalRoomsThisFloor` so loading a save doesn't re-roll the floor length.

---

## Floor Config Setup

Create one `FloorConfig` asset per floor:

**FloorConfig_1:**
- floorNumber: 1, minRooms: 10, maxRooms: 12
- normalCombatWeight: 45, eliteWeight: 8, shopWeight: 12, mysteryWeight: 25, restWeight: 8, treasureWeight: 2
- mysteryEventWeight: 60, mysteryCombatWeight: 30, mysteryShopWeight: 10
- guaranteedShopAtRoom: 4, eliteUnlockAfterRoom: 2, minRoomsBetweenElites: 2

**FloorConfig_2:**
- floorNumber: 2, minRooms: 11, maxRooms: 13
- eliteWeight: 12 (more elites)
- guaranteedShopAtRoom: 5

**FloorConfig_3:**
- floorNumber: 3, minRooms: 10, maxRooms: 15
- restWeight: 14 (more rest before boss)
- eliteWeight: 15
- guaranteedShopAtRoom: 5

---

## Deck Review Integration

During the navigation screen, the UI can call:

```csharp
// Get a summary (card count, type breakdown, average cost)
DeckSummary summary = runManager.GetDeckSummary();
Debug.Log(summary.ToString());
// Output: "14 cards | 6A 5S 2P 1C | 3 upgraded | avg cost 1.4"

// Get sorted card list for the review panel
var cards = runManager.GetDeckForReview(DeckReviewSort.ByCost);
foreach (var card in cards)
    Debug.Log($"{card.Data.cardName} [{card.GetEffectiveCost()}e] T{(int)card.CurrentTier}");

// Or access the full provider for grouped views
var byType = runManager.DeckReview.GetGroupedByType();
var upgradeable = runManager.DeckReview.GetUpgradeable();
```

The deck review is purely read-only — it cannot modify the deck.

---

## Navigation Screen — What the UI Needs

**Subscribe to:**
```csharp
runManager.OnChoicesGenerated   += ShowNavigationUI;
runManager.OnProgressLabelUpdated += UpdateProgressBar;
runManager.OnRoomEntered        += TransitionToRoom;
runManager.OnRewardAvailable    += ShowRewardScreen;
runManager.OnEventStarted       += ShowEventUI;
runManager.OnShopOpened         += ShowShopUI;
runManager.OnRestSiteOpened     += ShowRestUI;
runManager.OnRunEnded           += ShowRunEndScreen;
```

**Call when player acts:**
```csharp
runManager.SelectChoice(choice)
runManager.ClaimReward(cardData)        // null = skip
runManager.CloseShop()
runManager.ResolveEvent(eventChoice)
runManager.ResolveRest(type, card)
```

**For each RoomChoice displayed:**
```csharp
choice.RoomType    // RoomType enum for icon selection
choice.Label       // "Monster Room", "?", "Shop", etc.
choice.Hint        // Shown below label
// Note: Mystery rooms have Label="?" and Hint="Unknown — anything could be inside"
// Do NOT show enemies or event name for Mystery rooms
```

---

## Applying the Patch

Two steps before this compiles:

1. Paste `RestoreGold()` and `RestoreProgression()` from `RunState_SavePatch.cs` into `RunState.cs`
2. Add `public int TotalRoomsThisFloor` as a settable property to `NavigationSystem.cs`:

```csharp
// In NavigationSystem.cs, change:
public int TotalRoomsThisFloor { get; private set; }
// To:
public int TotalRoomsThisFloor { get; set; }
```

This allows `RunManager.TryLoadRun()` to restore the saved floor length.
