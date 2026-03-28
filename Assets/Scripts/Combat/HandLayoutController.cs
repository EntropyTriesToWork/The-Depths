using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the player's hand as a flat evenly-spaced row of cards.
/// Spawns CardUIController prefabs when cards are drawn, removes them when
/// played or discarded, and repositions all cards whenever the hand changes.
///
/// Attach to the hand container GameObject in the combat scene.
/// Subscribe to CombatManager events to stay in sync automatically.
/// </summary>
public class HandLayoutController : MonoBehaviour
{
    #region Singleton

    public static HandLayoutController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    #region Inspector

    [Header("References")]
    [SerializeField] private GameObject      cardPrefab;   // must have CardUIController
    [SerializeField] private RectTransform   handContainer; // parent for all card objects

    [Header("Layout")]
    [SerializeField] private float cardSpacing  = 110f;  // gap between card centres
    [SerializeField] private float cardY        = 0f;    // local Y of all cards in the row

    #endregion

    #region State

    private readonly List<CardUIController> cardControllers = new();

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCardDrawn    += HandleCardDrawn;
            CombatManager.Instance.OnCardPlayed   += HandleCardRemoved;
            CombatManager.Instance.OnCardDiscarded += HandleCardRemoved;
        }
    }

    private void OnDisable()
    {
        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.OnCardDrawn    -= HandleCardDrawn;
            CombatManager.Instance.OnCardPlayed   -= HandleCardRemoved;
            CombatManager.Instance.OnCardDiscarded -= HandleCardRemoved;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Removes a card controller from the hand and destroys its GameObject.
    /// Called by CardUIController after a successful play.
    /// </summary>
    public void RemoveCard(CardUIController controller)
    {
        if (cardControllers.Remove(controller))
        {
            Destroy(controller.gameObject);
            RefreshLayout();
        }
    }

    /// <summary>Repositions all cards in an evenly-spaced flat row.</summary>
    public void RefreshLayout()
    {
        int count = cardControllers.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * cardSpacing;
        float startX     = -totalWidth * 0.5f; // centre the row

        for (int i = 0; i < count; i++)
        {
            RectTransform rt = cardControllers[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * cardSpacing, cardY);
        }
    }

    /// <summary>Destroys all card objects and clears the hand (e.g. on combat end).</summary>
    public void ClearHand()
    {
        foreach (var controller in cardControllers)
            if (controller != null) Destroy(controller.gameObject);
        cardControllers.Clear();
    }

    #endregion

    #region Event Handlers

    private void HandleCardDrawn(CardData card)
    {
        SpawnCard(card);
    }

    private void HandleCardRemoved(CardData card)
    {
        // Find the first controller matching this card data and remove it
        CardUIController match = cardControllers.Find(c => c != null && CardMatches(c, card));
        if (match != null) RemoveCard(match);
    }

    #endregion

    #region Private Helpers

    private void SpawnCard(CardData data)
    {
        if (cardPrefab == null || handContainer == null)
        {
            Debug.LogWarning("[HandLayoutController] cardPrefab or handContainer not assigned.");
            return;
        }

        GameObject obj        = Instantiate(cardPrefab, handContainer);
        var        controller = obj.GetComponent<CardUIController>();

        if (controller == null)
        {
            Debug.LogWarning("[HandLayoutController] cardPrefab is missing CardUIController.");
            Destroy(obj);
            return;
        }

        controller.Initialize(data, CardUIController.CardMode.Combat);
        cardControllers.Add(controller);
        RefreshLayout();
    }

    private bool CardMatches(CardUIController controller, CardData data)
    {
        // Access the card data stored on the controller via a public accessor
        return controller.CardData == data;
    }

    #endregion
}
