using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// RCC25 chandelier generator - INSTANTIATED VERSION with Runtime OBJ Export
/// Creates actual GameObject instances that can be exported to OBJ format at runtime.
/// Dimensions are in **millimeters** in the inspector, auto-converted to meters.
public class RCC2_Instantiated : MonoBehaviour
{
    [Header("Crystal Rendering (required)")]
    public Mesh crystalMesh;
    public Material crystalMaterial;

    [Tooltip("Parent space for placement (null = world).")]
    public Transform parentSpace;

    [Header("Geometry (mm)")]
    [Tooltip("Overall width (X) in mm (e.g., 1900).")]
    public float widthMM = 1900f;
    [Tooltip("Overall depth (Z) in mm (e.g., 950).")]
    public float depthMM = 950f;
    [Tooltip("Overall height (Y) drop of bowl in mm (e.g., 900).")]
    public float heightMM = 900f;

    [Header("Counts")]
    public int countSmall = 200;
    public int countMedium = 161;

    [Header("Crystal Sizes (meters)")]
    [Tooltip("Uniform scale (meters) for size S range.")]
    public Vector2 smallScaleRange = new Vector2(0.010f, 0.016f);
    [Tooltip("Uniform scale (meters) for size M range.")]
    public Vector2 mediumScaleRange = new Vector2(0.017f, 0.024f);

    [Header("Distribution Controls")]
    [Tooltip("Seed for repeatable randomness.")]
    public int seed = 25;
    [Tooltip("Make fill more dense toward center (0=uniform, 1=very central).")]
    [Range(0f, 1f)] public float gaussianCenterBias = 0.25f;
    [Tooltip("Vertical 'sag' of bowl (0=parabola only, 1=extra sag).")]
    [Range(0f, 1f)] public float extraSag = 0.0f;
    [Tooltip("Add jitter (as fraction of ellipse radii).")]
    [Range(0f, 0.2f)] public float posJitter = 0.04f;
    [Tooltip("Random tilt amount for crystals.")]
    [Range(0f, 1f)] public float rotationJitter = 0.35f;

    [Header("Ceiling & Threads")]
    [Tooltip("Local Y of the ceiling plate (meters). Crystals are below this.")]
    public float ceilingY = 0f;
    [Tooltip("Show hanging threads from ceiling to each crystal.")]
    public bool showThreads = true;

    [Tooltip("Thread mesh (use a simple cylinder mesh for best results).")]
    public Mesh threadMesh;
    public Material threadMaterial;
    [Tooltip("Thread radius/thickness (meters).")]
    public float threadRadius = 0.0006f;
    [Tooltip("Number of segments along thread length (higher = smoother curves).")]
    public int threadSegments = 2;

    [Header("Performance")]
    [Tooltip("Mobile cap on instances.")]
    public int maxInstancesMobile = 7000;
    [Tooltip("Desktop cap on instances.")]
    public int maxInstancesDesktop = 22000;
    public bool mobileMode = false;

    [Header("Additional Objects to Export")]
    [Tooltip("Add any extra GameObjects here to include in the OBJ export (ceiling plate, frame, etc.)")]
    public List<GameObject> additionalObjectsToExport = new List<GameObject>();

    [Header("Runtime Export")]
    [Tooltip("Default filename for OBJ export (without extension).")]
    public string exportFileName = "MyChandelier";
    [Tooltip("Export to persistent data path (recommended for runtime).")]
    public bool usePersistentDataPath = true;
    [Tooltip("Combine all crystals into one object in the OBJ file.")]
    public bool combineIntoSingleObject = true;
    [Tooltip("Export threads as separate objects or combine with crystals.")]
    public bool exportThreadsSeparately = false;

    [Header("Runtime")]
    public bool autoRegenerateOnChange = true;
    [SerializeField, TextArea] string debugInfo;

    // ----- internals -----
    readonly List<GameObject> _crystalObjects = new();
    readonly List<GameObject> _threadObjects = new();

    float _a, _b;     // ellipse semi-axes (meters)
    float _h;         // bowl height (meters)
    int _totalPlaced; // total instances

    void OnValidate()
    {
        widthMM = Mathf.Max(100f, widthMM);
        depthMM = Mathf.Max(100f, depthMM);
        heightMM = Mathf.Max(100f, heightMM);
        countSmall = Mathf.Max(0, countSmall);
        countMedium = Mathf.Max(0, countMedium);

        if (autoRegenerateOnChange && Application.isPlaying)
            Regenerate();
    }

    void Start() => Regenerate();

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        if (crystalMesh == null || crystalMaterial == null)
        {
            Debug.LogWarning("Assign crystalMesh & crystalMaterial");
            return;
        }

        // Clean up previous crystals and threads
        CleanupCrystals();
        CleanupThreads();

