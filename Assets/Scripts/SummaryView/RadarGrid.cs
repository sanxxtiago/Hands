using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class RadarGrid : MaskableGraphic
{
    [SerializeField] private float radius = 100f;
    [SerializeField] private float thickness = 4f;

    [Range(3, 256)]
    [SerializeField] private int segments = 64;
    [SerializeField]
    [Range(0, 90)]
    private float rotationOffset = 30f;

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

        DrawRing(vh, radius);
        DrawRing(vh, radius * 0.75f);
        DrawRing(vh, radius * 0.50f);
        DrawRing(vh, radius * 0.25f);
    }

    private void DrawRing(VertexHelper vh, float ringRadius)
    {
        float innerRadius = ringRadius - thickness * 0.5f;
        float outerRadius = ringRadius + thickness * 0.5f;

        float angleStep = Mathf.PI * 2f / segments;

        int startIndex = vh.currentVertCount;

        for (int i = 0; i < segments; i++)
        {
            float angle = angleStep * i;

            Vector2 dir = new(
                Mathf.Cos(angle),
                Mathf.Sin(angle));

            AddVertex(vh, dir * outerRadius);
            AddVertex(vh, dir * innerRadius);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            int outer0 = startIndex + i * 2;
            int inner0 = outer0 + 1;

            int outer1 = startIndex + next * 2;
            int inner1 = outer1 + 1;

            vh.AddTriangle(outer0, outer1, inner1);
            vh.AddTriangle(outer0, inner1, inner0);
        }
    }

    private void AddVertex(VertexHelper vh, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = position;

        vh.AddVert(vertex);
    }
}