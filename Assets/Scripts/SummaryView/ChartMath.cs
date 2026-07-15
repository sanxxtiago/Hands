using UnityEngine;

public static class ChartMath
{
    /// <summary>
    /// Convierte un ángulo (grados) y un radio en una posición 2D.
    /// </summary>
    public static Vector2 Polar(float angleDegrees, float radius)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)) * radius;
    }

    /// <summary>
    /// Genera los vértices de un polígono regular.
    /// </summary>
    public static Vector2[] BuildPolygon(
        int sides,
        float radius,
        float rotationOffset = 90f)
    {
        sides = Mathf.Max(3, sides);

        Vector2[] points = new Vector2[sides];

        float step = 360f / sides;

        for (int i = 0; i < sides; i++)
        {
            float angle = rotationOffset - i * step;

            points[i] = Polar(angle, radius);
        }

        return points;
    }

    /// <summary>
    /// Genera los puntos de un Radar Chart.
    /// Los valores deben estar normalizados entre 0 y 1.
    /// </summary>
    public static Vector2[] BuildRadar(
        float[] values,
        float radius,
        float rotationOffset = 90f)
    {
        if (values == null || values.Length < 3)
            return new Vector2[0];

        Vector2[] points = new Vector2[values.Length];

        float step = 360f / values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            float angle = rotationOffset - i * step;

            points[i] = Polar(
                angle,
                radius * Mathf.Clamp01(values[i]));
        }

        return points;
    }

    /// <summary>
    /// Convierte una serie de valores (0-1) en puntos para un gráfico lineal.
    /// </summary>
    public static Vector2[] BuildLine(
       float[] values,
       Rect rect)
    {
        if (values == null || values.Length == 0)
            return new Vector2[0];

        Vector2[] points = new Vector2[values.Length];

        float step = values.Length > 1
            ? rect.width / (values.Length - 1)
            : 0f;

        for (int i = 0; i < values.Length; i++)
        {
            points[i] = new Vector2(
            rect.xMin + i * step,
            Mathf.Lerp(
                rect.yMin,
                rect.yMax,
                Mathf.Clamp01(values[i])));
        }

        return points;
    }

    /// <summary>
    /// Remapea un valor de un rango a otro.
    /// </summary>
    public static float Remap(
        float value,
        float inMin,
        float inMax,
        float outMin,
        float outMax)
    {
        return outMin +
               (value - inMin) /
               (inMax - inMin) *
               (outMax - outMin);
    }

    /// <summary>
    /// Calcula el centroide de un conjunto de puntos.
    /// </summary>
    public static Vector2 Centroid(Vector2[] points)
    {
        if (points == null || points.Length == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        foreach (Vector2 point in points)
            sum += point;

        return sum / points.Length;
    }

    /// <summary>
    /// Aplica una rotación a todos los puntos en grados
    /// </summary>
    public static void Rotate(Vector2[] points, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;

        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p = points[i];

            points[i] = new Vector2(
                p.x * cos - p.y * sin,
                p.x * sin + p.y * cos);
        }
    }
}