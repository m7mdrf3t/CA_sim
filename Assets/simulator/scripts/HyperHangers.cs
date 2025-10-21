using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

/// <summary>
/// Hyperbolic Paraboloid Hanger System with Chandelier Effect
/// Creates wire hangers with varying lengths and rotated end prefabs
/// </summary>
[ExecuteAlways]
public class HyparHangers : MonoBehaviour
{
    #region Configuration

    [Header("3D Surface Configuration")]
    [SerializeField, Tooltip("Enable 3D surface length calculation")]
    private bool useSurfaceLengths = true;

    [SerializeField, Tooltip("Surface curvature X (a) - Smaller = Sharper")]
    private float surfaceA = 20f;

    [SerializeField, Tooltip("Surface curvature Y (b) - Smaller = Sharper")]
    private float surfaceB = 30f;

    [SerializeField, Tooltip("Ceiling height (Z) where anchors are")]
    private float ceilingHeight = 3.0f;

    [SerializeField, Tooltip("Surface center X,Y position")]
    private Vector2 surfaceCenter = Vector2.zero;

    [SerializeField, Tooltip("Amplitude multiplier for height variation")]
    private float heightAmplitude = 1.0f;

    public bool addRandomization = false;
    [SerializeField]
    private float RandomVariationRation;

    [Header("Anchor Points")]
    [SerializeField, Tooltip("WireDistributor GameObject with generated grid points")]
    private Transform anchorRoot;

    [Header("Units & Physics")]
    [SerializeField, Tooltip("Direction wires hang (normalized automatically)")]
    private Vector3 hangDirection = Vector3.down;

    [Header("Visual Rendering - Cylinder Wire")]
    [SerializeField, Tooltip("Material for wire cylinder")]
    private Material wireMaterial;
    
    [SerializeField, Range(0.001f, 0.1f), Tooltip("Wire cylinder radius")]
    private float wireRadius = 0.005f;
    
    [SerializeField] private Color wireColor = Color.white;
    
    [SerializeField, Tooltip("Number of radial segments for cylinder (higher = smoother)")]
    private int cylinderSegments = 8;

    [Header("End Weight Configuration")]
    [SerializeField, Tooltip("List of prefabs for end weights/cubes (randomly selected)")]
    private List<GameObject> endCubePrefabs = new List<GameObject>();
        
    [SerializeField] private Color endCubeColor = new Color(0.8f, 0.8f, 0.2f);

    [SerializeField, Tooltip("Random rotation range for end weights (degrees)")]
    private Vector2 rotationRange = new Vector2(0f, 360f);

    [Header("Export Configuration")]
    [SerializeField, Tooltip("Additional objects to export with the hangers")]
    private List<GameObject> additionalExportObjects = new List<GameObject>();
    
    [SerializeField, Tooltip("Export file name (without extension)")]
    private string exportFileName = "HyparHangersExport";
    
    [SerializeField, Tooltip("Export path relative to persistent data path")]
    private string exportPath = "Exports";

    [Header("Advanced Options")]
    [SerializeField, Tooltip("Clear existing hangers before rebuild")]
    private bool clearOldOnBuild = true;
    
