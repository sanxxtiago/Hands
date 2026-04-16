using System.Collections;
using UnityEngine;

public class DotBehaviour : MonoBehaviour
{
    public float lifeTime = 3;
    public float hitRadius = .1f;
    public float trackingRadius = .4f;

    public bool IsHitted { get; set; }
    public bool IsTrackable { get; set; }
    public PathData Path { get; set; }

    public void Hit()
    {
        if (IsHitted) return;

        Debug.Log("Hitted!");
        IsHitted = true;

        if (IsTrackable)
        {
            FollowPath();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {

    }

    public void SetPath(PathData path)
    {
        Path = path;
    }

    public void FollowPath()
    {
        StartCoroutine(StartPathFollowing());
    }

    IEnumerator StartPathFollowing()
    {
        if (Path.curves == null || Path.curves.Count == 0)
            yield break;

        Vector3 lastPoint = Vector3.zero;
        for (int i = 0; i < Path.curves.Count; i++)
        {
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / Path.duration;
                lastPoint = BezierCurve.BezierCurvePosition(Path.curves[i], t);
                transform.position = lastPoint;
                yield return null;
            }
            if (IsPathComplete(lastPoint))
            {
                Destroy(gameObject,2f);
            }
        }
    }

    public bool IsPathComplete(Vector3 lastPoint)
    {
        if (transform.position == lastPoint)
        {
            return true;
        }
        return false;
    }
}