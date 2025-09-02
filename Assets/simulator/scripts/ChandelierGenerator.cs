using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// Chandelier Creator (WebGL-friendly)
/// - Shapes: Hourglass, SCurve, Lemniscate, FlatCloud, Waterfall
/// - Bands/strata, density, gaussian bias, sag, size
/// - Crystal scale/rotation/jitter, seeds
/// - Wind sway
/// - Mobile/Desktop caps + LOD
/// - JSON preset save/load
/// Rendering: Graphics.DrawMeshInstanced (1023 per batch)
public class ChandelierCreator : MonoBehaviour
{
    // ====== Public Data Models (serialized) ======
    [Serializable] public class StructureParams
    {
        public ShapePreset preset = ShapePreset.Hourglass;
        [Tooltip("Overall size in meters (W,H,D).")]
        public Vector3 sizeMeters = new(5f, 3.2f, 1.2f);

        [Range(1, 8)] public int bands = 2;
        [Tooltip("Approx crystal count target before caps/LOD.")]
        [Range(100, 50000)] public int density = 8000;

        [Tooltip("0 = uniform, 1 = very center-biased.")]
        [Range(0f, 1f)] public float gaussianBias = 0.35f;

        [Tooltip("Sag amount (gravity feel).")]
        [Range(0f, 0.5f)] public float sag = 0.08f;

        [Tooltip("Horizontal curvature multiplier for some presets.")]
        [Range(0f, 2f)] public float curveStrength = 1.0f;

        [Tooltip("Depth spread factor (0 = flat sheet, 1 = full depth).")]
        [Range(0f, 1.5f)] public float depthSpread = 0.6f;
    }

    [Serializable] public class CrystalParams
    {
        [Tooltip("Random seed for positions/rotations.")]
        public int seed = 42;

        [Tooltip("Meters.")]
        public Vector2 scaleRange = new(0.012f, 0.028f);

        [Tooltip("Random tilt amount.")]
        [Range(0f, 1f)] public float rotationJitter = 0.35f;

        [Tooltip("Local position noise as a fraction of size.")]
        [Range(0f, 1f)] public float positionJitter = 0.15f;

        [Tooltip("If true, use per-band shape offsets (slight separation ribbons).")]
        public bool bandOffset = true;

        [Tooltip("How much to offset adjacent bands.")]
        [Range(0f, 0.5f)] public float bandOffsetAmount = 0.12f;
    }

    [Serializable] public class AnimationParams
    {
        [Tooltip("Meters of sway at peak.")]
        public float windAmplitude = 0.01f;

        [Tooltip("Seconds per oscillation.")]
        public float windPeriod = 9.0f;

        [Tooltip("Randomize per-instance phase.")]
        public bool randomizedPhase = true;
    }

    [Serializable] public class PerfParams
    {
        public bool mobileMode = false;

        [Range(100, 20000)] public int maxInstancesMobile = 7000;
        [Range(100, 50000)] public int maxInstancesDesktop = 22000;

        [Tooltip("LOD: render only every Nth instance (1 = full detail).")]
        [Range(1, 8)] public int lodStep = 1;
    }

    [Serializable] public class SuspensionParams
    {
        [Tooltip("Expose a flag for UI to toggle cable visibility elsewhere (not rendered here).")]
        public bool showThreads = true;

        [Tooltip("Ideal per-thread vertical spacing in meters (for future use).")]
        [Range(0.02f, 0.4f)] public float threadSpacing = 0.12f;
    }

    [Serializable] public class Preset
    {
        public StructureParams structure = new();
        public CrystalParams crystals = new();
        public AnimationParams animation = new();
        public PerfParams perf = new();
        public SuspensionParams suspension = new();
    }

    public enum ShapePreset { Hourglass, SCurve, Lemniscate, FlatCloud, Waterfall }

    // ====== Inspector ======
    [Header("References")]
    public Mesh crystalMesh;
    public Material crystalMaterial;
    public Transform parentSpace;

    [Header("Parameters")]
    public Preset preset = new();

    [Header("Debug/Runtime")]
    [SerializeField] bool autoRegenerateOnChange = true;
    [SerializeField] bool drawGizmosBounds = false;

    // ====== Internal ======
    const int kBatch = 1023;
    List<Matrix4x4[]> _batches = new();
    MaterialPropertyBlock _mpb;
    Vector3 _halfSize;
    int _instanceCount;

    // cache arrays to reduce GC on regenerate
    Matrix4x4[] _matPool;
    readonly List<Vector4> _tempPhases = new(); // if you extend to per-instance shader data later

    // ====== Unity ======
    void OnValidate()
    {
        ClampParams();
        if (autoRegenerateOnChange && Application.isPlaying)
            Regenerate();
    }

    void Start()
    {
        Regenerate();
    }

