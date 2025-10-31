using System;
using System.Collections.Generic;
using UnityEngine;

// Enums moved outside the class to be accessible by other scripts
public enum CurveMode { Flat, Parabola, Cosine, Custom }
public enum GridSizingMode { FixedSpacing, AutoMatchTarget }
public enum PointSnappingMode
{
    SnapToTopCurve,     // All points follow the top curve
    SnapToBottomCurve,  // All points follow the bottom curve
    VerticalAlignment,  // Blend between top and bottom
    NoSnapping          // Uniform grid spacing
}

/// <summary>
/// Data struct for a single generated point. This is the data
/// passed to the HangerBuilder.
/// </summary>
public struct PointData
{
    public Vector3 position;
    public string name;
    public int columnIndex;
    public int rowIndex;
    public int totalRows;
    public float uParameter;
    public (float min, float max) columnBounds;
    public float normalizedU; // Normalized X position (0-1)
    public bool isAccepted; // Flag if point is valid
}

/// <summary>
/// Refactored from 'distributer'.
/// Generates a list of PointData based on a 'DistributionShape' and 'DensityProfile'.
/// It no longer builds hangers, but provides the data for the 'HangerBuilder'.
/// </summary>
public class PointGenerator : MonoBehaviour
{
    #region Configuration

    [Header("Distribution System")]
    private DistributionSystem system;

    [Header("Configuration")]
    [SerializeField, Tooltip("Optional UserConfig to drive dimensions.")]
    private UserConfig configuration;
    
    [SerializeField, Tooltip("Density preset (High, Medium, Low).")]
    private DensityProfile density;
    
    [SerializeField, Tooltip("Grid sizing strategy.")]
    private GridSizingMode gridSizing = GridSizingMode.AutoMatchTarget;

    [SerializeField, Tooltip("Snapping behavior for points within the shape.")]
    private PointSnappingMode snappingMode = PointSnappingMode.SnapToTopCurve;

    public enum ShapeType { Rectangle, Circle } // You can add Oval, Radial here later

    [SerializeField, Tooltip("Select the shape to generate points in.")]
    private ShapeType selectedShape = ShapeType.Rectangle;
    
    [SerializeField, Tooltip("Settings for the rectangular shape.")]
    public CurvedRectangleShape rectangleShape = new CurvedRectangleShape();
    
    [SerializeField, Tooltip("Settings for the circular shape.")]
    public CircularShape circularShape = new CircularShape();
    
    /// <summary>
    /// Gets the currently selected shape strategy.
    /// </summary>
    private DistributionShape activeShape
    {
        get
        {
            switch (selectedShape)
            {
                case ShapeType.Circle:
                    return circularShape;
                case ShapeType.Rectangle:
                default:
                    return rectangleShape;
            }
        }
    }

    [Header("Orientation")]
    [SerializeField] private bool leftToRight = true;
    [SerializeField] private bool topToBottom = true;

    [Header("Fixed Spacing (If Mode = FixedSpacing)")]

    private DensityLevel densityLevel = DensityLevel.High;
    [SerializeField, Range(0.01f, 1f)] private float spacingX = 0.10f;
    [SerializeField, Range(0.01f, 1f)] private float spacingZ = 0.08f;

    [Header("Auto Match Settings (If Mode = AutoMatch)")]
    [SerializeField, Range(0.0f, 0.2f)] private float acceptTolerance = 0.02f;
    [SerializeField, Range(5, 50)] private int maxAutoIters = 12;
    [SerializeField, Min(2)] private int minColsAuto = 4;
    [SerializeField, Min(10)] private int maxColsAuto = 200;
    
    [Header("Snapping Alignment (If Mode = VerticalAlignment)")]
    [SerializeField, Range(0f, 1f), Tooltip("0=bottom, 0.5=center, 1=top")]
    private float verticalAlignment = 0.5f;

    [Header("Visuals (for Debug Points)")]
    [SerializeField] private GameObject pointPrefab;
    [SerializeField, Range(0.005f, 0.1f)] private float gizmoRadius = 0.02f;
    [SerializeField] private bool drawLabels = false;
    [SerializeField] private Color pointColor = Color.yellow;
    [SerializeField] private Color acceptedPointColor = Color.green;
    [SerializeField] private Color rejectedPointColor = Color.red;

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
    private GridMetrics cachedMetrics;

    #endregion

    #region Data Structures

    // GridMetrics is internal logic, so it stays here.
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
        public Bounds Bounds;

