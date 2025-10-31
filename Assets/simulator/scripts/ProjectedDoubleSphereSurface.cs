using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Projects a 2D layout of points onto two spheres whose centers are
/// separated along an axis (hourglass / two-bowls shape).
/// The first forward intersection against either sphere is used,
/// but only if it falls on the configured hemisphere for that sphere.
/// </summary>
[System.Serializable]
public class ProjectedDoubleSphereSurface : HangerSurface
{
    [Tooltip("Midpoint between the two spheres in local space (converted with relativeTo.TransformPoint).")]
    public Vector3 center = Vector3.zero;

    [Tooltip("Sphere radius in world units.")]
    public float radius = 2f;

    [Tooltip("Distance between the two sphere centers along 'axis'.")]
    public float separation = 0.5f;

    [Header("Axis & projection direction")]
    [Tooltip("Axis pointing from the bottom sphere toward the top sphere.")]
    public Vector3 axis = Vector3.up;

    [Tooltip("Must match HangerBuilder.hangDirection (the ray direction).")]
    public Vector3 hangDirection = Vector3.down;

    [Header("Hemisphere selection")]
    [Tooltip("If true: use bottom hemisphere of the TOP sphere (hourglass).")]
    public bool topSphere_UseBottomHemisphere = true;

    [Tooltip("If true: use bottom hemisphere of the BOTTOM sphere (hourglass -> set this false).")]
    public bool bottomSphere_UseBottomHemisphere = false;

    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 d = hangDirection.normalized;      // ray direction (forward)
        Vector3 ax = axis.normalized;              // axis separating the two spheres

        // Build the two sphere centers in world space.
        // 'center' is the midpoint between them in local space.
        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 Ctop = mid + ax * (separation * 0.5f);
        Vector3 Cbot = mid - ax * (separation * 0.5f);
        float r = Mathf.Max(1e-5f, radius);

        Vector3 O = point.position;                // ray origin

        // Test a sphere and return best forward hit that matches its hemisphere; -1 if none.
        float HitSphere(Vector3 C, bool useBottom)
        {
            Vector3 OC = O - C;
            float a = Vector3.Dot(d, d);               // 1 if d normalized
            float b = 2f * Vector3.Dot(d, OC);
            float c = Vector3.Dot(OC, OC) - r * r;

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return -1f;

            float sqrtD = Mathf.Sqrt(disc);
            float inv2a = 0.5f / a;

            float t0 = (-b - sqrtD) * inv2a;
            float t1 = (-b + sqrtD) * inv2a;

            // hemisphere check relative to 'axis' (plane normal = axis through the center)
            bool OnRequestedHemisphere(Vector3 P)
            {
                float side = Vector3.Dot(P - C, ax);
                return useBottom ? (side <= 1e-6f) : (side >= -1e-6f);
            }

            float best = -1f;
            if (t0 >= 0f)
            {
                Vector3 P0 = O + d * t0;
                if (OnRequestedHemisphere(P0)) best = t0;
            }
            if (best < 0f && t1 >= 0f)
            {
                Vector3 P1 = O + d * t1;
                if (OnRequestedHemisphere(P1)) best = t1;
            }
            return best;
        }

        // Try both spheres, choose the closest valid forward hit.
        float tTop = HitSphere(Ctop, topSphere_UseBottomHemisphere);
        float tBot = HitSphere(Cbot, bottomSphere_UseBottomHemisphere);

        float t = -1f;
        if (tTop >= 0f) t = tTop;
        if (tBot >= 0f && (t < 0f || tBot < t)) t = tBot;

        return ApplyHeightOffset(t > 0f ? t : 0f);
    }

    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 ax = axis.normalized;
        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 Ctop = mid + ax * (separation * 0.5f);
        Vector3 Cbot = mid - ax * (separation * 0.5f);

        // Draw both spheres
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(Ctop, radius);
        Gizmos.DrawWireSphere(Cbot, radius);

        if (points == null) return;

        Gizmos.color = Color.cyan;
        Vector3 d = hangDirection.normalized;

        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;
            float len = CalculateLength(pt, relativeTo);
            if (len <= 0f) continue;

            Vector3 O = pt.position;
            Vector3 end = O + d * len;

            Gizmos.DrawLine(O, end);
            Gizmos.DrawWireSphere(end, 0.01f);
        }
    }
}