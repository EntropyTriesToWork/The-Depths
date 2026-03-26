using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class FlexibleGridLayout : UIBehaviour, ILayoutGroup, ILayoutController, ILayoutElement
{
    [SerializeField] private int columns = 3;
    [SerializeField] private Vector2 spacing = Vector2.zero;
    [SerializeField] private RectOffset padding = new RectOffset();
    [SerializeField] private GridLayoutGroup.Corner startCorner = GridLayoutGroup.Corner.UpperLeft;
    [SerializeField] private GridLayoutGroup.Axis startAxis = GridLayoutGroup.Axis.Horizontal;
    [SerializeField] private TextAnchor childAlignment = TextAnchor.UpperLeft;

    private RectTransform rectTransform;
    private List<RectTransform> children = new List<RectTransform>();
    private float[] columnWidths;
    private float[] rowHeights;
    private float totalWidth;
    private float totalHeight;
    private bool isDirty = true;

    protected override void Awake()
    {
        base.Awake();
        rectTransform = GetComponent<RectTransform>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetDirty();
    }
    private void OnChildRectTransformDimensionsChange()
    {
        SetDirty();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        SetDirty();
    }

    public void SetLayoutHorizontal()
    {
        if (isDirty) ComputeLayout();
        PositionChildren();
    }

    public void SetLayoutVertical()
    {
        if (isDirty) ComputeLayout();
        PositionChildren();
    }

    public void CalculateLayoutInputHorizontal()
    {
        if (isDirty) ComputeLayout();
    }

    public void CalculateLayoutInputVertical()
    {
        if (isDirty) ComputeLayout();
    }

    public float minWidth => totalWidth;
    public float preferredWidth => totalWidth;
    public float flexibleWidth => -1;
    public float minHeight => totalHeight;
    public float preferredHeight => totalHeight;
    public float flexibleHeight => -1;
    public int layoutPriority => 0;

    private void ComputeLayout()
    {
        if (rectTransform == null) return;

        children.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null) children.Add(rt);
            }
        }

        if (children.Count == 0)
        {
            totalWidth = 0;
            totalHeight = 0;
            columnWidths = null;
            rowHeights = null;
            isDirty = false;
            return;
        }

        int rows = Mathf.CeilToInt((float)children.Count / columns);
        columnWidths = new float[columns];
        rowHeights = new float[rows];

        for (int i = 0; i < columns; i++) columnWidths[i] = 0f;
        for (int i = 0; i < rows; i++) rowHeights[i] = 0f;

        for (int i = 0; i < children.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector2 size = children[i].rect.size;
            if (size.x > columnWidths[col]) columnWidths[col] = size.x;
            if (size.y > rowHeights[row]) rowHeights[row] = size.y;
        }

        totalWidth = padding.left + padding.right;
        for (int i = 0; i < columns; i++) totalWidth += columnWidths[i] + spacing.x;
        totalWidth -= spacing.x;

        totalHeight = padding.top + padding.bottom;
        for (int i = 0; i < rows; i++) totalHeight += rowHeights[i] + spacing.y;
        totalHeight -= spacing.y;

        isDirty = false;
    }

    private void PositionChildren()
    {
        if (children.Count == 0 || columnWidths == null || rowHeights == null) return;

        int rows = rowHeights.Length;

        Vector2 startPos = Vector2.zero;

        switch (childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.MiddleLeft:
            case TextAnchor.LowerLeft:
                startPos.x = padding.left;
                break;
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                startPos.x = (rectTransform.rect.width - totalWidth) * 0.5f;
                break;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                startPos.x = rectTransform.rect.width - totalWidth - padding.right;
                break;
        }

        switch (childAlignment)
        {
            case TextAnchor.UpperLeft:
            case TextAnchor.UpperCenter:
            case TextAnchor.UpperRight:
                startPos.y = -padding.top;
                break;
            case TextAnchor.MiddleLeft:
            case TextAnchor.MiddleCenter:
            case TextAnchor.MiddleRight:
                startPos.y = -(rectTransform.rect.height - totalHeight) * 0.5f;
                break;
            case TextAnchor.LowerLeft:
            case TextAnchor.LowerCenter:
            case TextAnchor.LowerRight:
                startPos.y = -rectTransform.rect.height + totalHeight + padding.bottom;
                break;
        }

        for (int i = 0; i < children.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            int actualCol = col;
            int actualRow = row;

            if (startCorner == GridLayoutGroup.Corner.UpperRight)
                actualCol = columns - 1 - col;
            if (startCorner == GridLayoutGroup.Corner.LowerLeft)
                actualRow = rows - 1 - row;
            if (startCorner == GridLayoutGroup.Corner.LowerRight)
            {
                actualCol = columns - 1 - col;
                actualRow = rows - 1 - row;
            }

            float x = startPos.x;
            for (int c = 0; c < actualCol; c++)
                x += columnWidths[c] + spacing.x;

            float y = startPos.y;
            for (int r = 0; r < actualRow; r++)
                y -= rowHeights[r] + spacing.y;

            children[i].anchoredPosition = new Vector2(x, y);
        }
    }

    public void SetDirty()
    {
        isDirty = true;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    protected override void OnDidApplyAnimationProperties()
    {
        base.OnDidApplyAnimationProperties();
        SetDirty();
    }
}