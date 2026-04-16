using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public PathData Path;
    void OnDrawGizmos()
    {

        // if (Path == null || Path.curves == null || Path.curves.Count == 0)
        //     return;

        // Vector3 gizmoPos = Vector3.zero;
        // for (int i = 0; i < Path.curves.Count; i++)
        // {
        //     float t = 0;
        //     while (t < 1)
        //     {
        //         t += Time.deltaTime / Path.duration;
        //         gizmoPos = BezierCurvePosition(Path.curves[i], t);
        //     }
        //     Gizmos.DrawSphere(gizmoPos, 0.02f);
        // }
    }

    public static Vector3 BezierCurvePosition(BezierCurveData data, float step)
    {
        return Mathf.Pow(1 - step, 3) * data.controlPoints[0] + 3 * Mathf.Pow(1 - step, 2) * step * data.controlPoints[1] + 3 * (1 - step) * Mathf.Pow(step, 2) * data.controlPoints[2] + Mathf.Pow(step, 3) * data.controlPoints[3];
    }
}