    [Header("Debug Visualization")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool drawGizmoLines = true;
    [SerializeField] private Color gizmoColor = Color.yellow;

    #endregion

    #region Private Fields

    private readonly List<Transform> createdHangers = new List<Transform>();
    private const string HANGER_PREFIX = "HANG_";
    private const string EXPORT_CONTAINER_NAME = "ExportedHangers";
    private BuildStatistics lastBuildStats;

    #endregion

    #region Data Structures

    public struct BuildStatistics
    {
        public int Created;
        public int TotalGridPoints;
        
        public override string ToString() =>
            $"Created: {Created}, Total Grid Points: {TotalGridPoints}";
    }

    #endregion

    #region Public API

    /// <summary>
    /// Builds or rebuilds all wire hangers from grid points using surface math
    /// </summary>
    [ContextMenu("Build / Rebuild Hangers")]
    public void Build()
    {
        if (!ValidateSetup()) return;

        if (clearOldOnBuild) ClearAllHangers();

        lastBuildStats = GenerateHangers();
        LogBuildResults();
    }

    /// <summary>
    /// Clears all created hangers.
    /// </summary>
    [ContextMenu("Clear Hangers")]
    public void ClearAllHangers()
    {
        var toDestroy = new List<GameObject>();
        
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (child.name.StartsWith(HANGER_PREFIX, StringComparison.OrdinalIgnoreCase))
                toDestroy.Add(child.gameObject);
        }

        foreach (var obj in toDestroy)
        {
            if (obj == null) continue;
            Destroy(obj);
        }

        createdHangers.Clear();
        
        if (showDebugInfo)
            Debug.Log($"[HyparHangers] Cleared {toDestroy.Count} hangers");
    }

    /// <summary>
    /// Exports hangers to OBJ format (works in Play mode and builds)
    /// </summary>
    [ContextMenu("Export Hangers to OBJ")]
    public void ExportToOBJ()
    {
        if (createdHangers == null || createdHangers.Count == 0)
        {
            Debug.LogWarning("[HyparHangers] No hangers to export! Build hangers first.");
            return;
        }

        // Create export container
        GameObject exportContainer = new GameObject(EXPORT_CONTAINER_NAME);
        exportContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        try
        {
            // Copy all hangers
            foreach (Transform hanger in createdHangers)
            {
                if (hanger == null) continue;

                var hangerCopy = new GameObject(hanger.name);
                hangerCopy.transform.SetParent(exportContainer.transform, false);
                hangerCopy.transform.SetPositionAndRotation(hanger.position, hanger.rotation);
                hangerCopy.transform.localScale = hanger.localScale;

                CopyMeshHierarchy(hanger, hangerCopy.transform, true);
            }

            // Copy additional objects
            if (additionalExportObjects != null)
            {
                foreach (GameObject obj in additionalExportObjects)
                {
                    if (obj == null) continue;

                    var objCopy = new GameObject(obj.name);
                    objCopy.transform.SetParent(exportContainer.transform, false);
                    objCopy.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
                    objCopy.transform.localScale = obj.transform.localScale;

                    CopyMeshHierarchy(obj.transform, objCopy.transform);
                }
            }

            // Determine export path
            string fullPath = Path.Combine(Application.persistentDataPath, exportPath).Replace("\\", "/").TrimEnd('/');
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            string objPath = $"{fullPath}/{exportFileName}.obj";
            OBJExporter.Export(exportContainer, objPath);

            Debug.Log(
                $"<b><color=green>═══ OBJ Export Successful ═══</color></b>\n" +
                $"  • Exported: {createdHangers.Count} hangers\n" +
                $"  • Format: <color=cyan>Wavefront OBJ</color>\n" +
                $"  • Saved to: <color=cyan>{objPath}</color>\n" +
                $"  • Compatible with: Blender, Maya, 3DS Max, Unity, etc."
            );

            // Open file location
            RevealInExplorer(objPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HyparHangers] OBJ export failed: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            Destroy(exportContainer);
        }
    }

