using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for all hanger length calculation strategies (Strategy Pattern).
/// Defines the 3D surface shape that the hangers will attach to.
/// </summary>
[System.Serializable]
public abstract class HangerSurface
{
    /// <summary>
    /// Global height offset applied to all surface calculations.
    /// This value should be set from UserConfig.
    /// Positive values raise the surface, negative values lower it.
    /// </summary>
    [Tooltip("Global height offset for the entire surface (set from UserConfig).")]
    public float globalHeightOffset = 0f;

    /// <summary>
    /// Calculates the final length of a single hanger based on its PointData.
    /// </summary>
    public abstract float CalculateLength(PointData point, Transform relativeTo);

    /// <summary>
    /// Draws debug visualizations for the 3D surface.
    /// </summary>
    public virtual void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Called by HangerBuilder's OnValidate.
    /// </summary>
    public virtual void OnValidate()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Apply the global height offset to a calculated surface height.
    /// Call this in CalculateLength after computing the base surface height.
    /// </summary>
    protected float ApplyHeightOffset(float baseHeight)
    {
        return baseHeight + globalHeightOffset;
    }
}
