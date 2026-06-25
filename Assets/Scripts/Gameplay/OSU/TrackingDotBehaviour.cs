using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackingDotBehaviour : DotBehaviour
{
    public float trackingRadius = .4f;
    public float followRadius = 0.5f;

    public PathData path;

    public bool IsFollowing { get; private set; }

    public float timeOutside = 0f;

    [SerializeField] private Vector3 startMarker;
    [SerializeField] private Vector3 endMarker;
    [SerializeField] private float pathZOffset = 0.005f;
    private LineRenderer pathInstance;
    void Start()
    {
        DrawPath();
    }
    public void SetPath(PathData path, LineRenderer pathPrefab)
    {
        this.path = path;
        pathInstance = Instantiate(pathPrefab);
        startMarker = path.curves[0].controlPoints[0];
        endMarker = path.curves[^1].controlPoints[3];
        pathInstance.transform.position += Vector3.forward * 0.01f;
        DrawPath();
    }
    public override void Hit()
    {
        if (IsHitted) return;

        IsHitted = true;

        StartCoroutine(StartPathFollowing());
    }

    IEnumerator StartPathFollowing()
    {
        if (path == null || path.curves == null || path.curves.Count == 0)
        {
            Fail();
            yield break;
        }

        foreach (var curve in path.curves)
        {
            float curveLength =
                BezierCurve.GetCurveLength(curve);

            var arcTable =
                BezierCurve.BuildArcLengthTable(curve);

            float travelledDistance = 0;

            while (travelledDistance < curveLength)
            {
                travelledDistance +=
                    path.speed * Time.deltaTime;

                float t =
                    BezierCurve.GetTAtDistance(
                        arcTable,
                        travelledDistance);

                transform.position =
                    BezierCurve.BezierCurvePosition(
                        curve,
                        t);

                yield return null;
            }

            transform.position =
                BezierCurve.BezierCurvePosition(
                    curve,
                    1f);
        }

        yield return new WaitForSeconds(1.5f);
        Destroy(pathInstance.gameObject);
        Complete();

        Destroy(gameObject, .25f);
    }

    public void SetTrackingState(bool isFollowing)
    {
        IsFollowing = isFollowing;

        if (bg != null)
        {
            bg.color = isFollowing
                ? Color.black
                : Color.white;
        }
    }

    public void Track(Vector3 handPosition)
    {
        float dist =
            Vector3.Distance(handPosition, transform.position);

        if (dist <= followRadius)
        {
            timeOutside = 0f;
            SetTrackingState(true);
        }
        else
        {
            timeOutside += Time.deltaTime;
            SetTrackingState(false);

            if (timeOutside > 0.3f)
            {
                Fail();
            }
        }
    }

    private void DrawPath()
    {
        List<Vector3> points = new();

        Vector3 offset = new(0, 0, pathZOffset);

        foreach (var curve in path.curves)
        {
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f;

                points.Add(
                    BezierCurve.BezierCurvePosition(
                        curve,
                        t)
                    + offset);
            }
        }

        pathInstance.positionCount = points.Count;
        pathInstance.SetPositions(points.ToArray());
    }
}