using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public enum ShapeType
{
    Box,
    Cylinder
}

public class BoxSpotsGenerator : MonoBehaviour
{

    [Header("Configuration")]
    public UserConfig config;

    [Header("Shape Configuration")]
    [Tooltip("Choose between box or cylinder base")]
    public ShapeType shapeType;

    [Header("Box/Cylinder Dimensions (meters) — Z locked to 1")]

    [Header("Box Dimensions (meters) — Z locked to 1")]
    public float xSize = 1f;
    public float ySize = 1f;
    [Tooltip("Always forced to 1m height. Shown for clarity.")]
    public float zSize = 1f;

    [Tooltip("Cylinder radius (auto-calculated from box dimensions or manual)")]
    public float cylinderRadius = 0.5f;

    [Tooltip("Auto-calculate radius to fit box dimensions")]
    public bool autoCalculateRadius = true;

    [Header("Spot Distribution")]
    [Tooltip("Base density: spots per square meter")]
    [Range(1, 100)]
    public int baseSpotsPerSquareMeter = 16;
    
    [Tooltip("Distribution pattern for spots")]
    public DistributionPattern pattern = DistributionPattern.Grid;

    [Header("Placement Configuration")]
    [Tooltip("Height of spots above the inside bottom (meters)")]
    [Range(0f, 0.99f)]
    public float spotHeightFromBottom = 0.01f;

    [Tooltip("Padding from X edges (left/right) in meters")]
    [Range(0f, 0.5f)]
    public float paddingX = 0.0f;

    [Tooltip("Padding from Y edges (front/back) in meters")]
    [Range(0f, 0.5f)]
    public float paddingY = 0.0f;

    [Tooltip("Random position jitter to break perfect alignment")]
    [Range(0f, 0.5f)]
    public float jitter = 0f;

    [Header("Spot Appearance")]
    public GameObject spotPrefab;
    [Range(0.001f, 0.5f)]
    public float spotScale = 0.05f;
    
    [Tooltip("Apply random rotation to spots")]
    public bool randomRotation = false;
    
    [Tooltip("Vary spot scale slightly for organic look")]
    [Range(0f, 0.5f)]
    public float scaleVariation = 0f;

    [Header("Box Appearance")]
    public Material boxMaterial;
    
    [Tooltip("Make box semi-transparent for easier spot visibility")]
    public bool transparentBox = true;
    
    [Tooltip("Box color tint")]
    public Color boxColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

    [Header("Transform")]
    [Tooltip("Offset the generated box & spots")]
    public Vector3 boxOffset = Vector3.zero;

    [Header("Advanced Options")]
    [Tooltip("Rebuild automatically when values change")]
    public bool autoRegenerate = true;
    
    [Tooltip("Seed for reproducible random generation")]
    public int randomSeed = 0;
    
    [Tooltip("Use seed for reproducible results")]
    public bool useSeed = false;
    
    [Tooltip("Show gizmos in scene view")]
    public bool showGizmos = true;
    
    [Tooltip("Avoid spot overlap by minimum distance check")]
    public bool avoidOverlap = false;
    
    [Range(0f, 0.2f)]
    [Tooltip("Minimum distance between spots when avoiding overlap")]
    public float minSpotDistance = 0.05f;

    // Internals
    private GameObject boxObject;
    private GameObject spotsParent;
    private List<Vector3> spotPositions = new List<Vector3>();

    public enum DistributionPattern
    {
        Grid,
        HexagonalPacked,
        PoissonDisc,
        Random
    }

