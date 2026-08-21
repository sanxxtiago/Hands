using UnityEngine;

public class BoundingBox : MonoBehaviour
{
    public bool debugBox = false;
    private BoxCollider col;
    public Vector3 min;
    public Vector3 max;
    public static BoundingBox Instance = null;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        col = GetComponent<BoxCollider>();
        min = col.center - col.size * 0.5f;
        max = col.center + col.size * 0.5f;
    }

    void Update()
    {
        if (!debugBox) return;

        DrawBoxCollider(col);
    }

    void DrawBoxCollider(BoxCollider col)
    {
        Vector3 c = col.center;
        Vector3 s = col.size / 2f;

        Vector3[] points = new Vector3[8];

        // calcular esquinas
        for (int i = 0; i < 8; i++)
        {
            points[i] = transform.TransformPoint(
                c + new Vector3(
                    s.x * (i < 4 ? -1 : 1),
                    s.y * ((i % 4 < 2) ? -1 : 1),
                    s.z * ((i % 2 == 0) ? -1 : 1)
                )
            );
        }

        // dibujar líneas
        Debug.DrawLine(points[0], points[1], Color.red);
        Debug.DrawLine(points[1], points[3], Color.red);
        Debug.DrawLine(points[3], points[2], Color.red);
        Debug.DrawLine(points[2], points[0], Color.red);

        Debug.DrawLine(points[4], points[5], Color.red);
        Debug.DrawLine(points[5], points[7], Color.red);
        Debug.DrawLine(points[7], points[6], Color.red);
        Debug.DrawLine(points[6], points[4], Color.red);

        Debug.DrawLine(points[0], points[4], Color.red);
        Debug.DrawLine(points[1], points[5], Color.red);
        Debug.DrawLine(points[2], points[6], Color.red);
        Debug.DrawLine(points[3], points[7], Color.red);
    }

    public Vector3 ClampInsideBox(
        Vector3 worldPosition,
        Transform objectTransform,
        Collider[] objectColliders)
    {
        if (objectTransform == null || objectColliders == null || objectColliders.Length == 0)
            return ClampPointInsideBox(worldPosition);

        Vector3 objectLocalPosition = transform.InverseTransformPoint(objectTransform.position);
        Vector3 minOffset = Vector3.zero;
        Vector3 maxOffset = Vector3.zero;
        bool hasBounds = false;

        foreach (Collider objectCollider in objectColliders)
        {
            if (objectCollider == null || !objectCollider.enabled)
                continue;

            Bounds worldBounds = objectCollider.bounds;
            Vector3 boundsMin = worldBounds.min;
            Vector3 boundsMax = worldBounds.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 worldCorner = new Vector3(
                            x == 0 ? boundsMin.x : boundsMax.x,
                            y == 0 ? boundsMin.y : boundsMax.y,
                            z == 0 ? boundsMin.z : boundsMax.z);

                        Vector3 localOffset =
                            transform.InverseTransformPoint(worldCorner) - objectLocalPosition;

                        if (!hasBounds)
                        {
                            minOffset = localOffset;
                            maxOffset = localOffset;
                            hasBounds = true;
                        }
                        else
                        {
                            minOffset = Vector3.Min(minOffset, localOffset);
                            maxOffset = Vector3.Max(maxOffset, localOffset);
                        }
                    }
                }
            }
        }

        if (!hasBounds)
            return ClampPointInsideBox(worldPosition);

        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos.x = Mathf.Clamp(localPos.x, min.x - minOffset.x, max.x - maxOffset.x);
        localPos.y = Mathf.Clamp(localPos.y, min.y - minOffset.y, max.y - maxOffset.y);
        localPos.z = Mathf.Clamp(localPos.z, min.z - minOffset.z, max.z - maxOffset.z);

        return transform.TransformPoint(localPos);
    }

    private Vector3 ClampPointInsideBox(Vector3 worldPosition)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        localPos.x = Mathf.Clamp(localPos.x, min.x, max.x);
        localPos.y = Mathf.Clamp(localPos.y, min.y, max.y);
        localPos.z = Mathf.Clamp(localPos.z, min.z, max.z);

        return transform.TransformPoint(localPos);
    }
}
