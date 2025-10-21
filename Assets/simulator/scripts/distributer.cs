using System;
using System.Collections.Generic;
using UnityEngine;

public enum CurveMode { Flat, Parabola, Cosine, Custom }

// Legacy data structures for wire management (not currently used by distributor)
[Serializable]
public class WireRow
{
    public string label;
    public float length;
    public string group;
}

[Serializable]
public class WireDataset
{
    public WireRow[] wires;
}

/// <summary>
/// Enhanced wire distribution system with equal-spacing grid and curved boundary support.
/// Distributes points across a rectangular area with configurable top/bottom curve profiles.
/// </summary>
public class distributer : MonoBehaviour
{
    #region Configuration Fields
    
    [Header("Rectangle Dimensions (meters)")]
    [SerializeField, Range(0.1f, 10f)] private float plateWidthX = 1.5f;
    [SerializeField, Range(0.1f, 10f)] private float plateHeightZ = 1.2f;

    [Header("Margins (meters) — Layout Bounds Only")]
    [SerializeField, Min(0f)] private float marginX = 0.0f;
    [SerializeField, Min(0f)] private float marginZ = 0.0f;
    [SerializeField] private bool centerOnOrigin = true;

    [Header("Orientation")]
    [SerializeField] private bool leftToRight = true;
    [SerializeField] private bool topToBottom = true;