    private void OnValidate()
    {
        // Enforce constraints
        zSize = 1f;
        xSize = Mathf.Max(0.01f, xSize);
        ySize = Mathf.Max(0.01f, ySize);
        shapeType = config.BaseType;
        
        // Auto-calculate cylinder radius to fit in box dimensions
        if (shapeType == ShapeType.Cylinder && autoCalculateRadius)
        {
            // Use the smaller dimension to ensure cylinder fits
            cylinderRadius = Mathf.Min(xSize, ySize) * 0.5f;
        }
        cylinderRadius = Mathf.Max(0.01f, cylinderRadius);

        spotHeightFromBottom = Mathf.Clamp(spotHeightFromBottom, 0f, zSize - 0.001f);
        paddingX = Mathf.Clamp(paddingX, 0f, xSize * 0.49f);
        paddingY = Mathf.Clamp(paddingY, 0f, ySize * 0.49f);
        baseSpotsPerSquareMeter = Mathf.Max(1, baseSpotsPerSquareMeter);
        spotScale = Mathf.Max(0.001f, spotScale);
        shapeType = config.BaseType;
        if (autoRegenerate)
            Generate();
    }

    private void Start()
    {
        xSize = config.xSize;
        ySize = config.ySize;
        shapeType = config.BaseType;    
        // if (!autoRegenerate)
           // Generate();
    }

    public void SetShapeType()
    {
        shapeType = config.BaseType;
    }

    [ContextMenu("Regenerate")]
    public void Generate()
    {
        xSize = config.xSize;
        ySize = config.ySize;
        shapeType = config.BaseType;   
        if (useSeed)
            Random.InitState(randomSeed);

        CleanupPrevious();
        CreateBox();
        CreateSpots();
    }


     public void GenerateRuntime()
    {
        xSize = config.xSize;
        ySize = config.ySize;
        
        if (useSeed)
            Random.InitState(randomSeed);
        SetShapeType();
        CleanupPrevious();
        CreateBox();
        CreateSpots();
    }

