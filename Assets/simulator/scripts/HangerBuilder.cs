using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

/// <summary>
/// Refactored from 'HyparHangers'.
/// This class consumes PointData and builds hangers based on a swappable
/// 'HangerSurface' strategy for length calculation.
/// </summary>
[ExecuteAlways]
public class HangerBuilder : MonoBehaviour
{
    #region Configuration

    [Header("General Settings")]
    public UserConfig config; // This is now set by DistributionSystem

    [Header("Source")]
    [SerializeField, Tooltip("The PointGenerator that provides the anchor points.")]
    private PointGenerator pointGenerator;

    // --- MODIFIED: Added FlatSurface ---
    public enum SurfaceType { Sinusoidal, Spherical, Flat , ProjectedSphere , ProjectedDoubleSphereSurface , projectedFunnelSurface , CircularRingSurface , CircularRingSurfaceFull} 

    [SerializeField, Tooltip("Select the 3D surface equation to use for hanger lengths.")]
    private SurfaceType selectedSurface = SurfaceType.Sinusoidal;
    
    [SerializeField, Tooltip("Settings for the Sinusoidal surface.")]
    public SinusoidalSurface sinusoidalSurface = new SinusoidalSurface();

    [SerializeField, Tooltip("Settings for the Sinusoidal surface.")]
    public CircularRingChandelierSurface CircularRingSurface = new CircularRingChandelierSurface();

    [SerializeField, Tooltip("Settings for the Sinusoidal surface.")]
    public CircularRingChandelierSurfaceFull CircularRingSurfaceFull = new CircularRingChandelierSurfaceFull();

    [SerializeField, Tooltip("Settings for projecting anchor points onto a sphere.")]
    public ProjectedSphereSurface projectedSphereSurface = new ProjectedSphereSurface();
    
    [SerializeField, Tooltip("Settings for the Spherical surface (where hanger ENDS form a sphere).")]
    public SphericalSurface sphericalSurface = new SphericalSurface();
    
    [SerializeField, Tooltip("Settings for the Double Spherical surface.")]
    public ProjectedDoubleSphereSurface projectedDoubleSphereSurface = new ProjectedDoubleSphereSurface();
    // --- ADDED: Field for the new FlatSurface ---
    [SerializeField, Tooltip("Settings for a Funnel Surface ceiling surface.")]
    public ProjectedFunnelSurface projectedFunnelSurface = new ProjectedFunnelSurface();
    // --- ADDED: Field for the new FlatSurface ---
    [SerializeField, Tooltip("Settings for a simple flat ceiling surface.")]
    public FlatSurface flatSurface = new FlatSurface();
    
    /// <summary>
    /// Gets the currently selected surface strategy.
    /// </summary>
    private HangerSurface activeSurface;
    // {
    //     get
    //     {
    //         // --- MODIFIED: Added FlatSurface case ---         
    //         switch (selectedSurface)
    //         {
    //             case SurfaceType.CircularRingSurface:
    //                 return CircularRingSurface; 
    //             case SurfaceType.CircularRingSurfaceFull:
    //                 return CircularRingSurfaceFull; 
    //             case SurfaceType.projectedFunnelSurface:
    //                 return projectedFunnelSurface;
    //             case SurfaceType.ProjectedDoubleSphereSurface:
    //                 return projectedDoubleSphereSurface;
    //             case SurfaceType.ProjectedSphere: // This case was added
    //                 return projectedSphereSurface;
    //             case SurfaceType.Spherical:
    //                 return sphericalSurface;
    //             case SurfaceType.Flat:
    //                 return flatSurface;
    //             case SurfaceType.Sinusoidal:
    //             default:
    //                 return sinusoidalSurface;
    //         }
    //    }
    // }

    [Header("Units & Physics")]
    [SerializeField, Tooltip("Direction wires hang (normalized automatically)")]
    private Vector3 hangDirection = Vector3.down;

