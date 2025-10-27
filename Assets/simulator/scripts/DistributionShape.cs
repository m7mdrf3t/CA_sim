using UnityEngine;

/// <summary>
/// Abstract base class for all distribution shapes (Strategy Pattern).
/// Defines the area and boundaries for point generation.
/// </summary>
[System.Serializable]
public abstract class DistributionShape
{
    /// <summary>
    /// Gets the world-space bounding box of the shape's usable area.
    /// </summary>
    public abstract Bounds GetBounds(Transform relativeTo);

    /// <summary>
    /// Gets the (min, max) vertical (Z) bounds for a given normalized (0-1) U coordinate.
    /// </summary>
    public abstract (float min, float max) GetVerticalBounds(float u, Transform relativeTo);

    /// <summary>
    /// Calculates the target number of points based on the shape's area and density.
    /// </summary>
    public abstract int CalculateTargetPoints(DensityProfile density);

    /// <summary>
    /// Applies vertical snapping logic to a point.
    /// </summary>
    public abstract Vector3 ApplySnapping(
        PointSnappingMode mode,
        Vector3 originalPos,
        float u,
        (float min, float max) bounds,
        int rowIndex,
        int totalRows,
        float verticalAlignment,
        bool topToBottom
    );
    
    /// <summary>
    /// Draws debug visualizations for the shape.
    /// </summary>
    public abstract void DrawGizmos(Transform relativeTo, bool showCurvePoints, int curveResolution);

    /// <summary>
    /// Called by PointGenerator's OnValidate.
    /// </summary>
    public virtual void OnValidate()
    {
        // Base implementation does nothing, but can be overridden
    }
}