    [Header("Top Curve Profile")]
    [SerializeField] private CurveMode topCurveMode = CurveMode.Parabola;
    [SerializeField, Range(0f, 0.5f), Tooltip("Downward sag at center (meters)")]
    private float topSag = 0.08f;
    [SerializeField, Tooltip("Custom curve: x=0..1, y=0..1 normalized sag")]
    private AnimationCurve topCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Bottom Curve (3 Linked Segments)")]
    [SerializeField] private bool useTripleBottom = true;
    [SerializeField, Range(0f, 0.5f), Tooltip("Maximum upward rise at center (meters)")]
    private float bottomMaxRise = 0.10f;
    [SerializeField, Range(0.05f, 0.9f)] private float splitLeft = 0.33f;
    [SerializeField, Range(0.10f, 0.95f)] private float splitRight = 0.66f;
    [SerializeField, Tooltip("Left segment curve")]
    private AnimationCurve bottomLeft = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0.3f));
    [SerializeField, Tooltip("Center segment curve")]
    private AnimationCurve bottomCenter = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0.6f));
    [SerializeField, Tooltip("Right segment curve")]
    private AnimationCurve bottomRight = new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 0));

    [Header("Distribution Equation")]
    [SerializeField, Min(1f), Tooltip("Points per base area unit")]
    private float constantPoints = 120f;
    [SerializeField, Min(0.001f), Tooltip("Base units divisor")]
    private float baseUnits = 1f;

    [Header("Grid Sizing")]
    [SerializeField] private GridSizingMode gridSizing = GridSizingMode.AutoMatchTarget;
    [SerializeField, Range(0.01f, 1f), Tooltip("Fixed horizontal spacing (meters)")]
    
    private float spacingX = 0.10f;
    [SerializeField, Range(0.01f, 1f), Tooltip("Fixed vearticaal spacing (meters)")]
    private float spacingZ = 0.08f;

    [Header("Auto Match Settings")]
    [SerializeField, Range(0.0f, 0.2f), Tooltip("Tolerance for target matching (±%)")]
    private float acceptTolerance = 0.02f;
    [SerializeField, Range(5, 50)] private int maxAutoIters = 12;
    [SerializeField, Min(2)] private int minColsAuto = 4;
    [SerializeField, Min(10)] private int maxColsAuto = 200;

    [Header("Visuals")]
    [SerializeField] private GameObject pointPrefab;
    [SerializeField, Range(0.005f, 0.1f)] private float gizmoRadius = 0.02f;
    [SerializeField] private bool drawLabels = false;
    [SerializeField] private Color outerBoundsColor = Color.white;
    [SerializeField] private Color usableBoundsColor = Color.gray;
    [SerializeField] private Color topCurveColor = Color.cyan;
    [SerializeField] private Color bottomCurveColor = Color.magenta;
    [SerializeField] private Color pointColor = Color.yellow;
    [SerializeField] private Color acceptedPointColor = Color.green;
    [SerializeField] private Color rejectedPointColor = Color.red;

    [Header("Point Snapping Behavior")]
    [SerializeField] private PointSnappingMode snappingMode = PointSnappingMode.SnapToTopCurve;
    [SerializeField, Range(0f, 1f), Tooltip("0=bottom, 0.5=center, 1=top")]
    private float verticalAlignment = 0.5f;

    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGridCandidates = false;
    [SerializeField] private bool showCurvePoints = true;
    [SerializeField, Range(10, 100)] private int curveResolution = 48;
    [SerializeField] private bool showColumnBounds = false;
    [SerializeField] private bool colorCodeByRow = false;
    [SerializeField] private bool showStatistics = true;

    #endregion

    #region Private Fields

    private readonly List<Transform> spawnedPoints = new List<Transform>();
    private readonly List<Vector3> debugCandidatePoints = new List<Vector3>();
    private readonly List<bool> debugPointAccepted = new List<bool>();
    private readonly List<Vector3> debugTopCurvePoints = new List<Vector3>();
    private readonly List<Vector3> debugBottomCurvePoints = new List<Vector3>();
    private GridMetrics cachedMetrics;
    
    public enum GridSizingMode { FixedSpacing, AutoMatchTarget }
    public enum PointSnappingMode 
    { 
        SnapToTopCurve,      // All points follow the top curve
        SnapToBottomCurve,   // All points follow the bottom curve
        VerticalAlignment,   // Blend between top and bottom based on verticalAlignment
        NoSnapping           // Original behavior - uniform grid spacing
    }

    #endregion

    #region Data Structures

    private struct GridMetrics
    {
        public int Columns;
        public int Rows;
        public float SpacingX;
        public float SpacingZ;
        public int TargetPoints;
        public int AcceptedPoints;
        public int DiscardedPoints;
        public float UsableWidth;
        public float UsableHeight;
        public float UsableArea;
        public GridSizingMode Mode;

        public override string ToString() =>
            $"Grid: {Columns}x{Rows}={Columns * Rows} candidates, " +
            $"Accepted: {AcceptedPoints}, Discarded: {DiscardedPoints}, " +
            $"Target: {TargetPoints}, dx={SpacingX:F3}m, dz={SpacingZ:F3}m";
    }

    private struct BoundaryPoint
    {
        public float X;
        public float Z;
        public float U; // Normalized position along X axis
    }

    #endregion

    #region Public API

    /// <summary>
    /// Builds or rebuilds the distribution grid based on current settings.
    /// </summary>
    [ContextMenu("Build / Rebuild")]
    public void Build()
    {
        ClearAllPoints();
        ClearDebugData();
        
        cachedMetrics = CalculateGridMetrics();
        GenerateCurveDebugPoints();
        GenerateGridPoints(cachedMetrics);
        LogBuildResults(cachedMetrics);
    }

    /// <summary>
    /// Clears all spawned points without rebuilding.
    /// </summary>
    [ContextMenu("Clear All Points")]
    public void ClearAllPoints()
    {
        foreach (var point in spawnedPoints)
        {
            if (point != null)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(point.gameObject);
                else
                    DestroyImmediate(point.gameObject);
                #else
                Destroy(point.gameObject);
                #endif
            }
        }
        spawnedPoints.Clear();
    }

    /// <summary>
    /// Clears debug visualization data.
    /// </summary>
    private void ClearDebugData()
    {
        debugCandidatePoints.Clear();
        debugPointAccepted.Clear();
        debugTopCurvePoints.Clear();
        debugBottomCurvePoints.Clear();
    }

    /// <summary>
    /// Gets the current number of spawned points.
    /// </summary>
    public int GetSpawnedCount() => spawnedPoints.Count;

    /// <summary>
    /// Gets the positions of all spawned points.
    /// </summary>
    public Vector3[] GetSpawnedPositions()
    {
        var positions = new Vector3[spawnedPoints.Count];
        for (int i = 0; i < spawnedPoints.Count; i++)
        {
            positions[i] = spawnedPoints[i] != null ? spawnedPoints[i].position : Vector3.zero;
        }
        return positions;
    }

    #endregion

    #region Grid Calculation

    private GridMetrics CalculateGridMetrics()
    {
        var metrics = new GridMetrics
        {
            Mode = gridSizing,
            UsableWidth = Mathf.Max(0f, plateWidthX - 2f * marginX),
            UsableHeight = Mathf.Max(0f, plateHeightZ - 2f * marginZ)
        };
        
        metrics.UsableArea = metrics.UsableWidth * metrics.UsableHeight;
        metrics.TargetPoints = CalculateTargetPoints();

        if (gridSizing == GridSizingMode.FixedSpacing)
        {
            CalculateFixedSpacingGrid(ref metrics);
        }
        else
        {
            CalculateAutoMatchGrid(ref metrics);
        }

        return metrics;
    }

    private int CalculateTargetPoints()
    {
        float fullArea = plateWidthX * plateHeightZ;
        return Mathf.Max(1, Mathf.RoundToInt((fullArea * constantPoints) / Mathf.Max(0.000001f, baseUnits)));
    }

    private void CalculateFixedSpacingGrid(ref GridMetrics metrics)
    {
        metrics.Columns = Mathf.Max(1, Mathf.FloorToInt(metrics.UsableWidth / Mathf.Max(1e-6f, spacingX)) + 1);
        metrics.Rows = Mathf.Max(1, Mathf.FloorToInt(metrics.UsableHeight / Mathf.Max(1e-6f, spacingZ)) + 1);
        metrics.SpacingX = metrics.Columns == 1 ? 0f : metrics.UsableWidth / (metrics.Columns - 1);
        metrics.SpacingZ = metrics.Rows == 1 ? 0f : metrics.UsableHeight / (metrics.Rows - 1);
    }

    private void CalculateAutoMatchGrid(ref GridMetrics metrics)
    {
        float aspect = metrics.UsableHeight <= 1e-6f ? 1f : metrics.UsableWidth / metrics.UsableHeight;
        int cols = Mathf.Clamp(
            Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt(metrics.TargetPoints * Mathf.Clamp(aspect, 0.25f, 4f)))),
            minColsAuto, maxColsAuto);
        int rows = Mathf.Max(1, Mathf.RoundToInt(metrics.TargetPoints / (float)cols));

        // Iterative refinement
        for (int iter = 0; iter < maxAutoIters; iter++)
        {
            float dx = cols == 1 ? metrics.UsableWidth : metrics.UsableWidth / (cols - 1);
            float dz = rows == 1 ? metrics.UsableHeight : metrics.UsableHeight / (rows - 1);
            
            int acceptedCount = SimulateAcceptedPoints(cols, rows, dx, dz);
            float tolerance = Mathf.Abs(acceptedCount - metrics.TargetPoints) / Mathf.Max(1f, metrics.TargetPoints);

            if (tolerance <= acceptTolerance)
            {
                metrics.Columns = cols;
                metrics.Rows = rows;
                metrics.SpacingX = dx;
                metrics.SpacingZ = dz;
                return;
            }

            if (acceptedCount == 0)
            {
                cols = Mathf.Min(maxColsAuto, cols + 2);
                rows += 2;
                continue;
            }

            // Scale towards target
            float scale = Mathf.Sqrt(metrics.TargetPoints / (float)acceptedCount);
            int newCols = Mathf.Clamp(Mathf.Max(1, Mathf.RoundToInt(cols * scale)), minColsAuto, maxColsAuto);
            int newRows = Mathf.Max(1, Mathf.RoundToInt(rows * scale));

            if (newCols == cols && newRows == rows)
                rows++;
            else
            {
                cols = newCols;
                rows = newRows;
            }
        }

        // Finalize with current values
        metrics.Columns = cols;
        metrics.Rows = rows;
        metrics.SpacingX = cols == 1 ? metrics.UsableWidth : metrics.UsableWidth / (cols - 1);
        metrics.SpacingZ = rows == 1 ? metrics.UsableHeight : metrics.UsableHeight / (rows - 1);
    }

    private int SimulateAcceptedPoints(int cols, int rows, float dx, float dz)
    {
        int accepted = 0;
        float x0 = GetLeftEdge();
        float z0 = GetBottomEdgeFlat();
        float zTopEdge = GetTopEdgeFlat();
        float zBotEdge = GetBottomEdgeFlat();

        for (int c = 0; c < cols; c++)
        {
            float x = cols == 1 ? (x0 + cachedMetrics.UsableWidth * 0.5f) : (x0 + c * dx);
            float u = CalculateUParameter(x);
            var bounds = GetColumnBounds(u);

            for (int r = 0; r < rows; r++)
            {
                float z = rows == 1
                    ? (z0 + (zTopEdge - zBotEdge) * 0.5f)
                    : (topToBottom ? (zTopEdge - r * dz) : (zBotEdge + r * dz));

                if (IsPointInsideBounds(z, bounds))
                    accepted++;
            }
        }

        return accepted;
    }

    #endregion

    #region Point Generation

    private void GenerateGridPoints(GridMetrics metrics)
    {
        float x0 = GetLeftEdge();
        float z0 = GetBottomEdgeFlat();
        float zTopEdge = GetTopEdgeFlat();
        float zBotEdge = GetBottomEdgeFlat();

        int accepted = 0, discarded = 0;

        for (int c = 0; c < metrics.Columns; c++)
        {
            float x = metrics.Columns == 1
                ? (x0 + metrics.UsableWidth * 0.5f)
                : (x0 + c * metrics.SpacingX);

            float u = CalculateUParameter(x);
            var bounds = GetColumnBounds(u);

            for (int r = 0; r < metrics.Rows; r++)
            {
                // Calculate base Z position from uniform grid
                float baseZ = metrics.Rows == 1
                    ? (z0 + metrics.UsableHeight * 0.5f)
                    : (topToBottom ? (zTopEdge - r * metrics.SpacingZ) : (zBotEdge + r * metrics.SpacingZ));

                Vector3 candidatePos = new Vector3(x, transform.position.y, baseZ);
                bool isAccepted = IsPointInsideBounds(baseZ, bounds);

                // Store debug info for original grid positions
                if (showGridCandidates || showDebugInfo)
                {
                    debugCandidatePoints.Add(candidatePos);
                    debugPointAccepted.Add(isAccepted);
                }

                if (isAccepted)
                {
                    // Apply snapping to get final position
                    Vector3 finalPos = ApplySnapping(candidatePos, u, bounds, r, metrics.Rows);
                    SpawnPoint(finalPos, $"C{c + 1}-R{r + 1}", r);
                    accepted++;
                }
                else
                {
                    discarded++;
                }
            }
        }

        metrics.AcceptedPoints = accepted;
        metrics.DiscardedPoints = discarded;
        cachedMetrics = metrics;
    }

    /// <summary>
    /// Applies snapping behavior to point position based on selected mode.
    /// </summary>
    private Vector3 ApplySnapping(Vector3 originalPos, float u, (float min, float max) bounds, int rowIndex, int totalRows)
    {
        float x = originalPos.x;
        float y = originalPos.y;
        float z = originalPos.z;

        switch (snappingMode)
        {
            case PointSnappingMode.SnapToTopCurve:
                // Snap all points to follow the top curve, maintaining relative vertical spacing
                float topZ = CalculateTopZ(u);
                float bottomZ = CalculateBottomZ(u);
                float columnHeight = Mathf.Abs(topZ - bottomZ);
                
                // Calculate position as offset from top
                float normalizedRowPos = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedRowPos = 1f - normalizedRowPos;
                
                z = topZ - (normalizedRowPos * columnHeight);
                break;

            case PointSnappingMode.SnapToBottomCurve:
                // Snap all points to follow the bottom curve
                float bottomCurveZ = CalculateBottomZ(u);
                float topCurveZ = CalculateTopZ(u);
                float heightFromBottom = Mathf.Abs(topCurveZ - bottomCurveZ);
                
                float normalizedFromBottom = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedFromBottom = 1f - normalizedFromBottom;
                
                z = bottomCurveZ + (normalizedFromBottom * heightFromBottom);
                break;

            case PointSnappingMode.VerticalAlignment:
                // Blend between top and bottom curves based on verticalAlignment
                float topCurve = CalculateTopZ(u);
                float bottomCurve = CalculateBottomZ(u);
                float blendedBase = Mathf.Lerp(bottomCurve, topCurve, verticalAlignment);
                float totalHeight = Mathf.Abs(topCurve - bottomCurve);
                
                float rowOffset = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) rowOffset = 1f - rowOffset;
                
                // Center around the blended position
                z = blendedBase + (0.5f - rowOffset) * totalHeight;
                break;

            case PointSnappingMode.NoSnapping:
            default:
                // Keep original uniform grid position
                z = originalPos.z;
                break;
        }

        return new Vector3(x, y, z);
    }

    private void SpawnPoint(Vector3 position, string pointName, int rowIndex)
    {
        Transform pointTransform;

        if (pointPrefab != null)
        {
            var instance = Instantiate(pointPrefab, position, Quaternion.identity, transform);
            instance.name = pointName;
            pointTransform = instance.transform;
        }
        else
        {
            var obj = new GameObject(pointName);
            obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            pointTransform = obj.transform;
        }

        // Add debug component for color coding
        if (colorCodeByRow)
        {
            var debugInfo = pointTransform.gameObject.AddComponent<PointDebugInfo>();
            debugInfo.rowIndex = rowIndex;
        }

        spawnedPoints.Add(pointTransform);
    }

    /// <summary>
    /// /// Generates curve visualization points for debugging.
    /// </summary>
    private void GenerateCurveDebugPoints()
    {
        debugTopCurvePoints.Clear();
        debugBottomCurvePoints.Clear();

        for (int i = 0; i <= curveResolution; i++)
        {
            float uDirection = i / (float)curveResolution;
            float u = leftToRight ? uDirection : 1f - uDirection;
            float x = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), uDirection);
            
            float topZ = CalculateTopZ(u);
            float bottomZ = CalculateBottomZ(u);
            
            debugTopCurvePoints.Add(new Vector3(x, transform.position.y, topZ));
            debugBottomCurvePoints.Add(new Vector3(x, transform.position.y, bottomZ));
        }
    }

    #endregion

    #region Curve Calculations

    private float CalculateUParameter(float worldX)
    {
        float xLeft = GetLeftEdge();
        float xRight = GetRightEdge();
        float uDirect = Mathf.InverseLerp(xLeft, xRight, worldX);
        return leftToRight ? uDirect : 1f - uDirect;
    }

    private float CalculateTopZ(float u)
    {
        float baseTop = GetTopEdgeFlat();
        
        return topCurveMode switch
        {
            CurveMode.Flat => baseTop,
            CurveMode.Parabola => baseTop - (4f * u * (1f - u) * topSag),
            CurveMode.Cosine => baseTop - ((1f - Mathf.Cos(Mathf.PI * u)) * 0.5f * topSag),
            CurveMode.Custom => baseTop - (Mathf.Clamp01(topCurve.Evaluate(u)) * topSag),
            _ => baseTop
        };
    }

    private float CalculateBottomZ(float u)
    {
        if (!useTripleBottom)
            return GetBottomEdgeFlat();

        float sL = Mathf.Clamp01(splitLeft);
        float sR = Mathf.Clamp01(splitRight);
        if (sR <= sL) sR = sL + 0.01f;

        float normalizedRise = u <= sL
            ? EvaluateSegment(bottomLeft, u, 0f, sL)
            : u < sR
                ? EvaluateSegment(bottomCenter, u, sL, sR)
                : EvaluateSegment(bottomRight, u, sR, 1f);

        return GetBottomEdgeFlat() + normalizedRise * bottomMaxRise;
    }

    private float EvaluateSegment(AnimationCurve curve, float u, float segmentStart, float segmentEnd)
    {
        float span = Mathf.Max(1e-6f, segmentEnd - segmentStart);
        float t = Mathf.Clamp01((u - segmentStart) / span);
        return Mathf.Clamp01(curve.Evaluate(t));
    }

    private (float min, float max) GetColumnBounds(float u)
    {
        float topZ = CalculateTopZ(u);
        float bottomZ = CalculateBottomZ(u);
        return (Mathf.Min(topZ, bottomZ), Mathf.Max(topZ, bottomZ));
    }

    private bool IsPointInsideBounds(float z, (float min, float max) bounds)
    {
        return z >= bounds.min && z <= bounds.max;
    }

    #endregion

    #region Geometry Helpers

    private float GetLeftEdge()
    {
        float baseX = centerOnOrigin ? transform.position.x - plateWidthX * 0.5f : transform.position.x;
        return baseX + marginX;
    }

    private float GetRightEdge()
    {
        float baseX = centerOnOrigin ? transform.position.x + plateWidthX * 0.5f : transform.position.x + plateWidthX;
        return baseX - marginX;
    }

    private float GetTopEdgeFlat()
    {
        float baseZ = centerOnOrigin ? transform.position.z + plateHeightZ * 0.5f : transform.position.z + plateHeightZ;
        return baseZ - marginZ;
    }

    private float GetBottomEdgeFlat()
    {
        float baseZ = centerOnOrigin ? transform.position.z - plateHeightZ * 0.5f : transform.position.z;
        return baseZ + marginZ;
    }

    #endregion

    #region Debug & Logging

    private void LogBuildResults(GridMetrics metrics)
    {
        if (!showDebugInfo) return;

        float fullArea = plateWidthX * plateHeightZ;
        float efficiency = metrics.TargetPoints > 0 ? (metrics.AcceptedPoints / (float)metrics.TargetPoints) * 100f : 0f;
        float fillRate = (metrics.Columns * metrics.Rows) > 0 ? (metrics.AcceptedPoints / (float)(metrics.Columns * metrics.Rows)) * 100f : 0f;

        string snappingInfo = snappingMode switch
        {
            PointSnappingMode.SnapToTopCurve => "Points snap to TOP curve (maintaining vertical spacing)",
            PointSnappingMode.SnapToBottomCurve => "Points snap to BOTTOM curve (maintaining vertical spacing)",
            PointSnappingMode.VerticalAlignment => $"Points aligned at {verticalAlignment:P0} between curves",
            PointSnappingMode.NoSnapping => "Uniform grid (no curve snapping)",
            _ => "Unknown"
        };

        string report = $"<b><color=cyan>═══ WireDistributor Build Report ═══</color></b>\n\n" +
                       $"<b>Dimensions:</b>\n" +
                       $"  • Plate Size: {plateWidthX:F3}m × {plateHeightZ:F3}m\n" +
                       $"  • Full Area: {fullArea:F4}m²\n" +
                       $"  • Usable Area: {metrics.UsableArea:F4}m² (after margins)\n" +
                       $"  • Margins: X={marginX:F3}m, Z={marginZ:F3}m\n\n" +

                       $"<b>Target Calculation:</b>\n" +
                       $"  • Formula: (Area × Constant) / BaseUnits\n" +
                       $"  • ({fullArea:F4} × {constantPoints}) / {baseUnits} = <color=yellow>{metrics.TargetPoints} points</color>\n\n" +

                       $"<b>Grid Configuration:</b>\n" +
                       $"  • Mode: <color=cyan>{metrics.Mode}</color>\n" +
                       $"  • Columns: {metrics.Columns}\n" +
                       $"  • Rows: {metrics.Rows}\n" +
                       $"  • Total Candidates: {metrics.Columns * metrics.Rows}\n" +
                       $"  • Spacing: dx={metrics.SpacingX:F4}m, dz={metrics.SpacingZ:F4}m\n\n" +

                       $"<b>Point Snapping:</b>\n" +
                       $"  • <color=yellow>{snappingInfo}</color>\n\n" +

                       $"<b>Results:</b>\n" +
                       $"  • <color=green>Accepted: {metrics.AcceptedPoints} points</color>\n" +
                       $"  • <color=red>Rejected: {metrics.DiscardedPoints} points</color>\n" +
                       $"  • Efficiency: {efficiency:F1}% (vs target)\n" +
                       $"  • Fill Rate: {fillRate:F1}% (of grid)\n\n" +

                       $"<b>Curve Settings:</b>\n" +
                       $"  • Top: {topCurveMode} (sag={topSag:F3}m)\n" +
                       $"  • Bottom: {(useTripleBottom ? $"Triple-segment (rise={bottomMaxRise:F3}m)" : "Flat")}\n";

        if (useTripleBottom)
        {
            report += $"  • Splits: {splitLeft:F2} / {splitRight:F2}\n";
        }

        Debug.Log(report);
    }

    /// <summary>
    /// Gets detailed statistics about the current distribution.
    /// </summary>
    [ContextMenu("Print Detailed Statistics")]
    public void PrintDetailedStatistics()
    {
        if (spawnedPoints.Count == 0)
        {
            Debug.LogWarning("[WireDistributor] No points generated. Build first!");
            return;
        }

        // Calculate density
        float fullArea = plateWidthX * plateHeightZ;
        float density = spawnedPoints.Count / fullArea;

        // Calculate bounding box
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (var pt in spawnedPoints)
        {
            if (pt == null) continue;
            min = Vector3.Min(min, pt.position);
            max = Vector3.Max(max, pt.position);
        }

        Vector3 actualSize = max - min;
        float actualArea = actualSize.x * actualSize.z;

        Debug.Log(
            $"<b><color=cyan>═══ Detailed Statistics ═══</color></b>\n\n" +
            $"<b>Point Distribution:</b>\n" +
            $"  • Total Points: {spawnedPoints.Count}\n" +
            $"  • Density: {density:F2} points/m²\n" +
            $"  • Actual Bounding Box: {actualSize.x:F3}m × {actualSize.z:F3}m\n" +
            $"  • Actual Coverage Area: {actualArea:F4}m²\n\n" +
            $"<b>Spacing Analysis:</b>\n" +
            $"  • Configured: dx={cachedMetrics.SpacingX:F4}m, dz={cachedMetrics.SpacingZ:F4}m\n" +
            $"  • Avg Spacing: {((cachedMetrics.SpacingX + cachedMetrics.SpacingZ) / 2f):F4}m\n" +
            $"  • Grid Density: {1f / (cachedMetrics.SpacingX * cachedMetrics.SpacingZ):F2} points/m² (theoretical)\n"
        );
    }

    #endregion

    #region Unity Callbacks

    private void OnValidate()
    {
        splitLeft = Mathf.Clamp01(splitLeft);
        splitRight = Mathf.Clamp01(splitRight);
        if (splitRight < splitLeft + 0.01f)
            splitRight = splitLeft + 0.01f;
    }

    private void OnDrawGizmosSelected()
    {
        DrawOuterBounds();
        DrawUsableBounds();
        
        if (showCurvePoints)
        {
            DrawTopCurve();
            DrawBottomCurve();
            DrawCurveControlPoints();
        }
        
        if (showGridCandidates)
        {
            DrawGridCandidates();
        }
        
        if (showColumnBounds)
        {
            DrawColumnBoundaries();
        }
        
        DrawSpawnedPoints();
        
        if (showStatistics)
        {
            DrawStatisticsOverlay();
        }
    }

    private void DrawOuterBounds()
    {
        Vector3 bl = GetOuterCorner(0, 0);
        Vector3 br = GetOuterCorner(plateWidthX, 0);
        Vector3 tl = GetOuterCorner(0, plateHeightZ);
        Vector3 tr = GetOuterCorner(plateWidthX, plateHeightZ);

        Gizmos.color = outerBoundsColor;
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawUsableBounds()
    {
        Vector3 bl = new Vector3(GetLeftEdge(), transform.position.y, GetBottomEdgeFlat());
        Vector3 br = new Vector3(GetRightEdge(), transform.position.y, GetBottomEdgeFlat());
        Vector3 tl = new Vector3(GetLeftEdge(), transform.position.y, GetTopEdgeFlat());
        Vector3 tr = new Vector3(GetRightEdge(), transform.position.y, GetTopEdgeFlat());

        Gizmos.color = usableBoundsColor;
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawTopCurve()
    {
        DrawCurve(CalculateTopZ, topCurveColor, 48);
    }

    private void DrawBottomCurve()
    {
        DrawCurve(CalculateBottomZ, bottomCurveColor, 48);
    }

    private void DrawCurve(Func<float, float> curveFunction, Color color, int segments)
    {
        Gizmos.color = color;
        Vector3 previousPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float uDirection = i / (float)segments;
            float u = leftToRight ? uDirection : 1f - uDirection;
            float x = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), uDirection);
            float z = curveFunction(u);
            Vector3 point = new Vector3(x, transform.position.y, z);

            if (i > 0)
                Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
        }
    }

    private void DrawCurveControlPoints()
    {
        // Draw key points on curves to show shape
        float y = transform.position.y + 0.02f; // Slightly above for visibility

        // Top curve control points
        Gizmos.color = topCurveColor;
        for (int i = 0; i <= 10; i++)
        {
            float u = i / 10f;
            float x = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), leftToRight ? u : 1f - u);
            float z = CalculateTopZ(u);
            Gizmos.DrawWireSphere(new Vector3(x, y, z), gizmoRadius * 0.5f);
        }

        // Bottom curve control points
        if (useTripleBottom)
        {
            Gizmos.color = bottomCurveColor;
            for (int i = 0; i <= 10; i++)
            {
                float u = i / 10f;
                float x = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), leftToRight ? u : 1f - u);
                float z = CalculateBottomZ(u);
                Gizmos.DrawWireSphere(new Vector3(x, y, z), gizmoRadius * 0.5f);
            }

            // Draw split markers
            Gizmos.color = Color.yellow;
            float xSplitL = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), leftToRight ? splitLeft : 1f - splitLeft);
            float xSplitR = Mathf.Lerp(GetLeftEdge(), GetRightEdge(), leftToRight ? splitRight : 1f - splitRight);
            float zSplitL = CalculateBottomZ(splitLeft);
            float zSplitR = CalculateBottomZ(splitRight);

            Gizmos.DrawWireSphere(new Vector3(xSplitL, y, zSplitL), gizmoRadius * 1.5f);
            Gizmos.DrawWireSphere(new Vector3(xSplitR, y, zSplitR), gizmoRadius * 1.5f);
        }
    }
    
    private void DrawGridCandidates()
    {
        for (int i = 0; i < debugCandidatePoints.Count; i++)
        {
            Gizmos.color = debugPointAccepted[i] ? acceptedPointColor : rejectedPointColor;
            Gizmos.DrawWireSphere(debugCandidatePoints[i], gizmoRadius * 0.7f);
        }
    }

    private void DrawColumnBoundaries()
    {
        #if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
        
        int columns = cachedMetrics.Columns > 0 ? cachedMetrics.Columns : 10;
        float x0 = GetLeftEdge();
        float usableW = GetRightEdge() - GetLeftEdge();
        
        for (int c = 0; c < columns; c++)
        {
            float x = columns == 1 ? (x0 + usableW * 0.5f) : (x0 + c * cachedMetrics.SpacingX);
            float u = CalculateUParameter(x);
            var bounds = GetColumnBounds(u);
            
            Vector3 top = new Vector3(x, transform.position.y, bounds.max);
            Vector3 bottom = new Vector3(x, transform.position.y, bounds.min);
            
            UnityEditor.Handles.DrawLine(top, bottom, 2f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(top, gizmoRadius * 0.5f);
            Gizmos.DrawWireSphere(bottom, gizmoRadius * 0.5f);
        }
        #endif
    }
    private void DrawStatisticsOverlay()
    {
        #if UNITY_EDITOR
        if (spawnedPoints.Count == 0) return;

        Vector3 centerPos = transform.position + Vector3.up * 0.1f;
        string stats = $"Points: {spawnedPoints.Count}/{cachedMetrics.TargetPoints}\n" +
                      $"Grid: {cachedMetrics.Columns}×{cachedMetrics.Rows}\n" +
                      $"Efficiency: {(cachedMetrics.TargetPoints > 0 ? (spawnedPoints.Count / (float)cachedMetrics.TargetPoints * 100f) : 0f):F1}%";
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        UnityEditor.Handles.Label(centerPos, stats, style);
        #endif
    }
    private void DrawSpawnedPoints()
    {
        foreach (var point in spawnedPoints)
        {
            if (point == null) continue;
            
            Color pointGizmoColor = pointColor;
            
            // Color code by row if enabled
            if (colorCodeByRow)
            {
                var debugInfo = point.GetComponent<PointDebugInfo>();
                if (debugInfo != null)
                {
                    float hue = (debugInfo.rowIndex * 0.15f) % 1f;
                    pointGizmoColor = Color.HSVToRGB(hue, 0.8f, 1f);
                }
            }
            
            Gizmos.color = pointGizmoColor;
            Gizmos.DrawSphere(point.position, gizmoRadius);
            
            #if UNITY_EDITOR
            if (drawLabels)
            {
                UnityEditor.Handles.Label(
                    point.position + Vector3.up * gizmoRadius * 1.5f,
                    point.name
                );
            }
            #endif
        }
    }

    private Vector3 GetOuterCorner(float offsetX, float offsetZ)
    {
        float x = centerOnOrigin
            ? transform.position.x - plateWidthX * 0.5f + offsetX
            : transform.position.x + offsetX;
        float z = centerOnOrigin
            ? transform.position.z - plateHeightZ * 0.5f + offsetZ
            : transform.position.z + offsetZ;
        
        return new Vector3(x, transform.position.y, z);
    }

    #endregion
}

/// <summary>
/// Helper component for debugging individual points.
/// </summary>
public class PointDebugInfo : MonoBehaviour
{
    public int rowIndex;
    public int columnIndex;
    public float uParameter;
    public bool wasAccepted = true;
}