    [Header("Visual Rendering - Cylinder Wire")]
    [SerializeField, Tooltip("Material for wire cylinder")]
    private Material wireMaterial;

    [SerializeField, Range(0.0001f, 0.1f), Tooltip("Wire cylinder radius")]
    private float wireRadius = 0.001f;

    [SerializeField,Range(0.1f , 0.3f)]
    private float thresholdWireLength = 0.01f;

    [SerializeField] private Color wireColor = Color.white;

    [SerializeField, Tooltip("Number of radial segments for cylinder (higher = smoother)")]
    private int cylinderSegments = 8; 

    // [Header("End Weight Configuration")]
    // [SerializeField, Tooltip("List of prefabs for end weights/cubes (randomly selected)")]
    // private List<GameObject> endCubePrefabs = new List<GameObject>();

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
    
    [Header("Editor Settings")]
    [SerializeField, Tooltip("Recalculate surface gizmos live in the editor as values are changed.")]
    private bool liveUpdateInEditor = false;
    private bool isDirty = true; // Flag to trigger refresh

    #endregion

    #region Private Fields

    private List<Transform> createdHangers = new List<Transform>();
    private const string HANGER_PREFIX = "HANG_";
    private const string EXPORT_CONTAINER_NAME = "ExportedHangers";
    private BuildStatistics lastBuildStats;
    
    private List<PointData> editorPointData = new List<PointData>();

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
    /// This is the new entry point for building hangers.
    /// It is called by the DistributionSystem.
    /// </summary>
    public void BuildHangers(List<PointData> points)
    {
        if (!ValidateSetup()) return;
        
        if(config != null)
        {
            selectedSurface = config.hangerSurface;    
            activeSurface = selectedSurface switch
            {
                SurfaceType.CircularRingSurface => CircularRingSurface, 
                SurfaceType.CircularRingSurfaceFull => CircularRingSurfaceFull, 
                SurfaceType.projectedFunnelSurface => projectedFunnelSurface,
                SurfaceType.ProjectedDoubleSphereSurface => projectedDoubleSphereSurface,
                SurfaceType.ProjectedSphere => projectedSphereSurface,
                SurfaceType.Spherical => sphericalSurface,
                SurfaceType.Flat => flatSurface,
                SurfaceType.Sinusoidal => sinusoidalSurface,
                _ => sinusoidalSurface,
            };
        }

        if (clearOldOnBuild) ClearAllHangers();

        editorPointData = points; // Cache points for gizmos
        lastBuildStats = GenerateHangers(points);
        LogBuildResults();
    }
    
    [ContextMenu("Build / Rebuild Hangers (Standalone)")]
    private void BuildStandalone()
    {
        Debug.LogWarning("<b>[HangerBuilder]</b> Standalone build initiated. " +
            "This will try to find a PointGenerator in the scene. " +
            "The recommended way to build is via the 'DistributionSystem'.");
            
        if (pointGenerator == null)
        {
            pointGenerator = FindObjectOfType<PointGenerator>();
        }
        
        if (pointGenerator == null)
        {
            Debug.LogError("<b>[HangerBuilder]</b> Standalone build failed. " +
                "Could not find a 'PointGenerator' in the scene.", this);
            return;
        }
        
        // Run the full generation and build
        pointGenerator.ApplyConfiguration(this.config);
        List<PointData> points = pointGenerator.GeneratePointsData();
        BuildHangers(points);
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
        
        foreach (var hanger in createdHangers)
        {
            if (hanger != null && !toDestroy.Contains(hanger.gameObject))
            {
                toDestroy.Add(hanger.gameObject);
            }
        }

        for (int i = toDestroy.Count - 1; i >= 0; i--)
        {
            if (toDestroy[i] == null) continue;
            #if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(toDestroy[i]);
            else
                DestroyImmediate(toDestroy[i]);
            #else
            Destroy(toDestroy[i]);
            #endif
        }

        createdHangers.Clear();

        if (showDebugInfo)
            Debug.Log($"[HangerBuilder] Cleared {toDestroy.Count} hangers");
    }