    private void CleanupPrevious()
    {
        if (boxObject) DestroyImmediate(boxObject);
        if (spotsParent) DestroyImmediate(spotsParent);
        spotPositions.Clear();
    }

private void CreateBox()
{
    GameObject primitive;
    
    cylinderRadius = Mathf.Min(xSize, ySize) * 0.5f;

    if (shapeType == ShapeType.Cylinder)
    {
        primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        primitive.name = "Generated_Cylinder";
        primitive.transform.SetParent(transform, false);
        
        // Cylinder: diameter = 2*radius, height = zSize
        float diameter = cylinderRadius * 2f;
        primitive.transform.localScale = new Vector3(diameter, 0.3f, diameter);
        
        float halfHeight = zSize * 0.5f;
        primitive.transform.localPosition = boxOffset + new Vector3(0, halfHeight, 0);
    }
    else // Box
    {
        primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        primitive.name = "Generated_Box";
        primitive.transform.SetParent(transform, false);
        primitive.transform.localScale = new Vector3(xSize, zSize, ySize);
        
        float halfHeight = zSize * 0.5f;
        primitive.transform.localPosition = boxOffset + new Vector3(0, halfHeight, 0);
    }

    boxObject = primitive;

    // Remove collider
    var boxCol = boxObject.GetComponent<Collider>();
    if (boxCol) DestroyImmediate(boxCol);

    // Apply material and color
    var renderer = boxObject.GetComponent<Renderer>();
    if (renderer)
    {
        if (boxMaterial != null)
        {
            renderer.sharedMaterial = boxMaterial;
        }
        else if (transparentBox)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = boxColor;
            renderer.sharedMaterial = mat;
        }
    }
}

    private void CreateSpots()
    {
        float area;
        
        if (shapeType == ShapeType.Cylinder)
        {
            // Area of circle
            area = Mathf.PI * cylinderRadius * cylinderRadius;
        }
        else
        {
            // Area of rectangle
            area = xSize * ySize;
        }
        
        int targetCount = Mathf.RoundToInt(baseSpotsPerSquareMeter * area);

        spotsParent = new GameObject("Spots_Bottom");
        spotsParent.transform.SetParent(transform, false);
        spotsParent.transform.localPosition = Vector3.zero;

        if (targetCount <= 0) return;

        switch (pattern)
        {
            case DistributionPattern.Grid:
                GenerateGridPattern(targetCount);
                break;
            case DistributionPattern.HexagonalPacked:
                GenerateHexPattern(targetCount);
                break;
            case DistributionPattern.PoissonDisc:
                GeneratePoissonDisc(targetCount);
                break;
            case DistributionPattern.Random:
                GenerateRandomPattern(targetCount);
                break;
        }

        string shapeName = shapeType == ShapeType.Cylinder ? "Cylinder" : "Box";
        Debug.Log($"[BoxSpotGenerator] {shapeName} {xSize:F2}×{ySize:F2}×1m — {spotPositions.Count} spots using {pattern} pattern");
    }


    private void GenerateGridPattern(int targetCount)
    {
        int cols = Mathf.CeilToInt(Mathf.Sqrt(targetCount));
        int rows = Mathf.CeilToInt((float)targetCount / cols);

        float minX = -xSize * 0.5f + paddingX;
        float maxX = xSize * 0.5f - paddingX;
        float minZ = -ySize * 0.5f + paddingY;
        float maxZ = ySize * 0.5f - paddingY;

        float denomC = Mathf.Max(1, cols - 1);
        float denomR = Mathf.Max(1, rows - 1);

        int index = 0;
        for (int r = 0; r < rows && index < targetCount; r++)
        {
            float tR = (rows == 1) ? 0.5f : r / denomR;
            float zPlane = Mathf.Lerp(minZ, maxZ, tR);

            for (int c = 0; c < cols && index < targetCount; c++)
            {
                float tC = (cols == 1) ? 0.5f : c / denomC;
                float xPlane = Mathf.Lerp(minX, maxX, tC);

                Vector3 pos = new Vector3(xPlane, spotHeightFromBottom, zPlane);
                pos = ApplyJitter(pos);
                
                if (!avoidOverlap || !HasNearbySpot(pos))
                {
                    CreateSpotAtPosition(pos, index);
                    index++;
                }
            }
        }
    }

    private void GenerateHexPattern(int targetCount)
    {
        float minX = -xSize * 0.5f + paddingX;
        float maxX = xSize * 0.5f - paddingX;
        float minZ = -ySize * 0.5f + paddingY;
        float maxZ = ySize * 0.5f - paddingY;

        float areaWidth = maxX - minX;
        float areaHeight = maxZ - minZ;
        
        float spotSpacing = Mathf.Sqrt((areaWidth * areaHeight) / targetCount);
        float hexHeight = spotSpacing * Mathf.Sqrt(3) / 2;

        int rows = Mathf.CeilToInt(areaHeight / hexHeight);
        int colsPerRow = Mathf.CeilToInt(areaWidth / spotSpacing);

        int index = 0;
        for (int r = 0; r < rows && index < targetCount; r++)
        {
            float z = minZ + r * hexHeight;
            float xOffset = (r % 2 == 1) ? spotSpacing * 0.5f : 0f;

            for (int c = 0; c < colsPerRow && index < targetCount; c++)
            {
                float x = minX + c * spotSpacing + xOffset;
                
                if (x >= minX && x <= maxX && z >= minZ && z <= maxZ)
                {
                    Vector3 pos = new Vector3(x, spotHeightFromBottom, z);
                    pos = ApplyJitter(pos);
                    CreateSpotAtPosition(pos, index);
                    index++;
                }
            }
        }
    }

    private void GeneratePoissonDisc(int targetCount)
    {
        float minX = -xSize * 0.5f + paddingX;
        float maxX = xSize * 0.5f - paddingX;
        float minZ = -ySize * 0.5f + paddingY;
        float maxZ = ySize * 0.5f - paddingY;

        float area = (maxX - minX) * (maxZ - minZ);
        float radius = Mathf.Sqrt(area / targetCount) * 0.7f;

        List<Vector2> points = GeneratePoissonDiscSampling(minX, maxX, minZ, maxZ, radius, targetCount);

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 pos = new Vector3(points[i].x, spotHeightFromBottom, points[i].y);
            CreateSpotAtPosition(pos, i);
        }
    }

    private void GenerateRandomPattern(int targetCount)
    {
        float minX = -xSize * 0.5f + paddingX;
        float maxX = xSize * 0.5f - paddingX;
        float minZ = -ySize * 0.5f + paddingY;
        float maxZ = ySize * 0.5f - paddingY;

        int attempts = 0;
        int maxAttempts = targetCount * 10;
        
        for (int i = 0; i < targetCount && attempts < maxAttempts; attempts++)
        {
            float x = Random.Range(minX, maxX);
            float z = Random.Range(minZ, maxZ);
            Vector3 pos = new Vector3(x, spotHeightFromBottom, z);

            if (!avoidOverlap || !HasNearbySpot(pos))
            {
                CreateSpotAtPosition(pos, i);
                i++;
            }
        }
    }

    private Vector3 ApplyJitter(Vector3 pos)
    {
        if (jitter > 0f)
        {
            pos.x += Random.Range(-jitter, jitter);
            pos.z += Random.Range(-jitter, jitter);
        }
        return pos;
    }

    private bool HasNearbySpot(Vector3 pos)
    {
        foreach (var spot in spotPositions)
        {
            if (Vector3.Distance(new Vector3(pos.x, 0, pos.z), new Vector3(spot.x, 0, spot.z)) < minSpotDistance)
                return true;
        }
        return false;
    }

    private bool IsInsideCylinder(Vector3 pos)
    {
        if (shapeType != ShapeType.Cylinder) return true;
        
        float effectiveRadius = cylinderRadius - paddingX; // Use paddingX for radial padding
        float distanceFromCenter = Mathf.Sqrt(pos.x * pos.x + pos.z * pos.z);
        return distanceFromCenter <= effectiveRadius;
    }
    private void CreateSpotAtPosition(Vector3 localPos, int index)
    {
        // Check if position is valid for cylinder
        if (!IsInsideCylinder(localPos))
            return;
        
        GameObject spotGO = CreateSpot();
        spotGO.name = $"Spot_{index + 1}";
        spotGO.transform.SetParent(spotsParent.transform, false);

        Vector3 finalPos = localPos + boxOffset;
        spotGO.transform.localPosition = finalPos;

        float scale = spotScale;
        if (scaleVariation > 0f)
            scale *= Random.Range(1f - scaleVariation, 1f + scaleVariation);
        
        spotGO.transform.localScale = Vector3.one * scale;

        if (randomRotation)
            spotGO.transform.localRotation = Random.rotation;

        var col = spotGO.GetComponent<Collider>();
        if (col) DestroyImmediate(col);

        spotPositions.Add(finalPos);
    }

    private GameObject CreateSpot()
    {
        if (spotPrefab != null)
            return Instantiate(spotPrefab);

        return GameObject.CreatePrimitive(PrimitiveType.Sphere);
    }

    private List<Vector2> GeneratePoissonDiscSampling(float minX, float maxX, float minZ, float maxZ, float radius, int targetCount)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> activeList = new List<Vector2>();

        float cellSize = radius / Mathf.Sqrt(2);
        int gridWidth = Mathf.CeilToInt((maxX - minX) / cellSize);
        int gridHeight = Mathf.CeilToInt((maxZ - minZ) / cellSize);
        int[,] grid = new int[gridWidth, gridHeight];

        for (int i = 0; i < gridWidth; i++)
            for (int j = 0; j < gridHeight; j++)
                grid[i, j] = -1;

        Vector2 firstPoint = new Vector2(Random.Range(minX, maxX), Random.Range(minZ, maxZ));
        points.Add(firstPoint);
        activeList.Add(firstPoint);

        int gx = (int)((firstPoint.x - minX) / cellSize);
        int gy = (int)((firstPoint.y - minZ) / cellSize);
        grid[gx, gy] = 0;

        int maxIterations = targetCount * 30;
        int iterations = 0;

        while (activeList.Count > 0 && points.Count < targetCount && iterations < maxIterations)
        {
            iterations++;
            int idx = Random.Range(0, activeList.Count);
            Vector2 point = activeList[idx];
            bool found = false;

            for (int k = 0; k < 30; k++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2);
                float distance = Random.Range(radius, 2 * radius);
                Vector2 newPoint = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                if (newPoint.x >= minX && newPoint.x < maxX && newPoint.y >= minZ && newPoint.y < maxZ)
                {
                    int newGx = (int)((newPoint.x - minX) / cellSize);
                    int newGy = (int)((newPoint.y - minZ) / cellSize);

                    bool valid = true;
                    for (int i = Mathf.Max(0, newGx - 2); i < Mathf.Min(gridWidth, newGx + 3); i++)
                    {
                        for (int j = Mathf.Max(0, newGy - 2); j < Mathf.Min(gridHeight, newGy + 3); j++)
                        {
                            if (grid[i, j] != -1)
                            {
                                if (Vector2.Distance(newPoint, points[grid[i, j]]) < radius)
                                {
                                    valid = false;
                                    break;
                                }
                            }
                        }
                        if (!valid) break;
                    }

                    if (valid)
                    {
                        points.Add(newPoint);
                        activeList.Add(newPoint);
                        grid[newGx, newGy] = points.Count - 1;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
                activeList.RemoveAt(idx);
        }

        return points;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Vector3 center = transform.position + boxOffset + Vector3.up * zSize * 0.5f;
        
        if (shapeType == ShapeType.Cylinder)
        {
            Gizmos.color = Color.yellow;
            DrawWireCylinder(center, cylinderRadius, zSize);
            
            Gizmos.color = Color.green;
            float innerRadius = cylinderRadius - paddingX;
            DrawWireCircle(transform.position + boxOffset + Vector3.up * spotHeightFromBottom, innerRadius);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(xSize, zSize, ySize));

            Gizmos.color = Color.green;
            float minX = -xSize * 0.5f + paddingX;
            float maxX = xSize * 0.5f - paddingX;
            float minZ = -ySize * 0.5f + paddingY;
            float maxZ = ySize * 0.5f - paddingY;
            
            Vector3 paddedCenter = transform.position + boxOffset + Vector3.up * (spotHeightFromBottom + boxOffset.y);
            Gizmos.DrawWireCube(paddedCenter, new Vector3(maxX - minX, 0.01f, maxZ - minZ));
        }
    }

