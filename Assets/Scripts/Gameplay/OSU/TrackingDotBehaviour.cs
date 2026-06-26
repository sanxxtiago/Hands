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

    [SerializeField] private float pathZOffset = 0.005f;
    [SerializeField] private float trailDistance = 0.03f;
    [SerializeField] private int samplesPerCurve = 30;

    private LineRenderer pathInstance;

    private readonly List<Vector3> pathPoints = new();
    private readonly List<float> pointDistances = new();

    public void SetPath(PathData path, LineRenderer pathPrefab)
    {
        this.path = path;

        pathInstance = Instantiate(pathPrefab);
        pathInstance.transform.position += Vector3.forward * 0.01f;

        DrawPath();
    }

    public override void Hit()
    {
        if (IsHitted)
            return;

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

        float travelledDistance = 0;

        foreach (var curve in path.curves)
        {
            float curveLength =
                BezierCurve.GetCurveLength(curve);

            List<CurveSample> arcTable =
                BezierCurve.BuildArcLengthTable(curve);

            while (travelledDistance < curveLength)
            {
                travelledDistance +=
                    path.speed * Time.deltaTime;

                float localDistance =
                    Mathf.Min(travelledDistance, curveLength);

                float t =
                    BezierCurve.GetTAtDistance(
                        arcTable,
                        localDistance);

                transform.position =
                    BezierCurve.BezierCurvePosition(curve, t);

                UpdateVisiblePath(localDistance);

                yield return null;
            }

            travelledDistance = 0;

            transform.position =
                BezierCurve.BezierCurvePosition(curve, 1f);
        }

        yield return new WaitForSeconds(.75f);

        Destroy(pathInstance.gameObject);

        Complete();

        Destroy(gameObject, .25f);
    }

    public void SetTrackingState(bool isFollowing)
    {
        IsFollowing = isFollowing;

        if (bg != null)
        {
            Color color =
                isFollowing ? Color.green : Color.white;

            color.a = 0.6f;

            bg.color = color;
        }
    }

    public void Track(Vector3 handPosition)
    {
        float dist =
            Vector3.Distance(handPosition, transform.position);

        if (dist <= followRadius)
        {
            timeOutside = 0;
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
        pathPoints.Clear();
        pointDistances.Clear();

        Vector3 offset = new(0, 0, pathZOffset);

        float accumulatedDistance = 0;
        Vector3? previous = null;

        foreach (var curve in path.curves)
        {
            for (int i = 0; i <= samplesPerCurve; i++)
            {
                float t = i / (float)samplesPerCurve;

                Vector3 point =
                    BezierCurve.BezierCurvePosition(curve, t)
                    + offset;

                pathPoints.Add(point);

                if (previous.HasValue)
                {
                    accumulatedDistance +=
                        Vector3.Distance(previous.Value, point);
                }

                pointDistances.Add(accumulatedDistance);

                previous = point;
            }
        }

        pathInstance.positionCount = pathPoints.Count;
        pathInstance.SetPositions(pathPoints.ToArray());
    }

    private void UpdateVisiblePath(float travelledDistance)
    {
        float visibleStart =
            Mathf.Max(0, travelledDistance - trailDistance);

        int firstVisible = 0;

        while (firstVisible < pointDistances.Count &&
               pointDistances[firstVisible] < visibleStart)
        {
            firstVisible++;
        }

        int visibleCount =
            pathPoints.Count - firstVisible;

        if (visibleCount <= 0)
        {
            pathInstance.positionCount = 0;
            return;
        }

        pathInstance.positionCount = visibleCount;

        for (int i = 0; i < visibleCount; i++)
        {
            pathInstance.SetPosition(
                i,
                pathPoints[firstVisible + i]);
        }
    }
}