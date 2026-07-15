using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineChartGrid : MaskableGraphic
{
    [Header("Grid")]
    [SerializeField] private int horizontalLines = 5;
    [SerializeField] private int verticalLines = 10;

    [Header("Style")]
    [SerializeField] private float lineThickness = 2f;
    [SerializeField] private float firstLineThickness = 3f;
    [SerializeField] private Color axisColor = Color.white;
    [SerializeField] private Color gridColor = Color.white;


#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        horizontalLines = Mathf.Max(2, horizontalLines);
        verticalLines = Mathf.Max(2, verticalLines);

        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;

        DrawHorizontalDivisions(vh, rect);
        DrawVerticalDivisions(vh, rect);
    }

    private void DrawHorizontalDivisions(VertexHelper vh, Rect rect)
    {
        float step = rect.height / (horizontalLines - 1);

        for (int i = 0; i < horizontalLines; i++)
        {
            float y = rect.yMin + step * i;

            float thickness = i == 0
                ? firstLineThickness
                : lineThickness;

            ChartDrawing.DrawLine(
                vh,
                new Vector2(rect.xMin, y),
                new Vector2(rect.xMax, y),
                thickness,
                i == 0 ? axisColor : gridColor);
        }
    }

    private void DrawVerticalDivisions(VertexHelper vh, Rect rect)
    {
        float step = rect.width / (verticalLines - 1);

        for (int i = 0; i < verticalLines; i++)
        {
            float x = rect.xMin + step * i;

            float thickness = i == 0
                ? firstLineThickness
                : lineThickness;

            ChartDrawing.DrawLine(
                vh,
                new Vector2(x, rect.yMin),
                new Vector2(x, rect.yMax),
                thickness,
                i == 0 ? axisColor : gridColor);
        }
    }
}