        public override string ToString() =>
            $"Grid: {Columns}x{Rows}={Columns * Rows} candidates, " +
            $"Accepted: {AcceptedPoints}, Discarded: {DiscardedPoints}, " +
            $"Target: {TargetPoints}, dx={SpacingX:F3}m, dz={SpacingZ:F3}m";
    }

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        system = GetComponent<DistributionSystem>();
        // Apply config on start if it exists
        ApplyConfiguration(configuration);
    }
    public void SetDensityLevel(DensityLevel level)
    {
        densityLevel = level;
        // Update spacing based on selected level
        var s = DetermineDensitySpacing(densityLevel);
        spacingX = s;
        spacingZ = s;

        // If you want immediate update in editor:
        #if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        #endif

        // Recompute & respawn if you do this at runtime

        // var pts = GeneratePointsData();
        // SpawnPointPrefabs(pts);
        system.ClearSystem();
        system.BuildSystem();
    }

    private void OnValidate()
    {
        if (activeShape != null)
        {
            activeShape.OnValidate(); // Pass validation down to the active shape
        }

        if(densityLevel == DensityLevel.High)
        {
            spacingX = spacingZ = 0.05f;    
        }
        else if(densityLevel == DensityLevel.Medium) {
            spacingX = spacingZ = 0.1f;
        }
        else if(densityLevel == DensityLevel.Low) {
            spacingX = spacingZ = 0.5f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Delegate gizmo drawing to the active shape
        if (activeShape != null)
        {
            activeShape.DrawGizmos(transform, showCurvePoints, curveResolution);
        }

        if (showGridCandidates) DrawGridCandidates();
        if (showColumnBounds) DrawColumnBoundaries();
        DrawSpawnedPoints();
        if (showStatistics) DrawStatisticsOverlay();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Applies UserConfig data to the generator and its active shape.
    /// Called by DistributionSystem.
    /// </summary>
    public void ApplyConfiguration(UserConfig config)
    {
        if (config == null) return;
        this.configuration = config;

        // Pass config data to the shape (if it supports it)
        if (activeShape is CurvedRectangleShape rectShape)
        {
            rectShape.plateWidthX = config.xSize;
            rectShape.plateHeightZ = config.ySize;
        }
        // else if (activeShape is CircularShape circShape) { /* set radius? */ }
    }

    /// <summary>
    /// Main logic function. Calculates all point data.
    /// This is called by the DistributionSystem.
    /// </summary>
    /// <returns>A list of calculated PointData.</returns>
    public List<PointData> GeneratePointsData()
    {
        if (activeShape == null)
        {
            Debug.LogError("<b>[PointGenerator]</b> No 'Active Shape' found!", this);
            return new List<PointData>();
        }
        
        if (density == null)
        {
            Debug.LogError("<b>[PointGenerator]</b> No 'DensityProfile' assigned!", this);
            return new List<PointData>();
        }
        
        ApplyConfiguration(configuration); // Ensure config is applied
        ClearDebugData();

        cachedMetrics = CalculateGridMetrics();
        List<PointData> points = CalculatePointData(cachedMetrics);
        
        // Update metrics with final counts
        cachedMetrics.AcceptedPoints = points.Count;
        cachedMetrics.DiscardedPoints = (cachedMetrics.Columns * cachedMetrics.Rows) - points.Count;

        LogBuildResults(cachedMetrics);
        return points;
    }
    
    /// <summary>
    /// Instantiates the 'pointPrefab' at each position.
    /// Called by the DistributionSystem.
    /// </summary>
    public void SpawnPointPrefabs(List<PointData> points)
    {
        if (pointPrefab == null)
        {
            Debug.LogWarning("<b>[PointGenerator]</b> No 'pointPrefab' assigned. Skipping prefab instantiation.", this);
            return;
        }

        ClearAllPoints(); // Clear any old prefabs
        
        foreach (var data in points)
        {
            SpawnPoint(data.position, data.name, data.rowIndex);
        }
    }

    [ContextMenu("Build (Generator Only)")]
    private void BuildGeneratorOnly()
    {
        var points = GeneratePointsData();
        SpawnPointPrefabs(points);
    }

    [ContextMenu("Clear All Points")]
    public void ClearAllPoints()
    {
        for (int i = spawnedPoints.Count - 1; i >= 0; i--)
        {
            if (spawnedPoints[i] != null)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(spawnedPoints[i].gameObject);
                else
                    DestroyImmediate(spawnedPoints[i].gameObject);
                #else
                Destroy(spawnedPoints[i].gameObject);
                #endif
            }
        }
        spawnedPoints.Clear();
    }

    private void ClearDebugData()
    {
        debugCandidatePoints.Clear();
        debugPointAccepted.Clear();
    }

    public int GetSpawnedCount() => spawnedPoints.Count;

    #endregion

    #region Grid Calculation

    private GridMetrics CalculateGridMetrics()
    {
        Bounds bounds = activeShape.GetBounds(transform);
        var metrics = new GridMetrics
        {
            Mode = gridSizing,
            Bounds = bounds,
            UsableWidth = bounds.size.x,
            UsableHeight = bounds.size.z,
            UsableArea = bounds.size.x * bounds.size.z
        };

        metrics.TargetPoints = activeShape.CalculateTargetPoints(density);

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

    private void CalculateFixedSpacingGrid(ref GridMetrics metrics)
    {
        var s = DetermineDensitySpacing(densityLevel);
        spacingX = spacingZ = s;

        metrics.Columns = Mathf.Max(1, Mathf.FloorToInt(metrics.UsableWidth / Mathf.Max(1e-6f, spacingX)) + 1);
        metrics.Rows = Mathf.Max(1, Mathf.FloorToInt(metrics.UsableHeight / Mathf.Max(1e-6f, spacingX)) + 1);
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

        metrics.Columns = cols;
        metrics.Rows = rows;
        metrics.SpacingX = cols == 1 ? metrics.UsableWidth : metrics.UsableWidth / (cols - 1);
        metrics.SpacingZ = rows == 1 ? metrics.UsableHeight : metrics.UsableHeight / (rows - 1);
    }

    private float DetermineDensitySpacing(DensityLevel level)
    {
        switch (level)
        {
            case DensityLevel.High:     return 0.03f;
            case DensityLevel.Medium:   return 0.04f;
            case DensityLevel.Low:      return 0.05f;
            default:
                return 0.1f;
        }
    }

    private int SimulateAcceptedPoints(int cols, int rows, float dx, float dz)
    {
        int accepted = 0;
        float x0 = cachedMetrics.Bounds.min.x;
        float zTopEdge = cachedMetrics.Bounds.max.z;
        float zBotEdge = cachedMetrics.Bounds.min.z;
        
        for (int c = 0; c < cols; c++)
        {
            float x = cols == 1 ? (x0 + cachedMetrics.UsableWidth * 0.5f) : (x0 + c * dx);
            float u = CalculateUParameter(x, cachedMetrics.Bounds);
            var bounds = activeShape.GetVerticalBounds(u, transform);

            for (int r = 0; r < rows; r++)
            {
                float z = rows == 1
                    ? (zBotEdge + cachedMetrics.UsableHeight * 0.5f)
                    : (topToBottom ? (zTopEdge - r * dz) : (zBotEdge + r * dz));

                if (z >= bounds.min && z <= bounds.max)
                    accepted++;
            }
        }
        return accepted;
    }

    #endregion

    #region Point Generation

    /// <summary>
    /// This is the core loop that calculates points. It no longer spawns prefabs.
    /// </summary>
    private List<PointData> CalculatePointData(GridMetrics metrics)
    {
        var generatedPoints = new List<PointData>();
        
        float x0 = metrics.Bounds.min.x;
        float zTopEdge = metrics.Bounds.max.z;
        float zBotEdge = metrics.Bounds.min.z;
        float yPos = transform.position.y;

        for (int c = 0; c < metrics.Columns; c++)
        {
            float x = metrics.Columns == 1
                ? (x0 + metrics.UsableWidth * 0.5f)
                : (x0 + c * metrics.SpacingX);

            float u = CalculateUParameter(x, metrics.Bounds);
            var columnBounds = activeShape.GetVerticalBounds(u, transform);

            for (int r = 0; r < metrics.Rows; r++)
            {
                float baseZ = metrics.Rows == 1
                    ? (zBotEdge + metrics.UsableHeight * 0.5f)
                    : (topToBottom ? (zTopEdge - r * metrics.SpacingZ) : (zBotEdge + r * metrics.SpacingZ));

                Vector3 candidatePos = new Vector3(x, yPos, baseZ);
                bool isAccepted = baseZ >= columnBounds.min && baseZ <= columnBounds.max;

                if (showGridCandidates || showDebugInfo)
                {
                    debugCandidatePoints.Add(candidatePos);
                    debugPointAccepted.Add(isAccepted);
                }

                if (isAccepted)
                {
                    // Apply snapping to get final position
                    Vector3 finalPos = activeShape.ApplySnapping(
                        snappingMode,
                        candidatePos,
                        u,
                        columnBounds,
                        r,
                        metrics.Rows,
                        verticalAlignment,
                        topToBottom
                    );
                    
                    generatedPoints.Add(new PointData
                    {
                        position = finalPos,
                        name = $"C{c + 1}-R{r + 1}",
                        columnIndex = c,
                        rowIndex = r,
                        totalRows = metrics.Rows,
                        uParameter = u,
                        columnBounds = columnBounds
                    });
                }
            }
        }
        return generatedPoints;
    }
    
    /// <summary>
    /// This method is now only for spawning debug prefabs.
    /// </summary>
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
            // Fallback for when no prefab is assigned
            var obj = new GameObject(pointName);
            obj.transform.SetParent(transform, false);
            obj.transform.position = position;
            pointTransform = obj.transform;
        }

        if (colorCodeByRow)
        {
            var debugInfo = pointTransform.gameObject.AddComponent<PointDebugInfo>();
            debugInfo.rowIndex = rowIndex;
        }

        spawnedPoints.Add(pointTransform);
    }

    #endregion

    #region Helpers

    private float CalculateUParameter(float worldX, Bounds bounds)
    {
        float uDirect = Mathf.InverseLerp(bounds.min.x, bounds.max.x, worldX);
        return leftToRight ? uDirect : 1f - uDirect;
    }

    #endregion

    #region Debug & Logging

    private void LogBuildResults(GridMetrics metrics)
    {
        if (!showDebugInfo) return;

        float efficiency = metrics.TargetPoints > 0 ? (metrics.AcceptedPoints / (float)metrics.TargetPoints) * 100f : 0f;
        float fillRate = (metrics.Columns * metrics.Rows) > 0 ? (metrics.AcceptedPoints / (float)(metrics.Columns * metrics.Rows)) * 100f : 0f;
        
        string report = $"<b><color=cyan>═══ PointGenerator Build Report ═══</color></b>\n\n" +
                        $"<b>Shape:</b> <color=yellow>{activeShape.GetType().Name}</color>\n" +
                        $"<b>Density:</b> <color=yellow>{(density ? density.name : "N/A")}</color>\n" +
                        $"<b>Usable Area:</b> {metrics.UsableArea:F4}m²\n\n" +

                        $"<b>Target Calculation:</b>\n" +
                        $"  • Target: <color=yellow>{metrics.TargetPoints} points</color>\n\n" +

                        $"<b>Grid Configuration:</b>\n" +
                        $"  • Mode: <color=cyan>{metrics.Mode}</color>\n" +
                        $"  • Grid: {metrics.Columns}×{metrics.Rows} = {metrics.Columns * metrics.Rows} candidates\n" +
                        $"  • Spacing: dx={metrics.SpacingX:F4}m, dz={metrics.SpacingZ:F4}m\n\n" +

                        $"<b>Results:</b>\n" +
                        $"  • <color=green>Accepted: {metrics.AcceptedPoints} points</color>\n" +
                        $"  • <color=red>Rejected: {metrics.DiscardedPoints} points</color>\n" +
                        $"  • Efficiency: {efficiency:F1}% (vs target)\n" +
                        $"  • Fill Rate: {fillRate:F1}% (of grid)\n";

        Debug.Log(report);
    }
    
    // --- Gizmo Drawing Methods ---

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
        if (activeShape == null) return;
        
        UnityEditor.Handles.color = new Color(1f, 1f, 0f, 0.3f);
        int columns = cachedMetrics.Columns > 0 ? cachedMetrics.Columns : 10;
        float x0 = cachedMetrics.Bounds.min.x;
        
        for (int c = 0; c < columns; c++)
        {
            float x = columns == 1 ? (x0 + cachedMetrics.UsableWidth * 0.5f) : (x0 + c * cachedMetrics.SpacingX);
            float u = CalculateUParameter(x, cachedMetrics.Bounds);
            var bounds = activeShape.GetVerticalBounds(u, transform);
            
            Vector3 top = new Vector3(x, transform.position.y, bounds.max);
            Vector3 bottom = new Vector3(x, transform.position.y, bounds.min);
            
            UnityEditor.Handles.DrawLine(top, bottom, 2f);
        }
        #endif
    }

    private void DrawStatisticsOverlay()
    {
        #if UNITY_EDITOR
        if (cachedMetrics.AcceptedPoints == 0) return;

        Vector3 centerPos = transform.position + Vector3.up * 0.1f;
        string stats = $"Points: {cachedMetrics.AcceptedPoints}/{cachedMetrics.TargetPoints}\n" +
                       $"Grid: {cachedMetrics.Columns}×{cachedMetrics.Rows}\n" +
                       $"Efficiency: {(cachedMetrics.TargetPoints > 0 ? (cachedMetrics.AcceptedPoints / (float)cachedMetrics.TargetPoints * 100f) : 0f):F1}%";
        
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

    #endregion
}


// This helper class is still needed by PointGenerator,
// so I'm including it here for completeness, though it's unchanged.
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


