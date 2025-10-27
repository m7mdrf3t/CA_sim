using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A shape strategy that distributes points directly on the surface of a 3D sphere.
/// </summary>
[System.Serializable]
public class SphericalDistribution : DistributionShape
{
    [Header("Sphere Distribution Settings")]
    public float radius = 1.0f;
    [Tooltip("Number of iterations for Fibonacci sphere point generation. Higher = more even distribution.")]
    [Range(10, 2000)]
    public int fibonacciPointCount = 500; 
    public Vector3 centerOffset = Vector3.zero;

    /// <summary>
    /// Gets a dummy bounding box for the entire sphere.
    /// This is mainly for PointGenerator's internal calculations and gizmos,
    /// as points are generated directly, not from a grid.
    /// </summary>
    // public override Bounds GetBounds(Transform relativeTo)
    // {
    //     Vector3 center = relativeTo.position + centerOffset;
    //     Vector3 size = new Vector3(radius * 2f, radius * 2f, radius * 2f);
    //     return new Bounds(center, size);
    // }

    public override Bounds GetBounds(Transform relativeTo)
    {
        // Calculate bounds using THIS class's properties
        Vector3 worldCenter = relativeTo.position + centerOffset;
        Vector3 size = Vector3.one * radius * 2f;
        
        // Return a new Bounds, calculated here
        return new Bounds(worldCenter, size);
    }

    /// <summary>
    /// This method is not directly used for point generation in this shape,
    /// as points are generated using spherical coordinates.
    /// It returns a full height range for visualization or compatibility.
    /// </summary>
    public override (float min, float max) GetVerticalBounds(float u, Transform relativeTo)
    {
        Bounds bounds = GetBounds(relativeTo);
        return (bounds.min.y, bounds.max.y); // Return full vertical range
    }

    /// <summary>
    /// Calculates target points using the surface area of a sphere (4 * Pi * r^2).
    /// </summary>
    public override int CalculateTargetPoints(DensityProfile density)
    {
        if (density == null) return 0;
        
        // Use the surface area of a sphere
        float surfaceArea = 4f * Mathf.PI * radius * radius;
        
        // fibonacciPointCount acts as a 'base' for how dense the sphere is intrinsically.
        // We'll scale it by the density profile for robustness.
        float densityMultiplier = (density.constantPoints / Mathf.Max(0.000001f, density.baseUnits));
        int target = Mathf.RoundToInt(fibonacciPointCount * densityMultiplier);
        
        return Mathf.Max(1, target);
    }

    /// <summary>
    /// ApplySnapping is not relevant for this shape, as points are directly placed.
    /// </summary>
    public override Vector3 ApplySnapping(PointSnappingMode mode, Vector3 originalPos, float u, (float min, float max) bounds, int rowIndex, int totalRows, float verticalAlignment, bool topToBottom)
    {
        return originalPos; // No snapping needed for pre-calculated spherical points
    }

    /// <summary>
    /// Draws the spherical distribution for visualization.
    /// </summary>
    public override void DrawGizmos(Transform relativeTo, bool showCurvePoints, int curveResolution)
    {
        Vector3 worldCenter = relativeTo.position + centerOffset;
        
        Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.7f); // Purple-ish
        Gizmos.DrawWireSphere(worldCenter, radius);

        // Optionally draw a few points for visual density representation
        if (showCurvePoints && fibonacciPointCount > 0)
        {
            Gizmos.color = Color.yellow;
            // Draw a subset of Fibonacci points for visual reference
            int numGizmoPoints = Mathf.Min(100, fibonacciPointCount);
            for (int i = 0; i < numGizmoPoints; i++)
            {
                float phi = Mathf.Acos(1 - 2 * (i / (float)fibonacciPointCount));
                float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i;
                
                Vector3 point = new Vector3(
                    radius * Mathf.Cos(theta) * Mathf.Sin(phi),
                    radius * Mathf.Sin(theta) * Mathf.Sin(phi),
                    radius * Mathf.Cos(phi)
                );
                Gizmos.DrawSphere(worldCenter + point, 0.01f);
            }
        }
    }

    /// <summary>
    /// Generates points directly on the surface of a sphere using Fibonacci spiral.
    /// This method is called by the PointGenerator to override its grid generation.
    /// </summary>
    public List<PointData> GenerateSphericalPoints(Transform relativeTo, int actualPointCount)
    {
        List<PointData> points = new List<PointData>();
        Vector3 worldCenter = relativeTo.position + centerOffset;

        if (actualPointCount <= 0) return points;

        // Fibonacci sphere generation
        // Reference: https://stackoverflow.com/questions/9600801/how-to-generate-evenly-distributed-points-on-sphere
        for (int i = 0; i < actualPointCount; i++)
        {
            float phi = Mathf.Acos(1 - 2 * (i / (float)actualPointCount)); // Latitude
            float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * i;             // Longitude (golden angle)

            Vector3 point = new Vector3(
                radius * Mathf.Cos(theta) * Mathf.Sin(phi),
                radius * Mathf.Sin(theta) * Mathf.Sin(phi),
                radius * Mathf.Cos(phi)
            );
            
            // Adjust to the world center
            Vector3 worldPoint = worldCenter + point;

            points.Add(new PointData
            {
                position = worldPoint,
                name = $"SpherePoint_{i}", // Name for debug/info
                normalizedU = 0.5f,        // Not strictly used for spherical, but good for compatibility
                isAccepted = true          // Always accepted as directly generated
            });
        }
        return points;
    }
}
