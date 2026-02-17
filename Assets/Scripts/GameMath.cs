using System.Collections;
using UnityEngine;
using System;

public static class GameMath
{
    // Lerp a number over time (for UI counting)
    public static IEnumerator CountUp(int start, int end, float duration, Action<int> onUpdate)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / duration;
            // Standard Lerp
            int current = Mathf.RoundToInt(Mathf.Lerp(start, end, t));
            onUpdate(current);
            yield return null;
        }
        onUpdate(end);
    }

    // Quadratic Bezier Curve (Start -> Curve Point -> End)
    public static Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        // Formula: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}