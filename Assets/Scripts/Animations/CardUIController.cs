using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the visual and interactive behaviour of a single card UI element.
/// Behaviour is determined entirely by the CardMode set during Initialize() —
/// the same prefab is reused across deck view, rewards, and combat.
///
/// DeckView  — hover expands card; right-click opens examine panel.
/// Reward    — same as DeckView plus left-click adds card to deck.
/// Combat    — hover reveals description; left-click begins drag-to-play;
///             targeted cards show a bezier arrow; untargeted cards play when
///             dragged above the play threshold.
/// </summary>
public class CardUIController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
    IDragHandler, IBeginDragHandler, IEndDragHandler
{
    #region Mode

    public enum CardMode { DeckView, Reward, Combat }

    #endregion

    #region Inspector — Layout

    [Header("References")]
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private RectTransform descriptionContainer;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI energyCostText;
    [SerializeField] private TextMeshProUGUI manaGainText;
    [SerializeField] private Image artImage;

    [Header("Card Sizes")]
    [SerializeField] private float normalWidth = 100f;
    [SerializeField] private float hoverWidth = 120f;
    [SerializeField] private float normalHeight = 100f;
    [SerializeField] private float hoverHeight = 150f;

    [Header("Description Container")]
    [SerializeField] private float descNormalHeight = 0f;
    [SerializeField] private float descHoverHeight = 40f;
    [SerializeField] private bool descAlwaysVisible = false;

    [Header("Transitions")]
    [SerializeField] private float transitionDuration = 0.2f;

    [Header("Combat Drag")]
    [Tooltip("Normalised screen Y above which an untargeted card is played (0=bottom, 1=top).")]
    [SerializeField] private float playThresholdY = 0.4f;

    #endregion

    #region Private State

    private CardMode mode;
    private CardData cardData;
    private UnityEngine.Events.UnityAction onClickAction;

    public CardData CardData => cardData; // read-only accessor for HandLayoutController

    private bool isDragging;
    private bool isTargeted;      // does this card require an explicit target?
    private Vector2 dragStartPos;
    private RectTransform canvasRect;
    private Canvas rootCanvas;

    private Coroutine widthCoroutine;
    private Coroutine heightCoroutine;
    private Coroutine descHeightCoroutine;
    private Coroutine descAlphaCoroutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) canvasRect = rootCanvas.GetComponent<RectTransform>();

        SetCardSize(normalWidth, normalHeight);

        if (descAlwaysVisible)
        {
            SetDescriptionHeight(descHoverHeight);
            SetDescriptionAlpha(1f);
        }
        else
        {
            SetDescriptionHeight(descNormalHeight);
            SetDescriptionAlpha(0f);
        }
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Configures the card for a specific context.
    /// Call this immediately after instantiating the prefab.
    /// </summary>
    /// <param name="data">Source CardData ScriptableObject.</param>
    /// <param name="cardMode">Determines which interactions are active.</param>
    /// <param name="descActive">Force description always visible.</param>
    /// <param name="onClickAction">
    ///   DeckView/Reward: called on left-click (e.g. add to deck).
    ///   Combat: not used directly — drag-to-play is handled internally.
    /// </param>
    public void Initialize(CardData data, CardMode cardMode, bool descActive = false, UnityEngine.Events.UnityAction onClickAction = null)
    {
        cardData = data;
        mode = cardMode;
        this.onClickAction = onClickAction;
        descAlwaysVisible = descActive;

        nameText.text = data.cardName;
        descriptionText.text = BuildDescription(data);
        energyCostText.text = data.manaCost.ToString();

        int totalManaGain = 0;
        foreach (var entry in data.effects)
            totalManaGain += entry.parameters.manaGain;
        manaGainText.text = totalManaGain > 0 ? $"+{totalManaGain}" : "0";

        if (artImage != null) artImage.sprite = data.art;

        isTargeted = IsTargetedCard(data);

        if (descAlwaysVisible)
        {
            SetDescriptionHeight(descHoverHeight);
            SetDescriptionAlpha(1f);
        }
    }

    #endregion

    #region Pointer Events

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging) return;

        StopAllTransitions();
        widthCoroutine = StartCoroutine(TweenWidth(normalWidth, hoverWidth, transitionDuration));
        heightCoroutine = StartCoroutine(TweenHeight(normalHeight, hoverHeight, transitionDuration));

        if (!descAlwaysVisible)
        {
            descHeightCoroutine = StartCoroutine(TweenDescHeight(descNormalHeight, descHoverHeight, transitionDuration));
            descAlphaCoroutine = StartCoroutine(TweenAlpha(0f, 1f, transitionDuration));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;

        StopAllTransitions();
        widthCoroutine = StartCoroutine(TweenWidth(hoverWidth, normalWidth, transitionDuration));
        heightCoroutine = StartCoroutine(TweenHeight(hoverHeight, normalHeight, transitionDuration));

        if (!descAlwaysVisible)
        {
            descHeightCoroutine = StartCoroutine(TweenDescHeight(descHoverHeight, descNormalHeight, transitionDuration));
            descAlphaCoroutine = StartCoroutine(TweenAlpha(1f, 0f, transitionDuration));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mode == CardMode.Combat && eventData.button == PointerEventData.InputButton.Left)
            dragStartPos = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData) { } // handled in EndDrag

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return; // drag-release should not also fire a click

        switch (mode)
        {
            case CardMode.DeckView:
                if (eventData.button == PointerEventData.InputButton.Right)
                    OpenExaminePanel();
                break;

            case CardMode.Reward:
                if (eventData.button == PointerEventData.InputButton.Right)
                    OpenExaminePanel();
                else if (eventData.button == PointerEventData.InputButton.Left)
                    onClickAction?.Invoke(); // adds card to deck, handled by the spawner
                break;

            case CardMode.Combat:
                break; // combat input is entirely drag-based
        }
    }

    #endregion

    #region Drag Events (Combat only)

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mode != CardMode.Combat) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        isDragging = true;

        if (isTargeted)
            CombatArrow.Instance?.Show(transform.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mode != CardMode.Combat || !isDragging) return;

        if (!isTargeted)
            MoveCardWithMouse(eventData); // untargeted: card follows cursor
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (mode != CardMode.Combat || !isDragging) return;
        isDragging = false;

        if (isTargeted)
        {
            CombatArrow.Instance?.Hide();
            Entity target = GetEntityUnderPointer(eventData);
            if (target != null)
                TryPlayCard(target);
            else
                ReturnToHand(); // no valid target hit — cancel
        }
        else
        {
            ReturnToHand();
            float normY = eventData.position.y / Screen.height;
            if (normY >= playThresholdY)
                TryPlayCard(null); // untargeted card determines its own targets
        }
    }

    #endregion

    #region Card Play

    private void TryPlayCard(Entity explicitTarget)
    {
        if (cardData == null || CombatManager.Instance == null) return;

        var player = Object.FindAnyObjectByType<PlayerEntity>();
        var targets = BuildTargetList(explicitTarget, player);
        var context = new EffectContext(player, targets);

        bool played = CombatManager.Instance.TryPlayCard(cardData, context);
        if (played)
            HandLayoutController.Instance?.RemoveCard(this); // layout removes and destroys
        // if not played (e.g. not enough energy) the card stays in hand silently
    }

    private System.Collections.Generic.List<Entity> BuildTargetList(Entity explicitTarget, PlayerEntity player)
    {
        var list = new System.Collections.Generic.List<Entity>();

        TargetType targetType = TargetType.SingleEnemy; // fallback default
        foreach (var entry in cardData.effects)
        {
            targetType = entry.parameters.targetType;
            break; // use the first effect's target type to determine targeting
        }

        switch (targetType)
        {
            case TargetType.Self:
                if (player != null) list.Add(player);
                break;

            case TargetType.SingleEnemy:
                if (explicitTarget != null) list.Add(explicitTarget);
                break;

            case TargetType.AllEnemies:
                list.AddRange(FindObjectsByType<Entity>(FindObjectsSortMode.None));
                list.RemoveAll(e => e is PlayerEntity); // keep only enemies
                break;

            case TargetType.RandomEnemy:
                var enemies = new System.Collections.Generic.List<Entity>(
                    FindObjectsByType<Entity>(FindObjectsSortMode.None));
                enemies.RemoveAll(e => e is PlayerEntity);
                if (enemies.Count > 0)
                    list.Add(enemies[Random.Range(0, enemies.Count)]);
                break;

            case TargetType.AllCharacters:
                list.AddRange(FindObjectsByType<Entity>(FindObjectsSortMode.None));
                break;
        }

        return list;
    }

    #endregion

    #region Examine Panel

    private void OpenExaminePanel()
    {
        // Raise an event — the UI layer spawns the examine panel
        GameEvents.Raise(new OnCardExamineRequestedEvent(cardData));
    }

    #endregion

    #region Helpers

    private bool IsTargetedCard(CardData data)
    {
        foreach (var entry in data.effects)
            if (entry.parameters.targetType == TargetType.SingleEnemy)
                return true;
        return false;
    }

    private void MoveCardWithMouse(PointerEventData eventData)
    {
        if (rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        cardRect.localPosition = localPoint;
    }

    private void ReturnToHand()
    {
        // HandLayoutController repositions all cards; just reset local position
        if (HandLayoutController.Instance != null)
            HandLayoutController.Instance.RefreshLayout();
    }

    private Entity GetEntityUnderPointer(PointerEventData eventData)
    {
        foreach (var result in eventData.hovered)
        {
            var entity = result.GetComponent<Entity>();
            if (entity != null && entity.IsEnemy) return entity;
        }
        return null;
    }

    private string BuildDescription(CardData data)
    {
        // Falls back to the first effect's descriptionFormat if no override exists
        if (data.effects == null || data.effects.Count == 0) return string.Empty;
        return data.effects[0].effect != null ? data.effects[0].effect.descriptionFormat : string.Empty;
    }

    #endregion

    #region Transitions

    private void StopAllTransitions()
    {
        if (widthCoroutine != null) StopCoroutine(widthCoroutine);
        if (heightCoroutine != null) StopCoroutine(heightCoroutine);
        if (descHeightCoroutine != null) StopCoroutine(descHeightCoroutine);
        if (descAlphaCoroutine != null) StopCoroutine(descAlphaCoroutine);
    }

    private IEnumerator TweenWidth(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetCardSize(Mathf.Lerp(from, to, elapsed / duration), cardRect.sizeDelta.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetCardSize(to, cardRect.sizeDelta.y);
    }

    private IEnumerator TweenHeight(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetCardSize(cardRect.sizeDelta.x, Mathf.Lerp(from, to, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetCardSize(cardRect.sizeDelta.x, to);
    }

    private IEnumerator TweenDescHeight(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetDescriptionHeight(Mathf.Lerp(from, to, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetDescriptionHeight(to);
    }

    private IEnumerator TweenAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            SetDescriptionAlpha(Mathf.Lerp(from, to, elapsed / duration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetDescriptionAlpha(to);
    }

    #endregion

    #region Direct Setters

    private void SetCardSize(float width, float height)
    {
        cardRect.sizeDelta = new Vector2(width, height);
    }

    private void SetDescriptionHeight(float height)
    {
        Vector2 size = descriptionContainer.sizeDelta;
        size.y = height;
        descriptionContainer.sizeDelta = size;
    }

    private void SetDescriptionAlpha(float alpha)
    {
        Color c = descriptionText.color;
        descriptionText.color = new Color(c.r, c.g, c.b, alpha);
    }

    #endregion
}

#region Card Examine Event

public struct OnCardExamineRequestedEvent
{
    public CardData card;
    public OnCardExamineRequestedEvent(CardData c) => card = c;
}

#endregion