    #region Export (Unchanged)
    
    [ContextMenu("Export Hangers to OBJ")]
    public void ExportToOBJ()
    {
        if (createdHangers == null || createdHangers.Count == 0)
        {
            Debug.LogWarning("[HyparHangers] No hangers to export! Build hangers first.");
            return;
        }

        GameObject exportContainer = new GameObject(EXPORT_CONTAINER_NAME);
        exportContainer.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        try
        {
            foreach (Transform hanger in createdHangers)
            {
                if (hanger == null) continue;
                var hangerCopy = new GameObject(hanger.name);
                hangerCopy.transform.SetParent(exportContainer.transform, false);
                hangerCopy.transform.SetPositionAndRotation(hanger.position, hanger.rotation);
                hangerCopy.transform.localScale = hanger.localScale;
                CopyMeshHierarchy(hanger, hangerCopy.transform, true);
            }

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

            string fullPath = Path.Combine(Application.persistentDataPath, exportPath).Replace("\\", "/").TrimEnd('/');
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);

            string objPath = $"{fullPath}/{exportFileName}.obj";
            OBJExporter.Export(exportContainer, objPath);

            Debug.Log(
                $"<b><color=green>═══ OBJ Export Successful ═══</color></b>\n" +
                $"  • Exported: {createdHangers.Count} hangers\n" +
                $"  • Format: <color=cyan>Wavefront OBJ</color>\n" +
                $"  • Saved to: <color=cyan>{objPath}</color>"
            );

            RevealInExplorer(objPath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HyparHangers] OBJ export failed: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            #if UNITY_EDITOR
            if (Application.isPlaying) Destroy(exportContainer);
            else DestroyImmediate(exportContainer);
            #else
            Destroy(exportContainer);
            #endif
        }
    }

    private void CopyMeshHierarchy(Transform source, Transform destination, bool meshOnly = false)
    {
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

        foreach (Transform child in source)
        {
            if (meshOnly)
            {
                bool hasMesh = child.GetComponent<MeshFilter>() != null || HasMeshInChildren(child);
                if (!hasMesh) continue;
            }

            var childCopy = new GameObject(child.name);
            childCopy.transform.SetParent(destination, false);
            childCopy.transform.localPosition = child.localPosition;
            childCopy.transform.localRotation = child.localRotation;
            childCopy.transform.localScale = child.localScale;

            CopyMeshHierarchy(child, childCopy.transform, meshOnly);
        }
    }

    private bool HasMeshInChildren(Transform t)
    {
        if (t.GetComponent<MeshFilter>() != null) return true;
        foreach (Transform child in t)
        {
            if (HasMeshInChildren(child)) return true;
        }
        return false;
    }

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

    #endregion

    #region Core Generation

    /// <summary>
    /// Modified to use the activeSurface strategy for length calculation.
    /// </summary>
    private BuildStatistics GenerateHangers(List<PointData> points)
    {
        if (activeSurface == null)
        {
            Debug.LogError("<b>[HangerBuilder]</b> No 'Active Surface' strategy selected!", this);
            return new BuildStatistics();
        }
        
        var stats = new BuildStatistics { TotalGridPoints = points.Count };
        Vector3 hangDir = hangDirection.normalized;

        foreach (PointData point in points)
        {
            // if (!point.isAccepted) continue; // Skip points that aren't valid
            
            string gridName = point.name.Trim();
            
            // --- MODIFIED: Use strategy for length ---
            float lengthMeters = activeSurface.CalculateLength(point, transform);

            string wireLabel = GenerateWireLabelFromGrid(gridName);
            CreateHanger(point.position, gridName, wireLabel, lengthMeters, hangDir);
            stats.Created++;
        }

        return stats;
    }

    /// <summary>
    /// Creates a single hanger prefab
    /// </summary>
    private void CreateHanger(Vector3 startPos, string gridName, string wireLabel, float length, Vector3 direction)
    {
        if(length <= thresholdWireLength) return;
        // The length passed in is the final calculated length.
        Vector3 endPos = startPos + (direction * length);

        GameObject hangerObj = new GameObject($"{HANGER_PREFIX}{wireLabel}");
        hangerObj.transform.SetParent(transform, false);
        hangerObj.transform.position = startPos; // Set hanger root to anchor point

        var info = hangerObj.AddComponent<HangerInfo>();
        info.gridPointName = gridName;
        info.wireLabel = wireLabel;
        info.wireLength = length;

        // Pass LOCAL positions to wire/weight creators
        Vector3 localStart = Vector3.zero;
        Vector3 localEnd = hangerObj.transform.InverseTransformPoint(endPos);

        CreateCylinderWire(hangerObj.transform, localStart, localEnd, wireLabel);
        CreateEndWeight(hangerObj.transform, localEnd, wireLabel);

        createdHangers.Add(hangerObj.transform);
    }
    
    private void CreateCylinderWire(Transform parent, Vector3 localStart, Vector3 localEnd, string label)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = $"Wire_{label}";
        cylinder.transform.SetParent(parent, false);

        Vector3 midpoint = (localStart + localEnd) / 2f;
        cylinder.transform.localPosition = midpoint; // Use localPosition

        float distance = Vector3.Distance(localStart, localEnd);
        Vector3 direction = (localEnd - localStart).normalized;

        cylinder.transform.localScale = new Vector3(wireRadius * 2f, distance / 2f, wireRadius * 2f);

        if (direction != Vector3.up && direction != Vector3.down)
            cylinder.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
        else if (direction == Vector3.down)
            cylinder.transform.rotation = Quaternion.Euler(180f, 0f, 0f);

        var renderer = cylinder.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (wireMaterial != null)
                renderer.material = wireMaterial;
            else
                renderer.material = new Material(Shader.Find("Standard")) { color = wireColor };
        }

        var collider = cylinder.GetComponent<Collider>();
        if (collider != null)
        {
            #if UNITY_EDITOR
            if (Application.isPlaying) Destroy(collider);
            else DestroyImmediate(collider);
            #else
            Destroy(collider);
            #endif
        }
    }
    
    private void CreateEndWeight(Transform parent, Vector3 localPosition, string label)
    {
        GameObject cube;
        var spawnData = config.GetAllCrystalVariantData();
        Debug.Log($"[HangerBuilder] Found {spawnData.Count} crystal variant data entries for end weight selection.");
        List<UserConfig.CrystalSpawnData> possiblePrefabs = new List<UserConfig.CrystalSpawnData>();
            
        foreach(var data in spawnData)
        {
            possiblePrefabs.Add(data);
        }

        int randomIndex = UnityEngine.Random.Range(0, possiblePrefabs.Count);
        UserConfig.CrystalSpawnData endCubePrefab = possiblePrefabs.Count > 0 ? possiblePrefabs[randomIndex] : new UserConfig.CrystalSpawnData();
        if (endCubePrefab.prefab != null)
        {
            cube = Instantiate(endCubePrefab.prefab, parent); // Instantiate with parent
            cube.name = $"Weight_{label}";
            cube.transform.localPosition = localPosition; // Use localPosition
            float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
            cube.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f); // Use localRotation

            // Apply the color from the selected variant data
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetColor("_glasscolor", endCubePrefab.color);
                Debug.Log($"[HangerBuilder] Applied color {endCubePrefab.color} to {cube.name}");
            }
        }
        else
        {
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Weight_{label}";
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition; // Use localPosition
            cube.transform.localScale = Vector3.one * Mathf.Max(0.02f, wireRadius * 6f);
            float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
            cube.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f); // Use localRotation

            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = new Material(Shader.Find("_glass")) { color = endCubeColor };

            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
                #else
                Destroy(collider);
                #endif
            }
        }
    }

