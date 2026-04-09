using UnityEngine;

public class DebugCollider : MonoBehaviour
{
    private BoxCollider col;

    void Awake()
    {
        col = GetComponent<BoxCollider>();
    }
    void Update()
    {
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

}