        // convert to meters
        _a = (widthMM * 0.001f) * 0.5f;  // semi-major (X)
        _b = (depthMM * 0.001f) * 0.5f;  // semi-minor (Z)
        _h = (heightMM * 0.001f);        // bowl depth (Y downward)

        // target count & caps
        int desired = countSmall + countMedium;
        int cap = mobileMode ? maxInstancesMobile : maxInstancesDesktop;
        int count = Mathf.Min(desired, cap);

        UnityEngine.Random.InitState(seed);

        Transform parent = parentSpace ? parentSpace : transform;

        // Generate crystals
        int sRemain = Mathf.Min(countSmall, count);
        int mRemain = Mathf.Min(countMedium, Mathf.Max(0, count - sRemain));

        int iMat = 0;
        while (iMat < count && (sRemain > 0 || mRemain > 0))
        {
            bool placeSmall = (sRemain > 0) && (mRemain == 0 || UnityEngine.Random.value > 0.5f);

            Vector3 localPos = SamplePointInBowl();
            float scale = placeSmall
                ? Mathf.Lerp(smallScaleRange.x, smallScaleRange.y, UnityEngine.Random.value)
                : Mathf.Lerp(mediumScaleRange.x, mediumScaleRange.y, UnityEngine.Random.value);

            Quaternion rot = RandomTilt(rotationJitter);

            // Create crystal GameObject
            GameObject crystal = new GameObject($"Crystal_{iMat}");
            crystal.transform.SetParent(parent, false);
            crystal.transform.localPosition = localPos;
            crystal.transform.localRotation = rot;
            crystal.transform.localScale = Vector3.one * scale;

            // Add mesh components
            MeshFilter mf = crystal.AddComponent<MeshFilter>();
            mf.sharedMesh = crystalMesh;

            MeshRenderer mr = crystal.AddComponent<MeshRenderer>();
            mr.sharedMaterial = crystalMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            _crystalObjects.Add(crystal);

            // Thread (now using cylinder mesh)
            if (showThreads) CreateThreadCylinder(iMat, localPos, parent);

            if (placeSmall) sRemain--; else mRemain--;
            iMat++;
        }

        _totalPlaced = iMat;

