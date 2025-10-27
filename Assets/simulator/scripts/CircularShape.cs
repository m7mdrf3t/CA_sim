using UnityEngine;

/// <summary>
/// A shape strategy that distributes points within a 2D circle.
/// It calculates its area using Pi*r^2 and its bounds using the circle equation.
/// </summary>
[System.Serializable]
public class CircularShape : DistributionShape
{
    [Header("Circle Settings")]
    public float radius = 0.5f;
    public bool centerOnOrigin = true;

    /// <summary>
    /// Gets the square bounding box that encloses the circle.
    /// </summary>
    // public override Bounds GetBounds(Transform relativeTo)
    // {
    //     Vector3 center;
    //     if (centerOnOrigin)
    //     {
    //         center = relativeTo.position;
    //     }
    //     else
    //     {
    //         // Assumes transform is bottom-left corner of the bounding box
    //         center = relativeTo.position + new Vector3(radius, 0, radius);
    //     }
        
    //     Vector3 size = new Vector3(radius * 2f, 0, radius * 2f);
    //     return new Bounds(center, size);
    // }



    public override Bounds GetBounds(Transform relativeTo)
    {
        // Calculate bounds using THIS class's properties
        Vector3 center;
        if (centerOnOrigin)
        {
            center = relativeTo.position;
        }
        else
        {
            // Assumes transform is bottom-left corner of the bounding box
            center = relativeTo.position + new Vector3(radius, 0, radius);
        }
        
        Vector3 size = new Vector3(radius * 2f, 0, radius * 2f);
        
        // Return a new Bounds, calculated here
        return new Bounds(center, size);
    }
    /// <summary>
    /// This is the core logic. It uses the circle equation (x^2 + z^2 = r^2)
    /// to find the min and max z-values for a given column (u).
    /// </summary>
    public override (float min, float max) GetVerticalBounds(float u, Transform relativeTo)
    {
        Bounds bounds = GetBounds(relativeTo);
        
        // Convert normalized 'u' (0-1) to a local x-coordinate (-radius to +radius)
        float localX = Mathf.Lerp(-radius, radius, u);

        // Solve for z_delta: z_delta = sqrt(r^2 - x^2)
        float rSquared = radius * radius;
        float xSquared = localX * localX;
        float z_delta_squared = rSquared - xSquared;

        if (z_delta_squared < 0)
        {
            // This column is outside the circle's x-range.
            // Return invalid bounds so no points are accepted.
            return (0, 0); 
        }

        float z_delta = Mathf.Sqrt(z_delta_squared);
        float worldCenterZ = bounds.center.z;
        
        // z = center_z ± z_delta
        return (worldCenterZ - z_delta, worldCenterZ + z_delta);
    }

    /// <summary>
    /// Calculates target points using the area of a circle (Pi * r^2).
    /// This is your custom equation for point count.
    /// </summary>
    public override int CalculateTargetPoints(DensityProfile density)
    {
        if (density == null) return 0;
        
        // Use the area of a circle
        float fullArea = Mathf.PI * radius * radius; 
        
        return Mathf.Max(1, Mathf.RoundToInt(
            (fullArea * density.constantPoints) / Mathf.Max(0.000001f, density.baseUnits)
        ));
    }

    /// <summary>
    /// The snapping logic is generic. It works by snapping to the
    /// bounds.min/max, which GetVerticalBounds provides.
    /// This is identical to the CurvedRectangleShape's implementation.
    /// </summary>
    public override Vector3 ApplySnapping(PointSnappingMode mode, Vector3 originalPos, float u, (float min, float max) bounds, int rowIndex, int totalRows, float verticalAlignment, bool topToBottom)
    {
        float x = originalPos.x;
        float y = originalPos.y;
        float z = originalPos.z;

        float topZ = bounds.max;
        float bottomZ = bounds.min;
        float columnHeight = Mathf.Abs(topZ - bottomZ);

        switch (mode)
        {
            case PointSnappingMode.SnapToTopCurve:
                float normalizedRowPos = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedRowPos = 1f - normalizedRowPos;
                z = topZ - (normalizedRowPos * columnHeight);
                break;

            case PointSnappingMode.SnapToBottomCurve:
                float normalizedFromBottom = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedFromBottom = 1f - normalizedFromBottom;
                z = bottomZ + (normalizedFromBottom * columnHeight);
                break;

            case PointSnappingMode.VerticalAlignment:
                float blendedBase = Mathf.Lerp(bottomZ, topZ, verticalAlignment);
                float rowOffset = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) rowOffset = 1f - rowOffset;
                z = blendedBase + (0.5f - rowOffset) * columnHeight;
                break;

            case PointSnappingMode.NoSnapping:
            default:
                z = originalPos.z; // Keep original uniform grid position
                break;
        }

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Draws the circular shape for visualization.
    /// </summary>
    public override void DrawGizmos(Transform relativeTo, bool showCurvePoints, int curveResolution)
    {
        Bounds bounds = GetBounds(relativeTo);
        
        // 1. Draw the outer bounding box
        Gizmos.color = Color.gray;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        // 2. Draw the actual circle
        Gizmos.color = Color.cyan; // Use a distinct color for the circle
        Vector3 center = bounds.center;
        
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= curveResolution; i++)
        {
            float angle = (i / (float)curveResolution) * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            Vector3 newPoint = center + new Vector3(x, 0, z);
            
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}