    void Update()
    {
        if (_batches.Count == 0) return;

        // Global wind param for optional vertex sway (see shader note)
        float t = Time.time;
        Shader.SetGlobalVector("_WindParams", new Vector4(t, preset.animation.windAmplitude, Mathf.Max(0.1f, preset.animation.windPeriod), 0));

        for (int b = 0; b < _batches.Count; b++)
        {
            var chunk = _batches[b];
            Graphics.DrawMeshInstanced(crystalMesh, 0, crystalMaterial, chunk, chunk.Length, _mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmosBounds) return;
        Gizmos.color = new Color(1, 1, 1, 0.3f);
        var c = transform.position;
        var s = preset.structure.sizeMeters;
        Gizmos.DrawWireCube(c, s);
    }

    // ====== Public API (for UI/Code) ======
    public void SetPresetJson(string json)
    {
        var p = JsonUtility.FromJson<Preset>(json);
        if (p != null)
        {
            preset = p;
            Regenerate();
        }
    }

    public string GetPresetJson() => JsonUtility.ToJson(preset);

    public void SetMobileMode(bool mobile)
    {
        preset.perf.mobileMode = mobile;
        Regenerate();
    }

    public void SetShape(ShapePreset shp)
    {
        preset.structure.preset = shp;
        Regenerate();
    }

    public void SetDensity(int d)
    {
        preset.structure.density = Mathf.Clamp(d, 100, 50000);
        Regenerate();
    }

    public void SetSize(Vector3 meters)
    {
        preset.structure.sizeMeters = new Vector3(Mathf.Max(0.1f, meters.x), Mathf.Max(0.1f, meters.y), Mathf.Max(0.05f, meters.z));
        Regenerate();
    }

    public void Regenerate() => BuildInstances();

    // ====== Core Generation ======
    void BuildInstances()
    {
        if (crystalMesh == null || crystalMaterial == null) { Debug.LogWarning("Assign crystalMesh & crystalMaterial"); return; }

        ClampParams();

        _mpb ??= new MaterialPropertyBlock();
        _batches.Clear();
        _tempPhases.Clear();

        // counts & half size
        int cap = preset.perf.mobileMode ? preset.perf.maxInstancesMobile : preset.perf.maxInstancesDesktop;
        int desired = Mathf.Min(cap, preset.structure.density);
        int step = Mathf.Max(1, preset.perf.lodStep);
        _instanceCount = Mathf.Max(0, desired / step);

        _halfSize = preset.structure.sizeMeters * 0.5f;
        EnsureMatPoolSize(_instanceCount);

        Random.InitState(preset.crystals.seed);

        // Build matrices
        int written = 0;
        for (int i = 0; i < desired; i += step)
        {
            // sample band & normalized vertical within that band
            int bandIndex = Mathf.Max(0, preset.structure.bands - 1) == 0 ? 0 : Random.Range(0, preset.structure.bands);
            float t = Random.value; // 0..1 inside band
            float bandT = (bandIndex + t) / Mathf.Max(1, preset.structure.bands);

            float ang = Random.value * Mathf.PI * 2f;

            Vector3 pos = SampleShapePosition(bandT, ang, bandIndex);
            ApplyGaussianBias(ref pos);
            ApplyJitter(ref pos);

            float s = Mathf.Lerp(preset.crystals.scaleRange.x, preset.crystals.scaleRange.y, Random.value);
            Quaternion rot = RandomRotation();

            var trs = parentSpace ? parentSpace.localToWorldMatrix : Matrix4x4.identity;
            _matPool[written] = trs * Matrix4x4.TRS(pos, rot, Vector3.one * s);

            if (preset.animation.randomizedPhase)
                _tempPhases.Add(new Vector4(Random.value * 6.28318f, Random.Range(0.6f, 1.2f), 0, 0));

            written++;
            if (written >= _instanceCount) break;
        }

        // chunk into 1023 batches
        for (int i = 0; i < _instanceCount; i += kBatch)
        {
            int len = Mathf.Min(kBatch, _instanceCount - i);
            var chunk = new Matrix4x4[len];
            Array.Copy(_matPool, i, chunk, 0, len);
            _batches.Add(chunk);
        }
    }

    Vector3 SampleShapePosition(float bandT, float ang, int bandIndex)
    {
        // y in [-0.5, 0.5] scaled by height
        float y01 = Mathf.Clamp01(bandT);
        float y = Mathf.Lerp(-_halfSize.y, _halfSize.y, y01);

        // base horizontal radii from preset
        float rx = _halfSize.x;
        float rz = _halfSize.z * Mathf.Clamp(preset.structure.depthSpread, 0f, 1.5f);

        // curve factor across height [-1..1]
        float yh = Mathf.Lerp(-1f, 1f, y01);
        float c = preset.structure.curveStrength;

        float ox = 0f, oz = 0f;
        switch (preset.structure.preset)
        {
            case ShapePreset.Hourglass:
            {
                // Gaussian lobe: tight at center (y=0), wider near ends
                float lobe = Mathf.Exp(-Mathf.Pow(yh * 2.9f, 2f));
                float radius = Mathf.Lerp(0.12f, 1f, lobe);
                ox = rx * radius * Mathf.Cos(ang);
                oz = rz * radius * Mathf.Sin(ang) * 0.6f;
                // sag (more at lower half)
                float sagTerm = Mathf.Clamp01(1f - y01) * preset.structure.sag;
                y -= sagTerm * _halfSize.y * 0.3f;
                break;
            }
            case ShapePreset.SCurve:
            {
                // S-shaped horizontal bias with y
                float sCurve = ((float)Math.Tanh(yh * 1.4f)) * c; // -c..+c
                float radius = Mathf.Lerp(0.35f, 0.9f, 0.5f + 0.5f * Mathf.Cos(ang + sCurve));
                ox = rx * radius * Mathf.Cos(ang) + rx * 0.25f * sCurve;
                oz = rz * radius * Mathf.Sin(ang);
                y -= preset.structure.sag * _halfSize.y * Mathf.Clamp01(1f - y01);
                break;
            }
            case ShapePreset.Lemniscate:
            {
                // sideways ∞ in XZ, shrink near center in Y
                float a = c * 0.6f + 0.8f;
                float denom = 1f + Mathf.Sin(ang) * Mathf.Sin(ang);
                float rad = a / Mathf.Max(0.2f, denom); // ∞ curve
                float yShrink = Mathf.Lerp(0.6f, 1f, Mathf.Abs(yh)); // thinner in middle
                ox = rx * rad * Mathf.Cos(ang) * 0.4f * yShrink;
                oz = rz * rad * Mathf.Sin(ang) * 0.4f * yShrink;
                break;
            }
            case ShapePreset.FlatCloud:
            {
                float radius = Mathf.Lerp(0.55f, 1f, 0.5f + 0.5f * Mathf.Cos(ang * 2f));
                ox = rx * radius * Mathf.Cos(ang);
                oz = rz * radius * Mathf.Sin(ang);
                y = Mathf.Lerp(-_halfSize.y * 0.05f, _halfSize.y * 0.05f, Mathf.PerlinNoise(ang, y01));
                break;
            }
            case ShapePreset.Waterfall:
            {
                // dense at top, trailing down
                float topBias = Mathf.Pow(1f - y01, 1.6f);
                float radius = Mathf.Lerp(0.15f, 1f, topBias);
                ox = rx * radius * Mathf.Cos(ang * (1f + c * 0.25f));
                oz = rz * radius * Mathf.Sin(ang * (1f + c * 0.25f));
                y = Mathf.Lerp(_halfSize.y, -_halfSize.y, y01) - preset.structure.sag * _halfSize.y * y01;
                break;
            }
        }

        // Optional band separation (ribbon feel)
        if (preset.crystals.bandOffset && preset.structure.bands > 1)
        {
            float sign = (bandIndex % 2 == 0) ? -1f : 1f;
            ox += sign * preset.crystals.bandOffsetAmount * _halfSize.x;
        }

        return new Vector3(ox, y, oz);
    }

    void ApplyGaussianBias(ref Vector3 p)
    {
        if (preset.structure.gaussianBias <= 0f) return;
        float g = Mathf.Pow(Random.value, Mathf.Lerp(1f, 4f, preset.structure.gaussianBias));
        p *= Mathf.Lerp(1f, 0.65f, g); // pull some inward
    }

    void ApplyJitter(ref Vector3 p)
    {
        float j = preset.crystals.positionJitter;
        if (j <= 0f) return;
        p += new Vector3(
            (Random.value - 0.5f) * j * preset.structure.sizeMeters.x * 0.1f,
            (Random.value - 0.5f) * j * preset.structure.sizeMeters.y * 0.05f,
            (Random.value - 0.5f) * j * preset.structure.sizeMeters.z * 0.1f
        );
    }

    Quaternion RandomRotation()
    {
        float rj = preset.crystals.rotationJitter;
        return Quaternion.Euler(
            (Random.value - 0.5f) * 45f * rj,
            Random.value * 360f,
            (Random.value - 0.5f) * 45f * rj
        );
    }

    void EnsureMatPoolSize(int count)
    {
        if (_matPool == null || _matPool.Length < count)
            _matPool = new Matrix4x4[count];
    }

    void ClampParams()
    {
        // structure
        var s = preset.structure.sizeMeters;
        s = new Vector3(Mathf.Max(0.1f, s.x), Mathf.Max(0.1f, s.y), Mathf.Max(0.05f, s.z));
        preset.structure.sizeMeters = s;
        preset.structure.bands = Mathf.Clamp(preset.structure.bands, 1, 8);
        preset.structure.density = Mathf.Clamp(preset.structure.density, 100, 50000);
        // crystals
        preset.crystals.scaleRange.x = Mathf.Max(0.002f, Mathf.Min(preset.crystals.scaleRange.x, preset.crystals.scaleRange.y));
        preset.crystals.scaleRange.y = Mathf.Max(preset.crystals.scaleRange.x, preset.crystals.scaleRange.y);
        // perf
        preset.perf.lodStep = Mathf.Clamp(preset.perf.lodStep, 1, 8);
        preset.perf.maxInstancesMobile = Mathf.Clamp(preset.perf.maxInstancesMobile, 100, 20000);
        preset.perf.maxInstancesDesktop = Mathf.Clamp(preset.perf.maxInstancesDesktop, 100, 50000);
    }
}