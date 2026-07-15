using UnityEngine;
using UnityEngine.UI;

public static class ChartDrawing
{
    #region Vertex

    public static void AddVertex(VertexHelper vh, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;

        vh.AddVert(vertex);
    }

    #endregion

    #region Line

    public static void DrawLine(
        VertexHelper vh,
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new(-direction.y, direction.x);

        normal *= thickness * .5f;

        int index = vh.currentVertCount;

        AddVertex(vh, start - normal, color);
        AddVertex(vh, start + normal, color);
        AddVertex(vh, end + normal, color);
        AddVertex(vh, end - normal, color);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }

    #endregion

    #region Polyline

    public static void DrawPolyline(
        VertexHelper vh,
        Vector2[] points,
        float thickness,
        Color color,
        bool closed = false)
    {
        if (points == null || points.Length < 2)
            return;

        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(
                vh,
                points[i],
                points[i + 1],
                thickness,
                color);
        }

        if (closed)
        {
            DrawLine(
                vh,
                points[^1],
                points[0],
                thickness,
                color);
        }
    }

    #endregion

    #region Filled Polygon

    public static void DrawFilledPolygon(
        VertexHelper vh,
        Vector2[] points,
        Color color)
    {
        if (points == null || points.Length < 3)
            return;

        int start = vh.currentVertCount;

        AddVertex(vh, Vector2.zero, color);

        foreach (Vector2 point in points)
            AddVertex(vh, point, color);

        for (int i = 1; i <= points.Length; i++)
        {
            int next = (i == points.Length) ? 1 : i + 1;

            vh.AddTriangle(
                start,
                start + i,
                start + next);
        }
    }

    #endregion

    #region Circle

    public static void DrawCircle(
        VertexHelper vh,
        Vector2 center,
        float radius,
        int segments,
        Color color)
    {
        if (segments < 3)
            segments = 3;

        int start = vh.currentVertCount;

        AddVertex(vh, center, color);

        float step = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = step * i * Mathf.Deg2Rad;

            Vector2 point = center + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)) * radius;

            AddVertex(vh, point, color);
        }

        for (int i = 1; i <= segments; i++)
        {
            int next = (i == segments) ? 1 : i + 1;

            vh.AddTriangle(
                start,
                start + i,
                start + next);
        }
    }

    #endregion
}