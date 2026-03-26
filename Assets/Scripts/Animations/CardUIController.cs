using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardUIController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private RectTransform descriptionContainer;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image artImage;

    [Header("Card Size")]
    [SerializeField] private float normalHeight = 100f;
    [SerializeField] private float hoverHeight = 150f;

    [Header("Description Box")]
    [SerializeField] private float descriptionNormalHeight = 0f;
    [SerializeField] private float descriptionHoverHeight = 40f; 

    [Header("Name Font Size")]
    [SerializeField] private float normalNameFontSize = 36f;
    [SerializeField] private float hoverNameFontSize = 28f;

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 0.2f;

    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent onClick;

    private Coroutine heightCoroutine;
    private Coroutine descriptionHeightCoroutine;
    private Coroutine nameSizeCoroutine;
    private Coroutine descriptionAlphaCoroutine;

    private void Awake()
    {
        SetCardHeight(normalHeight);
        SetDescriptionHeight(descriptionNormalHeight);
        SetDescriptionAlpha(0f);
        nameText.fontSize = normalNameFontSize;
    }
    public void Initialize(string cardName, string cardDescription, Sprite artSprite)
    {
        nameText.text = cardName;
        descriptionText.text = cardDescription;
        if (artImage != null) artImage.sprite = artSprite;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllTransitions();
        heightCoroutine = StartCoroutine(TweenCardHeight(normalHeight, hoverHeight, transitionDuration));
        descriptionHeightCoroutine = StartCoroutine(TweenDescriptionHeight(descriptionNormalHeight, descriptionHoverHeight, transitionDuration));
        nameSizeCoroutine = StartCoroutine(TweenFontSize(nameText, normalNameFontSize, hoverNameFontSize, transitionDuration));
        descriptionAlphaCoroutine = StartCoroutine(TweenAlpha(descriptionText, 0f, 1f, transitionDuration));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllTransitions();
        heightCoroutine = StartCoroutine(TweenCardHeight(hoverHeight, normalHeight, transitionDuration));
        descriptionHeightCoroutine = StartCoroutine(TweenDescriptionHeight(descriptionHoverHeight, descriptionNormalHeight, transitionDuration));
        nameSizeCoroutine = StartCoroutine(TweenFontSize(nameText, hoverNameFontSize, normalNameFontSize, transitionDuration));
        descriptionAlphaCoroutine = StartCoroutine(TweenAlpha(descriptionText, 1f, 0f, transitionDuration));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }
    private void StopAllTransitions()
    {
        if (heightCoroutine != null) StopCoroutine(heightCoroutine);
        if (descriptionHeightCoroutine != null) StopCoroutine(descriptionHeightCoroutine);
        if (nameSizeCoroutine != null) StopCoroutine(nameSizeCoroutine);
        if (descriptionAlphaCoroutine != null) StopCoroutine(descriptionAlphaCoroutine);
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

    private IEnumerator TweenFontSize(TextMeshProUGUI target, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            target.fontSize = Mathf.Lerp(from, to, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.fontSize = to;
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