using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class LineChart : MaskableGraphic
{
    [Header("Style")]
    [SerializeField] private float lineThickness = 3f;

    [SerializeField] private float pointRadius = 5f;

    [SerializeField] private int pointSegments = 12;

    [SerializeField] private Color lineColor = Color.white;

    [SerializeField] private Color pointColor = Color.white;

    [SerializeField]
    private float[] values =
    {
        .2f,
        .4f,
        .3f,
        .7f,
        .5f,
        .8f,
        .6f,
        .9f,
        .7f,
        .8f
    };

    public void SetValues(params float[] newValues)
    {
        values = newValues;
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (values == null || values.Length == 0)
            return;

        Rect rect = rectTransform.rect;

        Vector2[] points = ChartMath.BuildLine(
            values,
            rect);

        ChartDrawing.DrawPolyline(
            vh,
            points,
            lineThickness,
            lineColor);

        foreach (Vector2 point in points)
        {
            ChartDrawing.DrawCircle(
                vh,
                point,
                pointRadius,
                pointSegments,
                pointColor);
        }
    }
}