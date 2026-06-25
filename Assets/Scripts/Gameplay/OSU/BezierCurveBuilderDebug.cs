using UnityEngine;

public class BezierCurveBuilderDebug : MonoBehaviour
{
    public PathData Path;
    void OnDrawGizmos()
    {
        if (Path == null || Path.curves == null)
            return;

        Gizmos.color = Color.green;

        foreach (var curve in Path.curves)
        {
            BezierCurve.DrawCurve(curve);
        }

        if (Path.curves.Count > 0)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(
                Path.curves[0].controlPoints[0],
                0.03f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(
                Path.curves[^1].controlPoints[3],
                0.03f);
        }
    }
}

