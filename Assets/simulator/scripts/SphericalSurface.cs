using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A HangerSurface strategy that calculates lengths so that the
/// hanger end points form the surface of a sphere.
/// </summary>
[System.Serializable]
public class SphericalSurface : HangerSurface
{
    [Header("Sphere Settings")]
    [SerializeField, Tooltip("The radius of the sphere")]
    public float radius = 1.0f;

    [SerializeField, Tooltip("The center of the sphere, relative to the HangerBuilder's transform")]
    public Vector3 centerOffset = Vector3.zero;

    [SerializeField, Tooltip("True to use the bottom half (like a bowl), False to use the top half (like a dome)")]
    public bool useBottomHalf = true;

    /// <summary>
    /// Calculates the hanger length based on the sphere equation.
    /// </summary>
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 anchorPos = point.position;
        Vector3 worldSphereCenter = relativeTo.position + centerOffset;

        // Solve for Y on the sphere's surface for the point's X and Z
        // (y - cy)^2 = r^2 - (x - cx)^2 - (z - cz)^2
        float x_dist_sq = (anchorPos.x - worldSphereCenter.x) * (anchorPos.x - worldSphereCenter.x);
        float z_dist_sq = (anchorPos.z - worldSphereCenter.z) * (anchorPos.z - worldSphereCenter.z);
        float r_sq = radius * radius;

        float y_delta_sq = r_sq - x_dist_sq - z_dist_sq;

        if (y_delta_sq < 0)
        {
            // This anchor point is horizontally outside the sphere's radius
            // We can't calculate a length. Return a minimum length.
            return 0.1f;
        }

        float y_delta = Mathf.Sqrt(y_delta_sq);
        
        // y = cy ± sqrt(...)
        float surface_y_top = worldSphereCenter.y + y_delta;
        float surface_y_bottom = worldSphereCenter.y - y_delta;

        float surface_y = useBottomHalf ? surface_y_bottom : surface_y_top;
        
        // Length is the vertical distance from the anchor to the calculated surface Y
        float length = anchorPos.y - surface_y;

        return Mathf.Max(0.1f, length); // Ensure length is positive
    }

    /// <summary>
    /// Draws the gizmos for the sphere and the hanger end points.
    /// </summary>
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 worldSphereCenter = relativeTo.position + centerOffset;

        // 1. Draw the wireframe of the sphere itself
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f); // Magenta
        Gizmos.DrawWireSphere(worldSphereCenter, radius);

        // 2. Draw the hanger lines to the calculated surface points
        if (points == null) return;
        
        Gizmos.color = Color.magenta;
        Vector3 hangDirection = Vector3.down; // Assume hang direction for gizmo

        foreach (var pt in points)
        {
            float length = CalculateLength(pt, relativeTo);
            Vector3 endPos = pt.position + hangDirection * length;
            
            Gizmos.DrawLine(pt.position, endPos);
            Gizmos.DrawWireSphere(endPos, 0.01f);
        }
    }
}