    /// <summary>
    /// Recursively copies mesh hierarchy, optionally filtering out non-mesh objects
    /// </summary>
    private void CopyMeshHierarchy(Transform source, Transform destination, bool meshOnly = false)
    {
        // Copy MeshFilter and MeshRenderer from source
        var sourceMeshFilter = source.GetComponent<MeshFilter>();
        var sourceMeshRenderer = source.GetComponent<MeshRenderer>();

        if (sourceMeshFilter != null && sourceMeshFilter.sharedMesh != null)
        {
            var destMeshFilter = destination.gameObject.AddComponent<MeshFilter>();
            destMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            if (sourceMeshRenderer != null)
            {
                var destMeshRenderer = destination.gameObject.AddComponent<MeshRenderer>();
                destMeshRenderer.sharedMaterials = sourceMeshRenderer.sharedMaterials;
            }
        }

        // Recursively copy children
        foreach (Transform child in source)
        {
            if (meshOnly)
            {
                var childMeshFilter = child.GetComponent<MeshFilter>();
                bool hasMeshInHierarchy = childMeshFilter != null && childMeshFilter.sharedMesh != null;

                if (!hasMeshInHierarchy)
                {
                    hasMeshInHierarchy = HasMeshInChildren(child);
                }

                if (!hasMeshInHierarchy)
                    continue;
            }

            var childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(destination, false);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;

            CopyMeshHierarchy(child, childCopy.transform, meshOnly);
        }
    }

    /// <summary>
    /// Checks if transform or any of its children have a mesh
    /// </summary>
    private bool HasMeshInChildren(Transform t)
    {
        if (t.GetComponent<MeshFilter>() != null)
            return true;

        foreach (Transform child in t)
        {
            if (HasMeshInChildren(child))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Opens file location in system explorer
    /// </summary>
    private void RevealInExplorer(string path)
    {
#if UNITY_STANDALONE_WIN
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path.Replace("/", "\\")}\"");
#elif UNITY_STANDALONE_OSX
        System.Diagnostics.Process.Start("open", $"-R \"{path}\"");
#elif UNITY_STANDALONE_LINUX
        System.Diagnostics.Process.Start("xdg-open", Path.GetDirectoryName(path));
#else
        Debug.Log($"Export saved to: {path}");
#endif
    }

    #endregion

    #region Core Generation

    private BuildStatistics GenerateHangers()
    {
        var stats = new BuildStatistics { TotalGridPoints = anchorRoot.childCount };
        Vector3 hangDir = hangDirection.normalized;

        foreach (Transform gridPoint in anchorRoot)
        {
            if (gridPoint == null) continue;

            string gridName = gridPoint.name.Trim();
            string wireLabel;
            float lengthMeters;

            // DYNAMIC SURFACE-BASED LENGTH CALCULATION
            lengthMeters = CalculateSurfaceLength(gridPoint.position) * heightAmplitude;

            // Generate wire label from grid name
            wireLabel = GenerateWireLabelFromGrid(gridName);

            // ALWAYS CREATE HANGER
            CreateHanger(gridPoint, gridName, wireLabel, lengthMeters, hangDir);
            stats.Created++;
        }

        return stats;
    }

    private void CreateHanger(Transform gridPoint, string gridName, string wireLabel, float length, Vector3 direction)
    {
        Vector3 startPos = gridPoint.position;

        // Calculate end position with random height adjustment
        float randomHeightAdjustment = UnityEngine.Random.Range(-0.5f * heightAmplitude, 0.5f * heightAmplitude);
        Vector3 endPos = startPos + (direction * (length + randomHeightAdjustment));

        // Create container
        GameObject hangerObj = new GameObject($"{HANGER_PREFIX}{wireLabel}");
        hangerObj.transform.SetParent(transform, false);

        // Store grid reference with adjusted length
        var info = hangerObj.AddComponent<HangerInfo>();
        info.gridPointName = gridName;
        info.wireLabel = wireLabel;
        info.wireLength = length + randomHeightAdjustment;

        // Create cylinder wire
        CreateCylinderWire(hangerObj.transform, startPos, endPos, wireLabel);

        // Add end weight with random rotation
        CreateEndWeight(hangerObj.transform, endPos, wireLabel);

        createdHangers.Add(hangerObj.transform);
    }
    
    private void CreateCylinderWire(Transform parent, Vector3 start, Vector3 end, string label)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = $"Wire_{label}";
        cylinder.transform.SetParent(parent, false);

        // Position at midpoint
        Vector3 midpoint = (start + end) / 2f;
        cylinder.transform.position = midpoint;

