using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ShaperCreator : MonoBehaviour
{
    [Header("Lemniscate (Cassini)")]
    [Tooltip("Half-distance between foci (±d, 0)")]
    public float d = 2f;
    [Tooltip("Cassini constant; use c≈d for a lemniscate (∞) waist")]
    public float c = 2f;

    [Header("Height Profile")]
    [Tooltip("Max height at center")]
    public float H = 3f;
    [Tooltip("Shape exponent: bigger = flatter top, steeper sides")]
    public float q = 2f;

    [Header("Sampling")]
    [Tooltip("How many points to generate")]
    public int pointCount = 3000;
    [Tooltip("Safety cap for rejection sampling loops")]
    public int maxAngleSamples = 2048;
    public int seed = 1234;
    public float jitterXY = 0f;
    public float jitterZ  = 0f;

    [Header("Prefab Scale Control")]
    [Tooltip("Desired world size of the spawned prefab (1=1m).")]
    public float prefabWorldScale = 0.03f; // ~3 cm default

    [Tooltip("Apply world scale regardless of parent scaling.")]
    public bool forceWorldScale = true;

    [Header("Gizmos (Editor preview)")]
    public bool drawGizmos = true;
    public float gizmoSize = 0.02f;
    public Color gizmoColor = new Color(0.7f, 0.85f, 1f, 1f);

    [Header("Prefab Spawning (optional)")]
    [Tooltip("If enabled, instantiate one prefab per point")]
    public bool instantiatePrefabs = false;
    public GameObject pointPrefab;
    [Tooltip("Parent for spawned instances (auto-created if null)")]
    public Transform pointsParent;

    [Header("Housekeeping")]
    [Tooltip("Auto-clear and rebuild on parameter change")]
    public bool autoRebuildOnValidate = true;

    // Generated points (local space)
    public List<Vector3> points = new List<Vector3>();

    System.Random rng;

    void OnValidate()
    {
        d = Mathf.Max(1e-4f, d);
        c = Mathf.Max(1e-4f, c);
        H = Mathf.Max(0f, H);
        q = Mathf.Max(0.5f, q);
        pointCount = Mathf.Max(1, pointCount);
        gizmoSize = Mathf.Max(0.001f, gizmoSize);

        if (autoRebuildOnValidate)
            Rebuild();
    }

    void Reset() => Rebuild();

    [ContextMenu("Rebuild (Clear + Generate + Spawn)")]
    public void Rebuild()
    {
        ClearSpawned();
        GeneratePoints();
        if (instantiatePrefabs)
            SpawnPrefabs();
    }

    [ContextMenu("Generate Points Only")]
    public void GeneratePoints()
    {
        rng = new System.Random(seed);
        points.Clear();

        int guard = 0;
        // Rejection sampling inside Cassini region F <= c^2 (via r_max(θ))
        while (points.Count < pointCount && guard++ < pointCount * 20)
        {
            float theta = (float)(rng.NextDouble() * 2.0 * Mathf.PI);

            // Solve for r_max along this theta using Cassini polar form
            // Let u = r^2: u^2 + (2d^2 - 4d^2 cos^2θ)u + (d^4 - c^4) = 0
            float cos = Mathf.Cos(theta);
            float d2  = d * d;
            float c4  = c * c * c * c;

            float A = 1f;
            float B = 2f * d2 - 4f * d2 * cos * cos;
            float C = d2 * d2 - c4;

            float disc = B * B - 4f * A * C;
            if (disc <= 0f) continue;

            float sqrtDisc = Mathf.Sqrt(disc);
            float u1 = (-B + sqrtDisc) * 0.5f;
            float u2 = (-B - sqrtDisc) * 0.5f;

            float rmax = 0f;
            if (u1 > 0f) rmax = Mathf.Sqrt(u1);
            if (u2 > 0f) rmax = Mathf.Max(rmax, Mathf.Sqrt(u2));
            if (rmax <= 0f) continue;

            // Sample radius [0, rmax]; sqrt for near-uniform area density
            float t = (float)rng.NextDouble();
            float r = rmax * Mathf.Sqrt(t);

            // Height: max at center, zero at boundary
            float z = H * (1f - Mathf.Pow(Mathf.Clamp01(r / rmax), q));

            // Convert to XZ plane (Y is up)
            float x = r * Mathf.Cos(theta);
            float zPlanar = r * Mathf.Sin(theta);

            // Optional jitter
            x       += ((float)rng.NextDouble() * 2f - 1f) * jitterXY;
            zPlanar += ((float)rng.NextDouble() * 2f - 1f) * jitterXY;
            z       += ((float)rng.NextDouble() * 2f - 1f) * jitterZ;

            points.Add(new Vector3(x, z, zPlanar));
        }

#if UNITY_EDITOR
        // Helpful warning if you end up with too few points (bad parameters)
        if (points.Count < pointCount * 0.5f)
        {
            Debug.LogWarning($"[ShaperCreator] Generated only {points.Count} / {pointCount} points. " +
                             $"Try setting c ≈ d (e.g., d={d:F2}, c={d:F2}) or reduce q.");
        }
#endif
    }

    Vector3 LocalScaleForWorld(float worldScale, Transform parent)
    {
        if (parent == null) return Vector3.one * worldScale;
        Vector3 lossy = parent.lossyScale;
        // Avoid divide-by-zero
        float sx = Mathf.Approximately(lossy.x, 0f) ? 1f : lossy.x;
        float sy = Mathf.Approximately(lossy.y, 0f) ? 1f : lossy.y;
        float sz = Mathf.Approximately(lossy.z, 0f) ? 1f : lossy.z;
        return new Vector3(worldScale / sx, worldScale / sy, worldScale / sz);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || points == null) return;
        Gizmos.color = gizmoColor;
        var M = transform.localToWorldMatrix;
        foreach (var p in points)
            Gizmos.DrawSphere(M.MultiplyPoint3x4(p), gizmoSize);
    }

    // -------- Prefab spawning --------

    void EnsureParent()
    {
        if (pointsParent != null) return;

        var existing = transform.Find("Points");
        if (existing != null)
        {
            pointsParent = existing;
            return;
        }

        var go = new GameObject("Points");
        go.transform.SetParent(transform, false);
        pointsParent = go.transform;

#if UNITY_EDITOR
        // Hide in hierarchy if you like:
        // go.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
#endif
    }

    [ContextMenu("Spawn Prefabs (points -> instances)")]
    public void SpawnPrefabs()
    {
        if (pointPrefab == null)
        {
            Debug.LogError("[ShaperCreator] pointPrefab is null. Assign a prefab first.");
            return;
        }

        EnsureParent();

#if UNITY_EDITOR
        bool inEditMode = !Application.isPlaying;
#endif

        var localToWorld = transform.localToWorldMatrix;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 localPos = points[i];
            Vector3 worldPos = localToWorld.MultiplyPoint3x4(localPos);

#if UNITY_EDITOR
            if (inEditMode)
            {
                var obj = PrefabUtility.InstantiatePrefab(pointPrefab, pointsParent) as GameObject;
                if (obj != null)
                {
                    obj.transform.position = worldPos;
                    obj.transform.rotation = transform.rotation;

                    if (forceWorldScale)
                        obj.transform.localScale = LocalScaleForWorld(prefabWorldScale, pointsParent);
                    else
                        obj.transform.localScale = Vector3.one * prefabWorldScale;
                }
            }
            else
#endif
            {
                var obj = Instantiate(pointPrefab, pointsParent);
                obj.transform.localPosition = localPos;
                obj.transform.localRotation = Quaternion.identity;

                if (forceWorldScale)
                    obj.transform.localScale = LocalScaleForWorld(prefabWorldScale, pointsParent);
                else
                    obj.transform.localScale = Vector3.one * prefabWorldScale;
            }
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorUtility.SetDirty(pointsParent.gameObject);
#endif
    }

    [ContextMenu("Clear Spawned Prefabs")]
    public void ClearSpawned()
    {
        if (pointsParent == null)
        {
            var existing = transform.Find("Points");
            if (existing != null) pointsParent = existing;
        }
        if (pointsParent == null) return;

        var toDestroy = new List<GameObject>();
        foreach (Transform child in pointsParent)
            toDestroy.Add(child.gameObject);

#if UNITY_EDITOR
        bool inEditMode = !Application.isPlaying;
        foreach (var go in toDestroy)
        {
            if (inEditMode) DestroyImmediate(go);
            else Destroy(go);
        }
#else
        foreach (var go in toDestroy) Destroy(go);
#endif
    }
}