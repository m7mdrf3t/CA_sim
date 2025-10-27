using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Add this to use .Count() and .First()

/// <summary>
/// A hanger surface strategy where hangers connect to a simple flat plane (e.g., a ceiling).
/// </summary>
[System.Serializable]
public class FlatSurface : HangerSurface
{
    [Tooltip("The Y-coordinate of the flat ceiling plane.")]
    public float ceilingY = 5f;
    
    [Tooltip("Additional fixed length to add to all hangers (can be negative).")]
    public float additionalLength = 0f;

    /// <summary>
    /// Calculates the length from the point's Y position to the ceilingY.
    /// </summary>
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        // Calculate the vertical distance from the point to the ceiling
        float length = Mathf.Abs(ceilingY - point.position.y);
        
        return Mathf.Max(0.01f, length + additionalLength);
    }

    /// <summary>
    /// Draws the flat ceiling plane and the hanger lines.
    /// --- THIS IS THE FIX: Changed List to IEnumerable ---
    /// </summary>
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        if (points == null || !points.Any()) return;

        // 1. Calculate the bounds *of the points themselves*
        // We must filter for 'isAccepted' first
        var acceptedPoints = points.Where(p => p.isAccepted).Select(p => p.position);
        
        if (!acceptedPoints.Any()) return; // No accepted points to draw

        Bounds pointBounds = new Bounds(acceptedPoints.First(), Vector3.zero);
        foreach (var pos in acceptedPoints)
        {
            pointBounds.Encapsulate(pos);
        }

        // 2. Draw the flat ceiling plane based on the point bounds
        Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Light gray
        
        Vector3 planeCenter = new Vector3(pointBounds.center.x, ceilingY, pointBounds.center.z);
        Vector3 planeSize = new Vector3(pointBounds.size.x + 0.5f, 0.01f, pointBounds.size.z + 0.5f);
        
        Gizmos.DrawCube(planeCenter, planeSize);

        // 3. Draw the hanger lines
        Gizmos.color = Color.green; 
        Vector3 hangDirection = Vector3.down; 

        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;
            
            // We can't use CalculateLength here easily as it needs the PointData struct,
            // but for gizmos, we can just draw to the ceiling.
            Vector3 endPos = new Vector3(pt.position.x, ceilingY - additionalLength, pt.position.z);
            
            Gizmos.DrawLine(pt.position, endPos);
            Gizmos.DrawWireSphere(endPos, 0.01f);
        }
    }
}

