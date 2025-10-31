using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// A hanger surface strategy that projects a 2D layout of points onto the
/// 3D surface of a sphere.
/// </summary>
[System.Serializable]
public class ProjectedSphereSurface : HangerSurface
{
    [Tooltip("Sphere center is relativeTo.TransformPoint(center)")]
    public Vector3 center = Vector3.zero;

    [Tooltip("Sphere radius in world units.")]
    public float radius = 2f;

    [Tooltip("True = pick intersection on/below the equator (y <= center.y). False = on/above.")]
    public bool useBottomHalf = true;

    [Header("Projection direction (match HangerBuilder.hangDirection)")]
    public Vector3 hangDirection = Vector3.down;   // <-- set this to exactly what your builder uses

    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        // Sphere in world space
        Vector3 C = relativeTo.TransformPoint(center);
        float r = Mathf.Max(1e-5f, radius);

        // Ray origin/direction in world space
        Vector3 O = point.position;                      // your startPos
        Vector3 d = hangDirection.normalized;            // MUST match CreateHanger's 'direction'

        // Solve |O + d*t - C|^2 = r^2  ->  a t^2 + b t + c = 0
        Vector3 OC = O - C;
        float a = Vector3.Dot(d, d);                     // should be 1 if d is normalized
        float b = 2f * Vector3.Dot(d, OC);
        float c = Vector3.Dot(OC, OC) - r * r;

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return 0f;                        // no intersection along the ray

        float sqrtD = Mathf.Sqrt(disc);
        float inv2a = 0.5f / a;

        // Two solutions along the line (t0 <= t1)
        float t0 = (-b - sqrtD) * inv2a;
        float t1 = (-b + sqrtD) * inv2a;

        // We need the FIRST intersection in the forward direction (t >= 0),
        // that also satisfies the hemisphere rule if requested.
        float chosen = PickHemisphereHit(O, d, C, t0, t1, useBottomHalf);

        // If none matched, return 0 (miss or wrong hemisphere)
        return ApplyHeightOffset(chosen > 0f ? chosen : 0f);
    }

    float PickHemisphereHit(Vector3 O, Vector3 d, Vector3 C, float t0, float t1, bool bottom)
    {
        // Create a small helper to check hemisphere
        bool IsOnRequestedHemisphere(Vector3 P)
        {
            return bottom ? (P.y <= C.y + 1e-6f) : (P.y >= C.y - 1e-6f);
        }

        float best = -1f;

        if (t0 >= 0f)
        {
            Vector3 P0 = O + d * t0;
            if (IsOnRequestedHemisphere(P0)) best = t0;
        }

        if (best < 0f && t1 >= 0f)
        {
            Vector3 P1 = O + d * t1;
            if (IsOnRequestedHemisphere(P1)) best = t1;
        }

        return best; // -1 => no valid hit forward that matches hemisphere
    }

    // (Optional) Gizmos: draw from anchor to the computed end point
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 C = relativeTo.TransformPoint(center);
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(C, radius);

        if (points == null) return;

        Gizmos.color = Color.cyan;
        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;

            float len = CalculateLength(pt, relativeTo);
            if (len <= 0f) continue;

            Vector3 O = pt.position;
            Vector3 d = hangDirection.normalized;
            Vector3 end = O + d * len;

            Gizmos.DrawLine(O, end);
            Gizmos.DrawWireSphere(end, 0.01f);
        }
    }
}