        // Calculate length and orientation
        float distance = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        // Scale: cylinder's default height is 2 units along Y-axis
        cylinder.transform.localScale = new Vector3(wireRadius * 2f, distance / 2f, wireRadius * 2f);

        // Rotate to align with direction
        if (direction != Vector3.up && direction != Vector3.down)
        {
            cylinder.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        }
        else if (direction == Vector3.down)
        {
            cylinder.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
        }

        // Apply material
        var renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (wireMaterial != null)
            {
                renderer.material = wireMaterial;
            }
            else
            {
                renderer.material = new Material(Shader.Find("Standard")) { color = wireColor };
            }
        }

        // Remove collider for performance
        var collider = cylinder.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void CreateEndWeight(Transform parent, Vector3 position, string label)
    {
        GameObject cube;

        // Select a random prefab from the list if available    
        int randomIndex = UnityEngine.Random.Range(0, endCubePrefabs.Count);
        GameObject endCubePrefab = endCubePrefabs.Count > 0 ? endCubePrefabs[randomIndex] : null;

        if (endCubePrefab != null)
        {
            cube = Instantiate(endCubePrefab, position, Quaternion.identity, parent);
            cube.name = $"Weight_{label}";

            // Apply random rotation for chandelier effect
            float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
            cube.transform.rotation = Quaternion.Euler(0f, randomYRot, 0f);
        }
        else
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Weight_{label}";
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one * Mathf.Max(0.02f, wireRadius * 6f);

            // Apply random rotation for chandelier effect
            float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
            cube.transform.rotation = Quaternion.Euler(0f, randomYRot, 0f);

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard")) { color = endCubeColor };
            }

            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }

    #endregion

    #region Surface Mathematics

    /// <summary>
    /// Generates wire label directly from grid name (e.g., "C1-R5" → "A1")
    /// </summary>
    private string GenerateWireLabelFromGrid(string gridName)
    {
        // Parse grid: "C1-R5" → Column=1, Row=5
        var parsed = ParseGridPointName(gridName);
        if (!parsed.HasValue) return $"WIRE_{gridName}";

        int col = parsed.Value.col;
        int row = parsed.Value.row;
        
        // Convert to wire label: A1, A2, B1, B2... (Sequential)
        char letter = (char)('A' + (col - 1));
        return $"{letter}{row}";
    }

    /// <summary>
    /// Calculates wire length based on hyperbolic paraboloid with dynamic and random variation
    /// Z = heightAmplitude * [(x²/a²) - (y²/b²)] + random offset
    /// Length = |ceilingHeight - Z_surface|
    /// </summary>
    private float CalculateSurfaceLength(Vector3 anchorPos)
    {
        // Get relative X,Y position on surface
        float x = anchorPos.x - surfaceCenter.x;
        float y = anchorPos.y - surfaceCenter.y;

        // Enhanced hyperbolic paraboloid with amplitude
        float baseSurfaceZ = heightAmplitude * ((x * x) / (surfaceA * surfaceA) - (y * y) / (surfaceB * surfaceB));

        // Add random height offset for chandelier effect
        float randomOffset = UnityEngine.Random.Range(-RandomVariationRation, RandomVariationRation) + UnityEngine.Random.Range(-RandomVariationRation, RandomVariationRation);

        if (addRandomization)
        {
            float zSurface = baseSurfaceZ + randomOffset;   
            float length = Mathf.Abs(ceilingHeight - zSurface);
            return Mathf.Max(0.1f, length);
        }
        else
        {
            float length = Mathf.Abs(ceilingHeight - baseSurfaceZ);
            return Mathf.Max(0.1f, length);
        }
    }
    
    private (int col, int row)? ParseGridPointName(string name)
    {
        // Parse names like "C1-R5", "C12-R3", etc.
        var match = System.Text.RegularExpressions.Regex.Match(name, @"C(\d+)-R(\d+)");
        if (match.Success)
        {
            int col = int.Parse(match.Groups[1].Value);
            int row = int.Parse(match.Groups[2].Value);
            return (col, row);
        }
        return null;
    }

    #endregion

    #region Setup Validation

    private bool ValidateSetup()
    {
        if (anchorRoot == null)
        {
            Debug.LogError("[HyparHangers] Missing 'anchorRoot'! Assign the WireDistributor GameObject.");
            return false;
        }

        if (anchorRoot.childCount == 0)
        {
            Debug.LogWarning("[HyparHangers] Anchor root has no children. Build the distributor points first!");
            return false;
        }

        return true;
    }

    #endregion

    #region Logging

    private void LogBuildResults()
    {
        if (!showDebugInfo) return;

        string report = $"<b><color=cyan>═══ HyparHangers Build Report ═══</color></b>\n\n" +
                       $"<b>Surface Statistics:</b>\n" +
                       $"  • <color=green>Created Hangers: {lastBuildStats.Created}</color>\n" +
                       $"  • Total Grid Points: {lastBuildStats.TotalGridPoints}\n\n" +
                       
                       $"<b>Surface Config:</b>\n" +
                       $"  • Curvature X (a): <color=yellow>{surfaceA}</color>\n" +
                       $"  • Curvature Y (b): <color=yellow>{surfaceB}</color>\n" +
                       $"  • Ceiling Height: {ceilingHeight}m\n" +
                       $"  • Surface Center: ({surfaceCenter.x:F1}, {surfaceCenter.y:F1})\n" +
                       $"  • Height Amplitude: {heightAmplitude}\n" +
                       $"  • Wire Radius: {wireRadius:F4}m\n";

        Debug.Log(report);
    }

    #endregion

    #region Unity Callbacks

    private void OnValidate()
    {
        if (hangDirection.sqrMagnitude < 1e-6f)
            hangDirection = Vector3.down;
        else
            hangDirection = hangDirection.normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmoLines) return;

        Gizmos.color = gizmoColor;

        foreach (Transform hanger in createdHangers)
        {
            if (hanger == null) continue;

            var info = hanger.GetComponent<HangerInfo>();
            if (info == null) continue;

            // Find wire cylinder
            Transform wireCylinder = hanger.Find($"Wire_{info.wireLabel}");
            if (wireCylinder != null)
            {
                // Get start and end positions from cylinder
                Vector3 cylinderPos = wireCylinder.position;
                Vector3 direction = wireCylinder.up;
                float halfLength = wireCylinder.localScale.y;

                Vector3 start = cylinderPos - direction * halfLength;
                Vector3 end = cylinderPos + direction * halfLength;

                Gizmos.DrawLine(start, end);
                Gizmos.DrawWireSphere(end, wireRadius * 2f);
            }
        }

        if (anchorRoot != null)
        {
            Gizmos.color = Color.red;
            Vector3 center = anchorRoot.position;
            Gizmos.DrawRay(center, hangDirection.normalized * 0.2f);
            Gizmos.DrawWireSphere(center + hangDirection.normalized * 0.2f, 0.02f);

            // Surface visualization
            Gizmos.color = Color.magenta;
            foreach (Transform pt in anchorRoot)
            {
                float baseLength = CalculateSurfaceLength(pt.position);
                float randomOffset = UnityEngine.Random.Range(-0.5f * heightAmplitude, 0.5f * heightAmplitude);
                float totalLength = baseLength + randomOffset;
                Vector3 surfacePos = new Vector3(pt.position.x, pt.position.y, ceilingHeight - totalLength);
                Gizmos.DrawWireSphere(surfacePos, 0.01f);
                Gizmos.DrawLine(pt.position, surfacePos);
            }
        }
    }

    #endregion
}

/// <summary>
/// Component attached to each hanger to store metadata.
/// </summary>
public class HangerInfo : MonoBehaviour
{
    public string gridPointName;
    public string wireLabel;
    public float wireLength;
}
