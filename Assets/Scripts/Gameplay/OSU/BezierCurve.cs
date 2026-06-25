using System.Collections.Generic;
using UnityEngine;

public static class BezierCurve 
{
    public static void DrawCurve(BezierCurveData curve, int segments = 30)
    {
        Vector3 previous = curve.controlPoints[0];

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;

            Vector3 current = BezierCurvePosition(curve, t);

            Gizmos.DrawLine(previous, current);

            previous = current;
        }
    }

    public static Vector3 BezierCurvePosition(BezierCurveData data, float step)
    {
        return Mathf.Pow(1 - step, 3) * data.controlPoints[0] + 3 * Mathf.Pow(1 - step, 2) * step * data.controlPoints[1] + 3 * (1 - step) * Mathf.Pow(step, 2) * data.controlPoints[2] + Mathf.Pow(step, 3) * data.controlPoints[3];
    }

    public static float GetCurveLength(
    BezierCurveData curve,
    int samples = 50)
    {
        float length = 0;

        Vector3 previous =
            BezierCurvePosition(curve, 0);

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;

            Vector3 current =
                BezierCurvePosition(curve, t);

            length += Vector3.Distance(previous, current);

            previous = current;
        }

        return length;
    }

    public static float GetPathLength(PathData path)
    {
        float total = 0;

        foreach (var curve in path.curves)
        {
            total += GetCurveLength(curve);
        }

        return total;
    }

    public static List<CurveSample> BuildArcLengthTable(
    BezierCurveData curve,
    int samples = 100)
    {
        List<CurveSample> table = new();

        float accumulatedDistance = 0;

        Vector3 previous =
            BezierCurvePosition(curve, 0);

        table.Add(new CurveSample(0, 0));

        for (int i = 1; i <= samples; i++)
        {
            float t = i / (float)samples;

            Vector3 current =
                BezierCurvePosition(curve, t);

            accumulatedDistance +=
                Vector3.Distance(previous, current);

            table.Add(
                new CurveSample(t, accumulatedDistance));

            previous = current;
        }

        return table;
    }

    public static float GetTAtDistance(
    List<CurveSample> table,
    float targetDistance)
    {
        if (targetDistance <= 0)
            return 0;

        float totalDistance =
            table[^1].distance;

        if (targetDistance >= totalDistance)
            return 1;

        for (int i = 1; i < table.Count; i++)
        {
            if (table[i].distance >= targetDistance)
            {
                CurveSample previous = table[i - 1];
                CurveSample current = table[i];

                float lerp =
                    Mathf.InverseLerp(
                        previous.distance,
                        current.distance,
                        targetDistance);

                return Mathf.Lerp(
                    previous.t,
                    current.t,
                    lerp);
            }
        }

        return 1;
    }
}

public struct CurveSample
{
    public float t;
    public float distance;

    public CurveSample(float t, float distance)
    {
        this.t = t;
        this.distance = distance;
    }
}