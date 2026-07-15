using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarChart : MaskableGraphic
{
    [Header("Shape")]
    [SerializeField] private float radius = 120f;

    [Header("Outline")]
    [SerializeField] private float lineThickness = 3f;

    [Header("Points")]
    [SerializeField] private float pointRadius = 6f;
    [SerializeField] private int pointSegments = 16;

    [Header("Colors")]
    [SerializeField] private Color fillColor = new(0, 1, 1, .20f);
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private Color pointColor = Color.white;

    [SerializeField] private float[] values = { 1, 1, 1 };

    public void SetValues(params float[] newValues)
    {
        values = newValues;
        SetVerticesDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (values == null || values.Length < 3)
            values = new float[] { 1, 1, 1 };

        SetVerticesDirty();
    }
#endif

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (values == null || values.Length < 3)
            return;

        Vector2[] points = ChartMath.BuildRadar(
            values,
            radius);

        ChartDrawing.DrawFilledPolygon(
            vh,
            points,
            fillColor);

        ChartDrawing.DrawPolyline(
            vh,
            points,
            lineThickness,
            lineColor,
            true);

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