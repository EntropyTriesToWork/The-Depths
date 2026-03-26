using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight, type-safe static event bus.
/// Subscribe:   GameEvents.Subscribe&lt;MyEvent&gt;(handler);
/// Unsubscribe: GameEvents.Unsubscribe&lt;MyEvent&gt;(handler);
/// Raise:       GameEvents.Raise(new MyEvent { ... });
/// Events are plain structs — no allocation overhead on raise.
/// Always unsubscribe in OnDisable / OnDestroy to avoid stale references.
/// </summary>
public static class GameEvents
{
    #region Core

    private static readonly Dictionary<Type, List<Delegate>> handlers = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        Type key = typeof(T);
        if (!handlers.ContainsKey(key))
            handlers[key] = new List<Delegate>();
        handlers[key].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        Type key = typeof(T);
        if (handlers.TryGetValue(key, out List<Delegate> list))
        {
            list.Remove(handler);
            if (list.Count == 0)
                handlers.Remove(key);
        }
    }

    public static void Raise<T>(T evt)
    {
        Type key = typeof(T);
        if (!handlers.TryGetValue(key, out List<Delegate> list)) return;

        Delegate[] snapshot = list.ToArray(); // copy so handlers can safely unsubscribe mid-raise
        foreach (Delegate d in snapshot)
        {
            try { ((Action<T>)d).Invoke(evt); }
            catch (Exception ex) { Debug.LogError($"[GameEvents] Handler exception for {key.Name}: {ex}"); }
        }
    }

    public static void ClearAll() => handlers.Clear(); // call between scenes or on full game reset

    public static void Clear<T>() => handlers.Remove(typeof(T));

    #endregion

    #region Debug

#if UNITY_EDITOR
    public static int GetSubscriberCount<T>() =>
        handlers.TryGetValue(typeof(T), out List<Delegate> list) ? list.Count : 0;
#endif

    #endregion
}

#region Game State Events

public struct OnGameStateChangedEvent
{
    public GameState previousState;
    public GameState newState;
    public OnGameStateChangedEvent(GameState prev, GameState next) { previousState = prev; newState = next; }
}

#endregion

#region Map / Exploration Events

public struct OnRoomEnteredEvent
{
    public RoomType roomType;
    public OnRoomEnteredEvent(RoomType t) => roomType = t;
}

public struct OnFloorCompletedEvent
{
    public int floorNumber;
    public OnFloorCompletedEvent(int f) => floorNumber = f;
}

#endregion

#region Combat Events

public struct OnCombatStartedEvent
{
    public Entity[] enemies;
    public OnCombatStartedEvent(Entity[] e) => enemies = e;
}
public struct OnCombatEndedEvent
{
    public bool playerWon;
    public OnCombatEndedEvent(bool w) => playerWon = w;
}
public struct OnPlayerTurnStartedEvent { }
public struct OnPlayerTurnEndedEvent { }
public struct OnEnemyTurnStartedEvent { public Entity enemy; }
public struct OnEnemyTurnEndedEvent { public Entity enemy; }
#endregion

#region Card Combat Events
public struct OnCardDrawnEvent
{
    public CardData card;
    public OnCardDrawnEvent(CardData c) => card = c;
}
public struct OnCardPlayedEvent
{
    public CardData card;
    public List<Entity> targets;
    public OnCardPlayedEvent(CardData c, List<Entity> t) { card = c; targets = t; }
}

public struct OnCardDiscardedEvent
{
    public CardData card;
    public OnCardDiscardedEvent(CardData c) => card = c;
}

#endregion

#region Deck / Collection Events
public struct OnCardObtainedEvent
{
    public CardData card;
    public OnCardObtainedEvent(CardData c) => card = c;
}
public struct OnCardRemovedFromDeckEvent
{
    public CardData card;
    public OnCardRemovedFromDeckEvent(CardData c) => card = c;
}

public struct OnCardUpgradedEvent
{
    public CardData original;
    public CardData upgraded;
    public OnCardUpgradedEvent(CardData o, CardData u) { original = o; upgraded = u; }
}

#endregion

#region Resource Events

public struct OnGoldChangedEvent
{
    public int previousAmount;
    public int newAmount;
    public OnGoldChangedEvent(int prev, int next) { previousAmount = prev; newAmount = next; }
}

public struct OnPotionUsedEvent
{
    public PotionData potion;
    public Entity target;
    public OnPotionUsedEvent(PotionData p, Entity t) { potion = p; target = t; }
}

public struct OnRelicObtainedEvent
{
    public RelicData relic;
    public OnRelicObtainedEvent(RelicData r) => relic = r;
}

#endregion

#region Placeholder Stubs  —  replace with real ScriptableObject files when built

[System.Serializable] public class PotionData : UnityEngine.ScriptableObject { public string potionName; }
[System.Serializable] public class RelicData : UnityEngine.ScriptableObject { public string relicName; }
public enum RoomType { Monster, Elite, Rest, Shop, Treasure, Boss, Event, Unknown }

#endregion