        debugInfo = $"Placed: {_totalPlaced} (S:{countSmall} M:{countMedium}, capped:{count})\n" +
                    $"Ellipse a={_a:F2}m b={_b:F2}m, Bowl h={_h:F2}m\n" +
                    $"Crystal GameObjects: {_crystalObjects.Count}\n" +
                    $"Thread GameObjects: {_threadObjects.Count}\n" +
                    $"Additional Objects: {additionalObjectsToExport.Count}";
    }

    void CleanupCrystals()
    {
        foreach (var go in _crystalObjects)
        {
            if (go != null)
            {
                DestroyImmediate(go);
            }
        }
        _crystalObjects.Clear();
    }

    void CleanupThreads()
    {
        foreach (var go in _threadObjects)
        {
            if (go != null)
            {
                DestroyImmediate(go);
            }
        }
        _threadObjects.Clear();
    }

    // ----- sampling -----

    Vector3 SamplePointInBowl()
    {
        Vector2 p = RandomPointInUnitCircle();
        float x = p.x * _a;
        float z = p.y * _b;

        if (gaussianCenterBias > 0f)
        {
            float g = Mathf.Pow(UnityEngine.Random.value, Mathf.Lerp(1f, 4f, gaussianCenterBias));
            x *= Mathf.Lerp(1f, 0.7f, g);
            z *= Mathf.Lerp(1f, 0.7f, g);
        }

        float rho = Mathf.Sqrt((x * x) / (_a * _a) + (z * z) / (_b * _b));
        rho = Mathf.Min(1f, rho);

        float y = -_h + _h * (rho * rho);

        if (extraSag > 0f)
        {
            y -= extraSag * (1f - rho) * 0.15f * _h;
        }

        if (posJitter > 0f)
        {
            x += (UnityEngine.Random.value - 0.5f) * posJitter * _a * 2f;
            z += (UnityEngine.Random.value - 0.5f) * posJitter * _b * 2f;
            y += (UnityEngine.Random.value - 0.5f) * posJitter * _h * 0.15f;
        }

        float Y = ceilingY + y;
        return new Vector3(x, Y, z);
    }

    static Vector2 RandomPointInUnitCircle()
    {
        float u = UnityEngine.Random.value;
        float v = UnityEngine.Random.value;
        float r = Mathf.Sqrt(u);
        float theta = 2f * Mathf.PI * v;
        return new Vector2(r * Mathf.Cos(theta), r * Mathf.Sin(theta));
    }

    static Quaternion RandomTilt(float jitter)
    {
        if (jitter <= 0f) return Quaternion.identity;
        return Quaternion.Euler(
            (UnityEngine.Random.value - 0.5f) * 45f * jitter,
            UnityEngine.Random.value * 360f,
            (UnityEngine.Random.value - 0.5f) * 45f * jitter
        );
    }

    // ----- threads as cylinders -----

    void CreateThreadCylinder(int index, Vector3 crystalPosLocal, Transform parent)
    {
        if (threadMesh == null)
        {
            Debug.LogWarning("Thread mesh not assigned. Assign a cylinder mesh for threads.");
            return;
        }

        Vector3 top = new Vector3(crystalPosLocal.x, ceilingY, crystalPosLocal.z);
        Vector3 bot = crystalPosLocal;
        
        Vector3 dir = bot - top;
        float length = dir.magnitude;
        
        if (length < 0.0001f) return;

        Vector3 mid = (top + bot) * 0.5f;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);

        GameObject thread = new GameObject($"Thread_{index}");
        thread.transform.SetParent(parent, false);
        thread.transform.localPosition = mid;
        thread.transform.localRotation = rotation;
        
        // Scale: radius in X/Z, length in Y
        thread.transform.localScale = new Vector3(threadRadius * 2f, length * 0.5f, threadRadius * 2f);

        MeshFilter mf = thread.AddComponent<MeshFilter>();
        mf.sharedMesh = threadMesh;

        MeshRenderer mr = thread.AddComponent<MeshRenderer>();
        mr.sharedMaterial = threadMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        _threadObjects.Add(thread);
    }

    void OnDestroy()
    {
        CleanupCrystals();
        CleanupThreads();
    }

    // ==================== OBJ EXPORT HELPERS ====================

    void ExportObjectsToOBJ(StringBuilder sb, List<GameObject> objects, string groupName, ref int vertexOffset, ref int normalOffset, ref int uvOffset)
    {
        if (objects == null || objects.Count == 0) return;

        bool firstInGroup = true;

        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Transform t = obj.transform;

            Vector3[] localVertices;
            Vector3[] localNormals;
            Vector2[] uvs;

            try
            {
                localVertices = mesh.vertices;
                localNormals = mesh.normals;
                uvs = mesh.uv;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Cannot read mesh '{mesh.name}'. Enable Read/Write in mesh import settings.\nError: {e.Message}");
                continue;
            }

            // For combined export, add group header only once
            if (firstInGroup && !string.IsNullOrEmpty(groupName))
            {
                sb.AppendLine($"g {groupName}");
                firstInGroup = false;
            }

            // Vertices in world space
            for (int i = 0; i < localVertices.Length; i++)
            {
                Vector3 v = t.TransformPoint(localVertices[i]);
                sb.AppendLine($"v {-v.x:F6} {v.y:F6} {v.z:F6}");
            }

            // Normals in world space
            if (localNormals != null && localNormals.Length > 0)
            {
                for (int i = 0; i < localNormals.Length; i++)
                {
                    Vector3 n = t.TransformDirection(localNormals[i]);
                    sb.AppendLine($"vn {-n.x:F6} {n.y:F6} {n.z:F6}");
                }
            }

            // UVs
            if (uvs != null && uvs.Length > 0)
            {
                foreach (Vector2 uv in uvs)
                {
                    sb.AppendLine($"vt {uv.x:F6} {uv.y:F6}");
                }
            }
        }

        // Second pass: write faces
        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;

            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;

            Vector3[] localVertices;
            Vector3[] localNormals;
            Vector2[] uvs;
            int[] triangles;

            try
            {
                localVertices = mesh.vertices;
                localNormals = mesh.normals;
                uvs = mesh.uv;
                triangles = mesh.triangles;
            }
            catch
            {
                continue;
            }

            bool hasNormals = localNormals != null && localNormals.Length > 0;
            bool hasUVs = uvs != null && uvs.Length > 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i1 = triangles[i] + 1 + vertexOffset;
                int i2 = triangles[i + 1] + 1 + vertexOffset;
                int i3 = triangles[i + 2] + 1 + vertexOffset;

                if (hasUVs && hasNormals)
                {
                    int uv1 = triangles[i] + 1 + uvOffset;
                    int uv2 = triangles[i + 1] + 1 + uvOffset;
                    int uv3 = triangles[i + 2] + 1 + uvOffset;
                    int n1 = triangles[i] + 1 + normalOffset;
                    int n2 = triangles[i + 1] + 1 + normalOffset;
                    int n3 = triangles[i + 2] + 1 + normalOffset;
                    sb.AppendLine($"f {i1}/{uv1}/{n1} {i2}/{uv2}/{n2} {i3}/{uv3}/{n3}");
                }
                else if (hasNormals)
                {
                    int n1 = triangles[i] + 1 + normalOffset;
                    int n2 = triangles[i + 1] + 1 + normalOffset;
                    int n3 = triangles[i + 2] + 1 + normalOffset;
                    sb.AppendLine($"f {i1}//{n1} {i2}//{n2} {i3}//{n3}");
                }
                else if (hasUVs)
                {
                    int uv1 = triangles[i] + 1 + uvOffset;
                    int uv2 = triangles[i + 1] + 1 + uvOffset;
                    int uv3 = triangles[i + 2] + 1 + uvOffset;
                    sb.AppendLine($"f {i1}/{uv1} {i2}/{uv2} {i3}/{uv3}");
                }
                else
                {
                    sb.AppendLine($"f {i1} {i2} {i3}");
                }
            }

            vertexOffset += localVertices.Length;
            normalOffset += hasNormals ? localNormals.Length : 0;
            uvOffset += hasUVs ? uvs.Length : 0;
        }
    }

    // ==================== RUNTIME OBJ EXPORT ====================

    public string ExportToOBJ()
    {
        return ExportToOBJ(exportFileName);
    }

    public string ExportToOBJ(string filename)
    {
        if (_crystalObjects.Count == 0)
        {
            Debug.LogWarning("No chandelier to export. Generate first by calling Regenerate().");
            return null;
        }

        try
        {
            string directory = usePersistentDataPath
                ? Application.persistentDataPath
                : Application.dataPath;

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string safeFilename = SanitizeFileName(filename);
            string fullPath = Path.Combine(directory, $"{safeFilename}_{timestamp}.obj");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Chandelier exported from Unity");
            sb.AppendLine($"# Generated: {System.DateTime.Now}");
            sb.AppendLine($"# Total crystals: {_totalPlaced}");
            sb.AppendLine($"# Total threads: {_threadObjects.Count}");
            sb.AppendLine($"# Additional objects: {additionalObjectsToExport.Count}");
            sb.AppendLine();

            int vertexOffset = 0;
            int normalOffset = 0;
            int uvOffset = 0;

            if (combineIntoSingleObject)
            {
                // Export as single combined chandelier
                sb.AppendLine("o Chandelier");
                
                // Combine crystals, threads, and additional objects
                List<GameObject> allObjects = new List<GameObject>();
                allObjects.AddRange(_crystalObjects);
                if (!exportThreadsSeparately)
                    allObjects.AddRange(_threadObjects);
                allObjects.AddRange(additionalObjectsToExport);

                ExportObjectsToOBJ(sb, allObjects, "", ref vertexOffset, ref normalOffset, ref uvOffset);

                // Export threads separately if requested
                if (exportThreadsSeparately && _threadObjects.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("o Threads");
                    ExportObjectsToOBJ(sb, _threadObjects, "", ref vertexOffset, ref normalOffset, ref uvOffset);
                }
            }
            else
            {
                // Export crystals as group
                if (_crystalObjects.Count > 0)
                {
                    sb.AppendLine("# Crystals");
                    ExportObjectsToOBJ(sb, _crystalObjects, "Crystals", ref vertexOffset, ref normalOffset, ref uvOffset);
                    sb.AppendLine();
                }

                // Export threads as group
                if (_threadObjects.Count > 0)
                {
                    sb.AppendLine("# Threads");
                    ExportObjectsToOBJ(sb, _threadObjects, "Threads", ref vertexOffset, ref normalOffset, ref uvOffset);
                    sb.AppendLine();
                }

                // Export additional objects
                if (additionalObjectsToExport.Count > 0)
                {
                    sb.AppendLine("# Additional Objects");
                    ExportObjectsToOBJ(sb, additionalObjectsToExport, "AdditionalObjects", ref vertexOffset, ref normalOffset, ref uvOffset);
                }
            }

            // Write to file
            File.WriteAllText(fullPath, sb.ToString());

            Debug.Log($"✓ Chandelier exported successfully!\nPath: {fullPath}\nSize: {new FileInfo(fullPath).Length / 1024}KB");

            return fullPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to export chandelier: {ex.Message}");
            return null;
        }
    }

    public string GetExportDirectory()
    {
        return usePersistentDataPath ? Application.persistentDataPath : Application.dataPath;
    }

    static string SanitizeFileName(string filename)
    {
        char[] invalids = Path.GetInvalidFileNameChars();
        return string.Join("_", filename.Split(invalids, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
    }

    [ContextMenu("Export to OBJ (Context Menu)")]
    void ExportToOBJContextMenu()
    {
        ExportToOBJ();
    }

    public void OnExportButtonClicked()
    {
        string path = ExportToOBJ();
        
        if (path != null)
        {
            Debug.Log($"Saved to: {path}");
        }
    }
}