private void DrawWireCylinder(Vector3 center, float radius, float height)
{
    DrawWireCircle(center + Vector3.up * height * 0.5f, radius);
    DrawWireCircle(center - Vector3.up * height * 0.5f, radius);
    
    // Draw vertical lines
    for (int i = 0; i < 8; i++)
    {
        float angle = i * Mathf.PI * 2f / 8f;
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        Gizmos.DrawLine(center + offset + Vector3.up * height * 0.5f, 
                       center + offset - Vector3.up * height * 0.5f);
    }
}

private void DrawWireCircle(Vector3 center, float radius, int segments = 32)
{
    float angleStep = 360f / segments;
    Vector3 prevPoint = center + new Vector3(radius, 0, 0);
    
    for (int i = 1; i <= segments; i++)
    {
        float angle = i * angleStep * Mathf.Deg2Rad;
        Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        Gizmos.DrawLine(prevPoint, newPoint);
        prevPoint = newPoint;
    }
}
    [ContextMenu("Clear All")]
    public void ClearAll()
    {
        CleanupPrevious();
    }

    private void OnApplicationQuit()
    {
        ClearAll();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(BoxSpotsGenerator))]
public class BoxSpotsGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BoxSpotsGenerator generator = (BoxSpotsGenerator)target;

        EditorGUILayout.Space();
        float area = generator.shapeType == ShapeType.Cylinder 
        ? Mathf.PI * generator.cylinderRadius * generator.cylinderRadius 
        : generator.xSize * generator.ySize;
        EditorGUILayout.HelpBox($"Estimated spots: {Mathf.RoundToInt(generator.baseSpotsPerSquareMeter * area)}", MessageType.Info);

        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Regenerate", GUILayout.Height(30)))
        {
            generator.Generate();
        }
        if (GUILayout.Button("Clear All", GUILayout.Height(30)))
        {
            generator.ClearAll();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox($"Estimated spots: {Mathf.RoundToInt(generator.baseSpotsPerSquareMeter * generator.xSize * generator.ySize)}", MessageType.Info);
    }
}
#endif