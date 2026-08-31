using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class ExpositionStatusIcon : MaskableGraphic
{
    private static readonly Color WarningColor = new(1f, 0.75686276f, 0.02745098f, 1f);
    private static readonly Color SuccessColor = new(0.13333334f, 0.77254903f, 0.36862746f, 1f);
    private static readonly Color ContrastColor = new(0.06666667f, 0.101960786f, 0.1764706f, 1f);
    private bool isWarning;

    public void SetWarning(bool value)
    {
        if (isWarning == value) return;
        isWarning = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        float radius = Mathf.Min(rect.width, rect.height) * 0.42f;
        if (isWarning) DrawWarning(vertexHelper, center, radius);
        else DrawSuccess(vertexHelper, center, radius);
    }

    private static void DrawWarning(VertexHelper v, Vector2 c, float r)
    {
        AddTriangle(v, c + Vector2.up * r, c + new Vector2(-r, -r * .82f), c + new Vector2(r, -r * .82f), WarningColor);
        float w = Mathf.Max(1.5f, r * .18f);
        AddSegment(v, c + Vector2.up * r * .42f, c + Vector2.down * r * .14f, w, ContrastColor);
        AddCircle(v, c + Vector2.down * r * .45f, w * .55f, 8, ContrastColor);
    }

    private static void DrawSuccess(VertexHelper v, Vector2 c, float r)
    {
        AddCircle(v, c, r, 20, SuccessColor);
        float w = Mathf.Max(1.5f, r * .16f);
        AddSegment(v, c + new Vector2(-r * .5f, 0f), c + new Vector2(-r * .12f, -r * .35f), w, Color.white);
        AddSegment(v, c + new Vector2(-r * .12f, -r * .35f), c + new Vector2(r * .55f, r * .42f), w, Color.white);
    }

    private static void AddTriangle(VertexHelper v, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int i = v.currentVertCount;
        AddVertex(v, a, color); AddVertex(v, b, color); AddVertex(v, c, color);
        v.AddTriangle(i, i + 1, i + 2);
    }

    private static void AddCircle(VertexHelper v, Vector2 c, float r, int segments, Color color)
    {
        int center = v.currentVertCount;
        AddVertex(v, c, color);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            AddVertex(v, c + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r, color);
            if (i > 0) v.AddTriangle(center, center + i, center + i + 1);
        }
    }

    private static void AddSegment(VertexHelper v, Vector2 start, Vector2 end, float width, Color color)
    {
        Vector2 perpendicular = new Vector2(-(end - start).normalized.y, (end - start).normalized.x) * width * .5f;
        int i = v.currentVertCount;
        AddVertex(v, start + perpendicular, color); AddVertex(v, start - perpendicular, color);
        AddVertex(v, end - perpendicular, color); AddVertex(v, end + perpendicular, color);
        v.AddTriangle(i, i + 1, i + 2); v.AddTriangle(i, i + 2, i + 3);
    }

    private static void AddVertex(VertexHelper v, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color; vertex.position = position; vertex.uv0 = Vector2.zero;
        v.AddVert(vertex);
    }
}
