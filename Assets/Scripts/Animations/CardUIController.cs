using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Events;

public class CardUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private RectTransform descriptionContainer;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI energyCostText;
    [SerializeField] private TextMeshProUGUI manaGainText;
    [SerializeField] private Image artImage;

    [SerializeField] private float normalWidth = 100f;
    [SerializeField] private float hoverWidth = 120f;
    [SerializeField] private float normalHeight = 100f;
    [SerializeField] private float hoverHeight = 150f;

    [SerializeField] private float descriptionNormalHeight = 0f;
    [SerializeField] private float descriptionHoverHeight = 40f;

    [SerializeField] private bool descriptionAlwaysVisible = false;

    [SerializeField] private float transitionDuration = 0.2f;

    [SerializeField] private UnityEngine.Events.UnityEvent onClick;

    private Coroutine widthCoroutine;
    private Coroutine heightCoroutine;
    private Coroutine descriptionHeightCoroutine;
    private Coroutine descriptionAlphaCoroutine;

    private void Awake()
    {
        SetCardWidth(normalWidth);
        SetCardHeight(normalHeight);

        if (descriptionAlwaysVisible)
        {
            SetDescriptionHeight(descriptionHoverHeight);
            SetDescriptionAlpha(1f);
        }
        else
        {
            SetDescriptionHeight(descriptionNormalHeight);
            SetDescriptionAlpha(0f);
        }
    }

    public void Initialize(string cardName, string cardDescription, Sprite artSprite, bool descActive, int energyCost, int manaGain, UnityAction OnClickAction)
    {
        nameText.text = cardName;
        descriptionText.text = cardDescription;
        descriptionAlwaysVisible = descActive;
        energyCostText.text = energyCost.ToString();
        manaGainText.text = manaGain.ToString();
        if (artImage != null) artImage.sprite = artSprite;
        if(OnClickAction != null) { onClick.AddListener(OnClickAction); }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllTransitions();

        widthCoroutine = StartCoroutine(TweenCardWidth(normalWidth, hoverWidth, transitionDuration));
        heightCoroutine = StartCoroutine(TweenCardHeight(normalHeight, hoverHeight, transitionDuration));

        if (!descriptionAlwaysVisible)
        {
            descriptionHeightCoroutine = StartCoroutine(TweenDescriptionHeight(descriptionNormalHeight, descriptionHoverHeight, transitionDuration));
            descriptionAlphaCoroutine = StartCoroutine(TweenAlpha(descriptionText, 0f, 1f, transitionDuration));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllTransitions();

        widthCoroutine = StartCoroutine(TweenCardWidth(hoverWidth, normalWidth, transitionDuration));
        heightCoroutine = StartCoroutine(TweenCardHeight(hoverHeight, normalHeight, transitionDuration));

        if (!descriptionAlwaysVisible)
        {
            descriptionHeightCoroutine = StartCoroutine(TweenDescriptionHeight(descriptionHoverHeight, descriptionNormalHeight, transitionDuration));
            descriptionAlphaCoroutine = StartCoroutine(TweenAlpha(descriptionText, 1f, 0f, transitionDuration));
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    private void StopAllTransitions()
    {
        if (widthCoroutine != null) StopCoroutine(widthCoroutine);
        if (heightCoroutine != null) StopCoroutine(heightCoroutine);
        if (descriptionHeightCoroutine != null) StopCoroutine(descriptionHeightCoroutine);
        if (descriptionAlphaCoroutine != null) StopCoroutine(descriptionAlphaCoroutine);
    }

    private IEnumerator TweenCardWidth(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float width = Mathf.Lerp(from, to, t);
            SetCardWidth(width);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetCardWidth(to);
    }

    private IEnumerator TweenCardHeight(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float height = Mathf.Lerp(from, to, t);
            SetCardHeight(height);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetCardHeight(to);
    }

    private IEnumerator TweenDescriptionHeight(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float height = Mathf.Lerp(from, to, t);
            SetDescriptionHeight(height);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetDescriptionHeight(to);
    }

    private IEnumerator TweenAlpha(TextMeshProUGUI target, float from, float to, float duration)
    {
        float elapsed = 0f;
        Color color = target.color;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(from, to, t);
            target.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.color = new Color(color.r, color.g, color.b, to);
    }

    private void SetCardWidth(float width)
    {
        Vector2 size = cardRect.sizeDelta;
        size.x = width;
        cardRect.sizeDelta = size;
    }

    private void SetCardHeight(float height)
    {
        Vector2 size = cardRect.sizeDelta;
        size.y = height;
        cardRect.sizeDelta = size;
    }

    private void SetDescriptionHeight(float height)
    {
        Vector2 size = descriptionContainer.sizeDelta;
        size.y = height;
        descriptionContainer.sizeDelta = size;
    }

    private void SetDescriptionAlpha(float alpha)
    {
        Color color = descriptionText.color;
        descriptionText.color = new Color(color.r, color.g, color.b, alpha);
    }
}