// private void CreateEndWeight(Transform parent, Vector3 localPosition, string label)
//     {
//         GameObject cube;

//         // Use UserConfig to get a random crystal prefab based on spawn weights
//         if (config != null && config.crystalDatabase != null)
//         {
//             var spawnData = config.GetAllCrystalVariantData();
//             List<GameObject> possiblePrefabs = new List<GameObject>();
            
//             foreach(var data in spawnData)
//             {
//                 possiblePrefabs.Add(data.prefab);
//             }

//             if (possiblePrefabs.Count > 0 && possiblePrefabs[0] != null)
//             {
//                 int randomIndex = UnityEngine.Random.Range(0, possiblePrefabs.Count);
//                 GameObject endCubePrefab = possiblePrefabs[randomIndex];
//                 cube = Instantiate(endCubePrefab, parent); // Instantiate with parent
//                 cube.name = $"Weight_{label}";
//                 cube.transform.localPosition = localPosition; // Use localPosition
//                 float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
//                 cube.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f); // Use localRotation

//                 // Apply material and color from spawnData if available
//                 var renderer = cube.GetComponent<Renderer>();
//                 if (renderer != null && spawnData.material != null)
//                 {
//                     renderer.material = new Material(spawnData.material); // Create a new instance to avoid modifying shared material
//                     renderer.material.color = spawnData.color;
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning($"[HangerBuilder] No valid prefab found in UserConfig for crystal type {spawnData.crystalType}. Falling back to default cube.");
//                 cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//                 cube.name = $"Weight_{label}";
//                 cube.transform.SetParent(parent, false);
//                 cube.transform.localPosition = localPosition;
//                 cube.transform.localScale = Vector3.one * Mathf.Max(0.02f, wireRadius * 6f);
//                 float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
//                 cube.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f);

