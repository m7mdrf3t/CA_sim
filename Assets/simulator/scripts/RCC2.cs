using System;
using System.Collections.Generic;
using UnityEngine;

/// RCC25 chandelier generator (ellipse top view + bowl side profile).
/// WebGL-safe: uses Graphics.DrawMeshInstanced (1023 batch size).
/// Dimensions are in **millimeters** in the inspector, auto-converted to meters.
public class RCC2 : MonoBehaviour
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
    [Tooltip("Vertical ‘sag’ of bowl (0=parabola only, 1=extra sag).")]
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

    [Tooltip("Option A: use a thin cylinder prefab for each thread (heavier).")]
    public GameObject threadPrefab;
    [Tooltip("Option B: use LineRenderer threads (lighter).")]
    public bool useLineRendererThreads = true;
    public Material threadMaterial;
    [Tooltip("Thread thickness for LineRenderer (meters).")]
    public float threadThickness = 0.0012f;

    [Header("Performance")]
    [Tooltip("Mobile cap on instances.")]
    public int maxInstancesMobile = 7000;
    [Tooltip("Desktop cap on instances.")]
    public int maxInstancesDesktop = 22000;
    public bool mobileMode = false;

    [Header("Runtime")]
    public bool autoRegenerateOnChange = true;
    [SerializeField, TextArea] string debugInfo;

    // ----- internals -----
    const int kBatch = 1023;
    readonly List<Matrix4x4[]> _batches = new();
    MaterialPropertyBlock _mpb;
    readonly System.Random _sysRand = new System.Random();
    Matrix4x4[] _matPool;
    readonly List<GameObject> _threadPool = new();
    readonly List<LineRenderer> _linePool = new();

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

    void Update()
    {
        if (_batches.Count == 0) return;

        for (int i = 0; i < _batches.Count; i++)
        {
            var chunk = _batches[i];
            Graphics.DrawMeshInstanced(crystalMesh, 0, crystalMaterial, chunk, chunk.Length, _mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        if (crystalMesh == null || crystalMaterial == null)
        {
            Debug.LogWarning("Assign crystalMesh & crystalMaterial");
            return;
        }

        // convert to meters
        _a = (widthMM * 0.001f) * 0.5f;  // semi-major (X)
        _b = (depthMM * 0.001f) * 0.5f;  // semi-minor (Z)
        _h = (heightMM * 0.001f);        // bowl depth (Y downward)

        // target count & caps
        int desired = countSmall + countMedium;
        int cap = mobileMode ? maxInstancesMobile : maxInstancesDesktop;
        int count = Mathf.Min(desired, cap);

        EnsureMatPoolSize(count);
        _batches.Clear();
        _mpb ??= new MaterialPropertyBlock();
        UnityEngine.Random.InitState(seed);
        _sysRand.InitializeSeed(seed);

        // generate positions for S and M
        int iMat = 0;
        int sRemain = Mathf.Min(countSmall, count);
        int mRemain = Mathf.Min(countMedium, Mathf.Max(0, count - sRemain));

        // We’ll interleave S and M so sizes are visually distributed.
        while (iMat < count && (sRemain > 0 || mRemain > 0))
        {
            bool placeSmall = (sRemain > 0) && (mRemain == 0 || UnityEngine.Random.value > 0.5f);

            Vector3 localPos = SamplePointInBowl();
            float scale = placeSmall
                ? Mathf.Lerp(smallScaleRange.x, smallScaleRange.y, UnityEngine.Random.value)
                : Mathf.Lerp(mediumScaleRange.x, mediumScaleRange.y, UnityEngine.Random.value);

            Quaternion rot = RandomTilt(rotationJitter);

            Matrix4x4 trsLocal = Matrix4x4.TRS(localPos, rot, Vector3.one * scale);
            Matrix4x4 parent = parentSpace ? parentSpace.localToWorldMatrix : Matrix4x4.identity;
            _matPool[iMat++] = parent * trsLocal;

            // Thread
            if (showThreads) EnsureThread(iMat - 1, localPos);

            if (placeSmall) sRemain--; else mRemain--;
        }

        _totalPlaced = iMat;
        // chunk matrices into instancing batches
        for (int i = 0; i < _totalPlaced; i += kBatch)
        {
            int len = Mathf.Min(kBatch, _totalPlaced - i);
            var chunk = new Matrix4x4[len];
            Array.Copy(_matPool, i, chunk, 0, len);
            _batches.Add(chunk);
        }

        // cleanup extra thread pool objects
        TrimThreadPools(_totalPlaced);

        debugInfo = $"Placed: {_totalPlaced} (S:{countSmall} M:{countMedium}, capped:{count})\n" +
                    $"Ellipse a={_a:F2}m b={_b:F2}m, Bowl h={_h:F2}m";
    }

    // ----- sampling -----

    // Returns a local-space point in meters, with Y measured downwards from ceilingY.
    Vector3 SamplePointInBowl()
    {
        // 1) Uniform random point inside ellipse (top view), then apply center bias
        Vector2 p = RandomPointInUnitCircle(); // uniform in unit circle
        // Map unit circle -> ellipse with semi-axes a,b
        float x = p.x * _a;
        float z = p.y * _b;

        // Optional central bias (pull inward a bit)
        if (gaussianCenterBias > 0f)
        {
            float g = Mathf.Pow(UnityEngine.Random.value, Mathf.Lerp(1f, 4f, gaussianCenterBias));
            x *= Mathf.Lerp(1f, 0.7f, g);
            z *= Mathf.Lerp(1f, 0.7f, g);
        }

        // 2) Bowl profile (paraboloid that touches y=0 at rim and y=-h at center)
        // normalized radial distance in ellipse
        float rho = Mathf.Sqrt((x * x) / (_a * _a) + (z * z) / (_b * _b)); // 0..1
        rho = Mathf.Min(1f, rho);

        // parabola: y = -h + h * rho^2   ⇒ y∈[-h,0]
        float y = -_h + _h * (rho * rho);

        // extra sag (slightly deeper)
        if (extraSag > 0f)
        {
            y -= extraSag * (1f - rho) * 0.15f * _h;
        }

        // 3) Small jitter to avoid perfect grid feel
        if (posJitter > 0f)
        {
            x += (UnityEngine.Random.value - 0.5f) * posJitter * _a * 2f;
            z += (UnityEngine.Random.value - 0.5f) * posJitter * _b * 2f;
            y += (UnityEngine.Random.value - 0.5f) * posJitter * _h * 0.15f;
        }

        // Convert to world upward reference: ceiling at ceilingY, bowl goes downwards
        float Y = ceilingY + y; // y is negative or zero
        return new Vector3(x, Y, z);
    }

    // uniform in unit circle using polar method
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

    // ----- threads -----

    void EnsureThread(int index, Vector3 crystalPosLocal)
    {
        // Thread goes from ceiling (ceilingY) straight down to crystal
        Vector3 top = new Vector3(crystalPosLocal.x, ceilingY, crystalPosLocal.z);
        Vector3 bot = crystalPosLocal;

        if (useLineRendererThreads)
        {
            var lr = GetOrCreateLR(index);
            lr.positionCount = 2;
            lr.SetPosition(0, ToWorld(top));
            lr.SetPosition(1, ToWorld(bot));
            lr.startWidth = lr.endWidth = threadThickness;
            lr.enabled = true;
        }
        else if (threadPrefab != null)
        {
            var go = GetOrCreateThreadGO(index);
            Vector3 worldTop = ToWorld(top);
            Vector3 worldBot = ToWorld(bot);
            Vector3 mid = (worldTop + worldBot) * 0.5f;
            Vector3 dir = (worldBot - worldTop);
            float len = dir.magnitude;
            if (len < 0.0001f) len = 0.0001f;

            go.transform.position = mid;
            go.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.right) * Quaternion.Euler(90,0,0);
            go.transform.localScale = new Vector3(go.transform.localScale.x, len * 0.5f, go.transform.localScale.z); // assumes prefab is 1m tall centered
            go.SetActive(true);
        }
    }

    Vector3 ToWorld(Vector3 local)
    {
        return parentSpace ? parentSpace.TransformPoint(local) : local;
    }

    LineRenderer GetOrCreateLR(int index)
    {
        for (int i = _linePool.Count; i <= index; i++)
        {
            var go = new GameObject($"ThreadLR_{i}");
            if (parentSpace) go.transform.SetParent(parentSpace, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCapVertices = 0;
            lr.numCornerVertices = 0;
            lr.alignment = LineAlignment.View; // billboard for thin look
            lr.material = threadMaterial;
            _linePool.Add(lr);
        }
        return _linePool[index];
    }

    GameObject GetOrCreateThreadGO(int index)
    {
        for (int i = _threadPool.Count; i <= index; i++)
        {
            var go = Instantiate(threadPrefab, parentSpace ? parentSpace : null);
            go.name = $"Thread_{i}";
            _threadPool.Add(go);
        }
        return _threadPool[index];
    }

    void TrimThreadPools(int keep)
    {
        for (int i = 0; i < _linePool.Count; i++)
            if (i >= keep && _linePool[i]) _linePool[i].enabled = false;

        for (int i = 0; i < _threadPool.Count; i++)
            if (i >= keep && _threadPool[i]) _threadPool[i].SetActive(false);
    }

    // ----- utils -----
    void EnsureMatPoolSize(int count)
    {
        if (_matPool == null || _matPool.Length < count)
            _matPool = new Matrix4x4[count];
    }
}

static class RandExt
{
    public static void InitializeSeed(this System.Random r, int seed)
    {
        // nothing; just semantic sugar so we can keep one System.Random if needed later
    }
}