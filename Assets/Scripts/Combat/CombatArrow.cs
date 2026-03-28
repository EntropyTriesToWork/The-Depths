using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders a dotted curved bezier arrow from a card to a target or the mouse.
/// Dots are pooled SpriteRenderer GameObjects spaced evenly along the curve.
/// No LineRenderer required — assign a circle/dot sprite in the inspector.
///
/// Setup:
///   1. Create an empty GameObject in the combat scene, add this component.
///   2. Assign a small circle Sprite to dotSprite.
///   3. Assign a triangle/arrow Sprite to the arrowHead child, or leave null.
///   4. Tune dotCount, dotSpacing, and controlHeight to taste.
/// </summary>
public class CombatArrow : MonoBehaviour
{
    #region Singleton

    public static CombatArrow Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    #endregion

    #region Inspector

    [Header("Bezier")]
    [SerializeField] private float controlHeight = 150f; // arc peak height

    [Header("Dots")]
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private int dotCount = 20;   // max dots in the pool
    [SerializeField] private float dotSize = 0.15f;
    [SerializeField] private float dotSpacing = 0.06f; // t-space gap between dots (0–1)
    [SerializeField] private Color dotColor = new Color(1f, 0.85f, 0.1f, 1f);

    [Header("Dot Fade")]
    [Tooltip("Dots near the tip are more opaque. 0 = uniform, 1 = full fade.")]
    [SerializeField] private float tipAlpha = 1f;
    [SerializeField] private float tailAlpha = 0.3f;

    [Header("Arrowhead")]
    [SerializeField] private GameObject arrowHead; // small child sprite, rotated to curve tangent

    [Header("Animation")]
    [Tooltip("How fast the dots march along the curve (t-units per second).")]
    [SerializeField] private float marchSpeed = 0.4f;

    #endregion

    #region Private State

    private readonly List<SpriteRenderer> dotPool = new();
    private Vector3 originPoint;
    private Vector3 targetPoint;
    private bool isVisible;
    private float marchOffset;   // scrolls 0..dotSpacing each cycle to animate march
    private Camera uiCamera;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        uiCamera = Camera.main;
        BuildPool();
        SetVisible(false);
    }

    private void Update()
    {
        if (!isVisible) return;

        marchOffset = (marchOffset + marchSpeed * Time.deltaTime) % dotSpacing; // animate march

        Vector3 mouseWorld = uiCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, uiCamera.nearClipPlane + 1f));
        targetPoint = mouseWorld;

        PlaceDots();
        UpdateArrowHead();
    }

    #endregion

    #region Public API

    public void Show(Vector3 worldOrigin)
    {
        originPoint = worldOrigin;
        targetPoint = worldOrigin;
        marchOffset = 0f;
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    #endregion

    #region Private — Pool

    private void BuildPool()
    {
        for (int i = 0; i < dotCount; i++)
        {
            var go = new GameObject($"ArrowDot_{i}");
            go.transform.SetParent(transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.color = dotColor;
            sr.sortingOrder = 10; // render above most combat elements
            go.transform.localScale = Vector3.one * dotSize;

            dotPool.Add(sr);
            go.SetActive(false);
        }
    }

    #endregion

    #region Private — Drawing

    private void PlaceDots()
    {
        Vector3 control = ControlPoint();

        int placed = 0;
        float t = marchOffset; // start offset animates the march

        for (int i = 0; i < dotPool.Count; i++)
        {
            if (t > 1f - dotSpacing) // stop before the arrowhead tip
            {
                dotPool[i].gameObject.SetActive(false);
                continue;
            }

            Vector3 pos = QuadraticBezier(originPoint, control, targetPoint, t);
            float alpha = Mathf.Lerp(tailAlpha, tipAlpha, t); // fade from tail to tip

            dotPool[i].gameObject.SetActive(true);
            dotPool[i].transform.position = pos;

            Color c = dotColor;
            dotPool[i].color = new Color(c.r, c.g, c.b, alpha);

            t += dotSpacing;
            placed++;
        }
    }

    private void UpdateArrowHead()
    {
        if (arrowHead == null) return;

        arrowHead.transform.position = targetPoint;

        Vector3 control = ControlPoint();
        float tPrev = Mathf.Max(0f, 1f - dotSpacing); // tangent from near-tip
        Vector3 prev = QuadraticBezier(originPoint, control, targetPoint, tPrev);
        Vector3 dir = (targetPoint - prev).normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrowHead.transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;
        foreach (var dot in dotPool) dot.gameObject.SetActive(visible && false); // PlaceDots enables individually
        if (!visible)
        {
            foreach (var dot in dotPool) dot.gameObject.SetActive(false);
        }
        if (arrowHead != null) arrowHead.SetActive(visible);
    }

    private Vector3 ControlPoint() =>
        (originPoint + targetPoint) * 0.5f + Vector3.up * controlHeight;

    private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    #endregion
}