//                 var renderer = cube.GetComponent<Renderer>();
//                 if (renderer != null)
//                 {
//                     renderer.material = new Material(Shader.Find("Standard")) { color = Color.white };
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("[HangerBuilder] UserConfig or CrystalDatabase is not assigned. Falling back to default cube.");
//             cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
//             cube.name = $"Weight_{label}";
//             cube.transform.SetParent(parent, false);
//             cube.transform.localPosition = localPosition;
//             cube.transform.localScale = Vector3.one * Mathf.Max(0.02f, wireRadius * 6f);
//             float randomYRot = UnityEngine.Random.Range(rotationRange.x, rotationRange.y);
//             cube.transform.localRotation = Quaternion.Euler(0f, randomYRot, 0f);

//             var renderer = cube.GetComponent<Renderer>();
//             if (renderer != null)
//             {
//                 renderer.material = new Material(Shader.Find("Standard")) { color = Color.white };
//             }
//         }

//         // Remove collider if present
//         var collider = cube.GetComponent<Collider>();
//         if (collider != null)
//         {
//             #if UNITY_EDITOR
//             if (Application.isPlaying) Destroy(collider);
//             else DestroyImmediate(collider);
//             #else
//             Destroy(collider);
//             #endif
//         }
//     }

    #endregion

    #region Surface Mathematics (Unchanged logic, just moved)
    
    private string GenerateWireLabelFromGrid(string gridName)
    {
        // Simple case for sphere points
        if (gridName.StartsWith("SpherePoint_"))
        {
            return gridName;
        }

        var parsed = ParseGridPointName(gridName);
        if (!parsed.HasValue) return $"WIRE_{gridName}";

        int col = parsed.Value.col;
        int row = parsed.Value.row;
        
        char letter = (char)('A' + (col - 1));
        return $"{letter}{row}";
    }
    
    private (int col, int row)? ParseGridPointName(string name)
    {
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
        if (hangDirection.sqrMagnitude < 1e-6f)
            hangDirection = Vector3.down;
        else
            hangDirection = hangDirection.normalized;

        return true;
    }

    #endregion

    #region Logging (Unchanged)

    private void LogBuildResults()
    {
        if (!showDebugInfo) return;
        if (activeSurface == null) return;
        if (lastBuildStats.TotalGridPoints == 0) return; // Don't log empty builds

        string report = $"<b><color=cyan>═══ HangerBuilder Build Report ═══</color></b>\n\n" +
                       $"<b>Surface Statistics:</b>\n" +
                       $"  • <color=green>Created Hangers: {lastBuildStats.Created}</color>\n" +
                       $"  • Total Grid Points: {lastBuildStats.TotalGridPoints}\n\n" +
                       
                       $"<b>Surface Config:</b>\n" +
                       $"  • Strategy: <color=yellow>{activeSurface.GetType().Name}</color>\n" +
                       $"  • Wire Radius: {wireRadius:F4}m\n";

        Debug.Log(report);
    }

    #endregion

    #region Unity Callbacks

    private void Update()
    {
        if (Application.isPlaying) return; // Only for editor logic
        if (!liveUpdateInEditor || !isDirty) return;
        
        if (pointGenerator == null) 
        {
            // Try to find it if we're live-updating and it's missing
            pointGenerator = FindObjectOfType<PointGenerator>();
            if (pointGenerator == null) return; // Still can't find it
        }
        
        // Get fresh point data from the generator
        // This ensures our surface gizmo matches the point layout
        editorPointData = pointGenerator.GeneratePointsData();
        isDirty = false;
    }

    private void OnValidate()
    {
        if (hangDirection.sqrMagnitude < 1e-6f)
            hangDirection = Vector3.down;
        else
            hangDirection = hangDirection.normalized;
            
        if (activeSurface != null)
        {
            activeSurface.OnValidate();
        }
        
        if (Application.isPlaying) return;

        if (liveUpdateInEditor)
        {
            isDirty = true;
        }
        
        // Auto-find generator if not set
        if (pointGenerator == null)
        {
            pointGenerator = FindObjectOfType<PointGenerator>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Draw wire gizmos (unchanged logic, but for 'createdHangers')
        if (drawGizmoLines)
        {
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
                    // Hanger object is at start pos, wire is child
                    Vector3 start = hanger.position;
                    // End point is at the wire's (midpoint * 2) relative to the parent
                    Vector3 end = hanger.TransformPoint(wireCylinder.transform.localPosition * 2f); 
                    
                    Gizmos.DrawLine(start, end);
                    Gizmos.DrawWireSphere(end, wireRadius * 2f);
                }
            }
        }

        // 2. Draw hang direction (unchanged)
        if (pointGenerator != null) // Use point generator as anchor
        {
            Gizmos.color = Color.red;
            Vector3 center = pointGenerator.transform.position;
            Gizmos.DrawRay(center, hangDirection.normalized * 0.2f);
            Gizmos.DrawWireSphere(center + hangDirection.normalized * 0.2f, 0.02f);
        }

        // 3. --- MODIFIED: Delegate surface gizmo drawing ---
        if (activeSurface != null && pointGenerator != null)
        {
            if (liveUpdateInEditor && !Application.isPlaying)
            {
                 // In live mode, use the fresh editorPointData
                if(isDirty) // If settings changed, force a data refresh
                {
                    editorPointData = pointGenerator.GeneratePointsData();
                    isDirty = false;
                }
                activeSurface.DrawGizmos(editorPointData, transform);
            }
            else if (!Application.isPlaying) // In editor but not live, draw last known
            {
                if(editorPointData == null || editorPointData.Count == 0)
                {
                    // Try to get data once for preview
                    editorPointData = pointGenerator.GeneratePointsData();
                }
                activeSurface.DrawGizmos(editorPointData, transform);
            }
            // (In play mode, gizmos are drawn based on 'createdHangers' logic above)
        }
    }

    #endregion
}


/// <summary>
/// Component attached to each hanger to store metadata. (Unchanged)
/// </summary>
public class HangerInfo : MonoBehaviour
{
    public string gridPointName;
    public string wireLabel;
    public float wireLength;
}

