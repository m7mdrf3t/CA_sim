using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enhanced funnel projection matching chandelier crystal references:
/// Dense solid core at bottom, gradually dispersing to sparse scattered drops at top.
/// </summary>
[System.Serializable]
public class ProjectedFunnelSurface : HangerSurface
{
    // ---------------- Placement ----------------
    [Header("Placement")]
    [Tooltip("Rim plane passes through this local point.")]
    public Vector3 center = Vector3.zero;

    [Tooltip("Axis the funnel revolves around (up). Strings fall along this axis.")]
    public Vector3 axis = Vector3.up;

    // ---------------- Shape ----------------
    [Header("Shape")]
    [Tooltip("Global scale multiplier. 2.0 = double size, 0.5 = half size.")]
    [Range(0.1f, 10f)] public float globalScale = 1f;

    [Tooltip("Rim radius (world units) - scaled by globalScale.")]
    public float rimRadius = 3f;

    [Tooltip("Vertical depth from rim plane down to the tip - scaled by globalScale.")]
    public float height = 2f;

    [Tooltip("Global curve shaping. Larger = flatter rim, sharper walls.")]
    [Range(0.5f, 6f)] public float exponent = 2.2f;

    [Header("Tip / Neck Roundness")]
    [Tooltip("How far from center the rounded tip extends.")]
    public float tipBlendRadius = 0.6f;

    [Tooltip("Curvature radius of the rounded tip (>= tipBlendRadius).")]
    public float tipCurvatureRadius = 1.2f;

    [Header("Neck Thinness")]
    [Tooltip("0 = none, 1 = very thin base by compressing radii near center.")]
    [Range(0f, 1f)] public float neckTighten = 0.5f;

    // ---------------- Projection ----------------
    [Header("Projection")]
    [Tooltip("Must match your builder's hang direction.")]
    public Vector3 hangDirection = Vector3.down;

    [Tooltip("If false, rays that miss the rim return 0; if true they hit flat rim plane.")]
    public bool capOutsideRim = true;

    // ---------------- Vertical Density Gradient (KEY FEATURE) ----------------
    [Header("Vertical Density Gradient")]
    [Tooltip("Enable dramatic top-to-bottom density transition.")]
    public bool enableVerticalGradient = true;

    [Tooltip("Power curve for density falloff. Higher = more dramatic transition.")]
    [Range(1f, 8f)] public float densityFalloffPower = 3.5f;

    [Tooltip("At what height fraction (0=tip, 1=rim) does density start dropping significantly.")]
    [Range(0f, 1f)] public float densityTransitionStart = 0.4f;

    [Tooltip("Minimum density at the very top (0=none, 1=full).")]
    [Range(0f, 1f)] public float topDensity = 0.15f;

    [Tooltip("Add random variation to vertical density for organic look.")]
    [Range(0f, 0.5f)] public float verticalDensityNoise = 0.2f;

    // ---------------- Radial Density (Core to Perimeter) ----------------
    [Header("Radial Density Distribution")]
    [Tooltip("Enable radial density falloff (dense center, sparse edges).")]
    public bool enableRadialDensity = true;

    [Tooltip("Power curve for radial density. Higher = denser core.")]
    [Range(0.5f, 4f)] public float radialDensityPower = 1.8f;

    [Tooltip("Minimum density at the perimeter.")]
    [Range(0f, 1f)] public float perimeterDensity = 0.6f;

    // ---------------- Scattered Particle Dispersion ----------------
    [Header("Particle Scattering")]
    [Tooltip("Enable random scattering (creates isolated drops like in reference).")]
    public bool enableScattering = true;

    [Tooltip("Base probability of scattering (higher = more scattered particles).")]
    [Range(0f, 1f)] public float scatterProbability = 0.3f;

    [Tooltip("Scale of scatter noise patterns.")]
    [Range(0.5f, 5f)] public float scatterNoiseScale = 2f;

    [Tooltip("Seed for scatter randomization.")]
    public float scatterSeed = 555f;

    // ---------------- 3D Turbulence ----------------
    [Header("3D Turbulence (Organic Flow)")]
    [Tooltip("Enable 3D Perlin noise for natural variation.")]
    public bool enableTurbulence = true;

    [Tooltip("Scale of turbulence noise (smaller = more detail).")]
    [Range(0.1f, 5f)] public float turbulenceScale = 1.5f;

    [Tooltip("Amplitude of turbulent displacement.")]
    [Range(0f, 0.5f)] public float turbulenceStrength = 0.12f;

    [Tooltip("Seed for turbulence noise.")]
    public float turbulenceSeed = 789f;

    // ---------------- Clustering (Organic Clumps) ----------------
    [Header("Cluster Density (Spore-like Clumps)")]
    [Tooltip("Enable clustered particle distribution.")]
    public bool enableClustering = true;

    [Tooltip("Size of individual clusters.")]
    [Range(0.1f, 3f)] public float clusterSize = 1.2f;

    [Tooltip("How pronounced the clustering is.")]
    [Range(0f, 1f)] public float clusterStrength = 0.35f;

    [Tooltip("Threshold below which clusters are removed (creates gaps).")]
    [Range(0f, 0.8f)] public float clusterThreshold = 0.25f;

    [Tooltip("Seed for cluster pattern.")]
    public float clusterSeed = 321f;

    // ---------------- Spiral Pattern (Optional) ----------------
    [Header("Spiral/Vortex Pattern (Optional)")]
    [Tooltip("Enable spiral twisting effect.")]
    public bool enableSpiral = false;

    [Tooltip("Number of spiral arms.")]
    [Range(1, 12)] public int spiralArms = 3;

    [Tooltip("How many full rotations from center to rim.")]
    [Range(0f, 4f)] public float spiralTwist = 1.5f;

    [Tooltip("Strength of spiral density modulation.")]
    [Range(0f, 1f)] public float spiralStrength = 0.4f;

    // ---------------- Original Features ----------------
    [Header("Rim Lip (Green Curve)")]
    public float rimLipHeight = 0.08f;
    [Range(0f,1f)] public float rimLipCenter = 0.92f;
    [Range(0.01f,0.5f)] public float rimLipWidth = 0.08f;
    [Range(0f,1f)] public float shoulderLift = 0.3f;
    [Range(0f,1f)] public float shoulderCenter = 0.65f;
    [Range(0.01f,0.6f)] public float shoulderWidth = 0.22f;

    [Header("Outer Line Thickness Noise")]
    public float outerNoiseAmplitude = 0.25f;
    public float outerNoiseBand = 0.9f;
    public float outerNoiseAngularFreq = 7f;
    public float outerNoiseSeed = 123f;
    [Range(0f, 1f)] public float outerNoiseUpBias = 1f;

    [Header("Outer Length Randomization")]
    [Tooltip("Width of rim band that gets random line length variation.")]
    public float rimLengthRandomBand = 0.8f;
    
    [Tooltip("Maximum random offset UPWARD (makes crystals hang higher, towards light source).")]
    public float rimLengthRandomAmplitudeUp = 0.35f;
    
    [Tooltip("Maximum random offset DOWNWARD (makes some hang lower).")]
    public float rimLengthRandomAmplitudeDown = 0.1f;
    
    [Tooltip("Undulation frequency around rim.")]
    public float rimLengthRandomFrequency = 7f;
    
    [Tooltip("Seed for rim line lengths.")]
    public float rimLengthRandomSeed = 222f;
    
    [Tooltip("Bias towards upward variation (1.0 = mostly up, 0.5 = balanced, 0 = mostly down).")]
    [Range(0f, 1f)] public float rimLengthUpwardBias = 0.8f;

    // ---------------- Helpers ----------------
    static Vector3 Ortho(Vector3 a)
    {
        return Vector3.Normalize(Vector3.Cross(Mathf.Abs(a.y) < 0.99f ? Vector3.up : Vector3.right, a));
    }

    // Enhanced 3D noise
    float Noise3D(float x, float y, float z, float seed)
    {
        float n1 = Mathf.PerlinNoise(x + seed, y + seed * 1.3f);
        float n2 = Mathf.PerlinNoise(y + seed * 1.7f, z + seed * 2.1f);
        float n3 = Mathf.PerlinNoise(z + seed * 2.3f, x + seed * 1.9f);
        return (n1 + n2 + n3) / 3f;
    }

    // Fast hash for scatter randomization
    float Hash(float x, float y, float z)
    {
        return Mathf.Abs(Mathf.Sin(x * 12.9898f + y * 78.233f + z * 45.164f) * 43758.5453f) % 1f;
    }

    // NEW: Vertical density gradient (sparse top, dense bottom)
    float GetVerticalDensity(float normalizedHeight, Vector3 worldPos)
    {
        if (!enableVerticalGradient) return 1f;

        // normalizedHeight: 0 at tip (bottom), 1 at rim (top)
        float heightFromTop = normalizedHeight; // 0=bottom, 1=top
        
        // Apply transition start point
        float adjustedHeight = Mathf.InverseLerp(densityTransitionStart, 1f, heightFromTop);
        
        // Power curve for dramatic falloff
        float densityFactor = 1f - Mathf.Pow(adjustedHeight, densityFalloffPower);
        densityFactor = Mathf.Lerp(topDensity, 1f, densityFactor);

        // Add noise variation for organic look
        if (verticalDensityNoise > 0f)
        {
            float noise = Noise3D(
                worldPos.x * 2f + scatterSeed * 1.1f,
                worldPos.y * 2f + scatterSeed * 1.3f,
                worldPos.z * 2f + scatterSeed * 1.7f,
                scatterSeed
            );
            float noiseVariation = (noise - 0.5f) * verticalDensityNoise;
            densityFactor = Mathf.Clamp01(densityFactor + noiseVariation);
        }

        return densityFactor;
    }

    // NEW: Radial density (dense center, sparse edges)
    float GetRadialDensity(float normalizedRadius)
    {
        if (!enableRadialDensity) return 1f;

        // 0 at center, 1 at rim
        float densityFactor = 1f - Mathf.Pow(normalizedRadius, radialDensityPower);
        return Mathf.Lerp(perimeterDensity, 1f, densityFactor);
    }

    // NEW: Scattering effect (random isolated particles)
    float GetScatterFactor(Vector3 worldPos, float normalizedHeight)
    {
        if (!enableScattering) return 1f;

        // More scattering at the top
        float heightInfluence = Mathf.Lerp(0.3f, 1f, normalizedHeight);
        float effectiveScatterProb = scatterProbability * heightInfluence;

        // 3D noise pattern for scatter distribution
        float scatterNoise = Noise3D(
            worldPos.x / scatterNoiseScale + scatterSeed,
            worldPos.y / scatterNoiseScale + scatterSeed * 1.4f,
            worldPos.z / scatterNoiseScale + scatterSeed * 1.8f,
            scatterSeed
        );

        // Create scattered particles with sharp threshold
        float scatterValue = scatterNoise > (1f - effectiveScatterProb) ? 1f : 0.2f;
        
        // Add some random variation to make truly isolated particles
        float hash = Hash(worldPos.x, worldPos.y, worldPos.z);
        if (hash < effectiveScatterProb * 0.3f) scatterValue = 1f;

        return scatterValue;
    }

    // Clustering
    float GetClusterFactor(Vector3 worldPos)
    {
        if (!enableClustering) return 1f;

        float clusterNoise = Noise3D(
            worldPos.x / clusterSize + clusterSeed,
            worldPos.y / clusterSize + clusterSeed * 1.3f,
            worldPos.z / clusterSize + clusterSeed * 1.7f,
            clusterSeed
        );

        // Apply threshold for gap creation
        if (clusterNoise < clusterThreshold) return 0f;

        // Soften the remaining values
        float normalizedValue = (clusterNoise - clusterThreshold) / (1f - clusterThreshold);
        return Mathf.Lerp(1f, Mathf.Pow(normalizedValue, 0.7f), clusterStrength);
    }

    // Spiral pattern
    float GetSpiralFactor(float r, float theta, float rimRadius)
    {
        if (!enableSpiral || spiralArms == 0) return 1f;

        float t = r / Mathf.Max(1e-5f, rimRadius);
        float spiralAngle = theta + t * spiralTwist * Mathf.PI * 2f;
        float spiralPhase = Mathf.Sin(spiralAngle * spiralArms) * 0.5f + 0.5f;
        return Mathf.Lerp(1f, spiralPhase, spiralStrength);
    }

    // 3D turbulent displacement
    Vector3 GetTurbulentDisplacement(Vector3 worldPos)
    {
        if (!enableTurbulence) return Vector3.zero;

        float scale = turbulenceScale;
        float nx = Noise3D(worldPos.x / scale, worldPos.y / scale, worldPos.z / scale, turbulenceSeed);
        float ny = Noise3D(worldPos.x / scale + 13.7f, worldPos.y / scale + 7.3f, worldPos.z / scale + 3.1f, turbulenceSeed);
        float nz = Noise3D(worldPos.x / scale + 21.1f, worldPos.y / scale + 17.9f, worldPos.z / scale + 11.3f, turbulenceSeed);

        return new Vector3(
            (nx - 0.5f) * turbulenceStrength,
            (ny - 0.5f) * turbulenceStrength,
            (nz - 0.5f) * turbulenceStrength
        );
    }

    float ProfileY(float r)
    {
        float R = Mathf.Max(1e-4f, rimRadius * globalScale);
        float H = Mathf.Max(1e-4f, height * globalScale);

        float t = Mathf.Clamp01(r / R);
        float tTight = Mathf.Pow(t, 1f + 3f * neckTighten);
        float s = tTight * tTight * tTight * (tTight * (6f * tTight - 15f) + 10f);
        float yBase = -H * (1f - s);

        float Rc = Mathf.Max(tipCurvatureRadius, Mathf.Max(1e-3f, tipBlendRadius));
        float rClamped = Mathf.Min(r, Rc - 1e-5f);
        float yCircle = (-H + Rc) - Mathf.Sqrt(Rc * Rc - rClamped * rClamped);
        float rb = Mathf.Max(1e-4f, tipBlendRadius);
        float u = Mathf.Clamp01(r / rb);
        float w = u * u * u * (u * (6f * u - 15f) + 10f);
        float y = Mathf.Lerp(yCircle, yBase, w);

        if (shoulderLift > 0f && shoulderWidth > 0.001f)
        {
            float muS = shoulderCenter;
            float sigmaS = Mathf.Max(0.01f, shoulderWidth) * 0.5f;
            float gS = Mathf.Exp(-0.5f * (t - muS) * (t - muS) / (sigmaS * sigmaS));
            y += shoulderLift * H * 0.15f * gS;
        }

        if (rimLipHeight != 0f && rimLipWidth > 0.001f)
        {
            float muL = Mathf.Clamp01(rimLipCenter);
            float sigmaL = Mathf.Max(0.01f, rimLipWidth) * 0.5f;
            float gL = Mathf.Exp(-0.5f * (t - muL) * (t - muL) / (sigmaL * sigmaL));
            y += rimLipHeight * gL;
        }

        return y;
    }

    float GetRimLengthRandomization(float r, float theta)
    {
        if ((rimLengthRandomAmplitudeUp <= 0f && rimLengthRandomAmplitudeDown <= 0f) || rimLengthRandomBand <= 1e-4f)
            return 0f;

        float R = Mathf.Max(1e-5f, rimRadius * globalScale);
        float band = Mathf.Clamp01(1f - Mathf.Abs(r - R) / (rimLengthRandomBand * globalScale));
        if (band <= 0f) return 0f;

        // Get noise value [0,1]
        float theta01 = (theta + Mathf.PI) / (2f * Mathf.PI);
        float n = Mathf.PerlinNoise(theta01 * rimLengthRandomFrequency + rimLengthRandomSeed * 0.013f,
                                    rimLengthRandomSeed * 0.151f);

        // Apply bias: push noise distribution toward upward bias
        float biased = Mathf.Pow(n, Mathf.Lerp(2f, 0.5f, rimLengthUpwardBias));
        
        // Map to asymmetric range
        float delta;
        if (biased > 0.5f)
        {
            // Upper half maps to upward movement (negative = shorter line = higher crystal)
            float t = (biased - 0.5f) * 2f;
            delta = -t * rimLengthRandomAmplitudeUp; // negative = pull up
        }
        else
        {
            // Lower half maps to downward movement (positive = longer line = lower crystal)
            float t = biased * 2f;
            delta = (1f - t) * rimLengthRandomAmplitudeDown; // positive = push down
        }

        return delta * band;
    }

    float RimThicknessUpOffset(float r, float theta, float R)
    {
        if (outerNoiseAmplitude <= 0f || outerNoiseBand <= 1e-4f) return 0f;

        float band = Mathf.Clamp01(1f - Mathf.Abs(r - R) / (outerNoiseBand * globalScale));
        if (band <= 0f) return 0f;

        float theta01 = (theta + Mathf.PI) / (2f * Mathf.PI);
        float n = Mathf.PerlinNoise(theta01 * outerNoiseAngularFreq + outerNoiseSeed * 0.017f,
                                    outerNoiseSeed * 0.113f);
        float m = n * 2f - 1f;
        float up = Mathf.Lerp(m, Mathf.Abs(m), outerNoiseUpBias);
        return outerNoiseAmplitude * band * Mathf.Max(0f, up);
    }

    // ---------------- MAIN PROJECTION ----------------
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 A = axis.normalized;
        Vector3 d = hangDirection.normalized;

        float s = Vector3.Dot(d, A);
        if (Mathf.Abs(s) < 1e-4f) return 0f;

        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 U = Ortho(A);
        Vector3 V = Vector3.Cross(A, U);

        // Apply turbulence
        Vector3 turbulentOffset = GetTurbulentDisplacement(point.position);
        Vector3 adjustedPos = point.position + turbulentOffset;

        Vector3 rel = adjustedPos - mid;
        float y0 = Vector3.Dot(rel, A);
        float x = Vector3.Dot(rel, U);
        float z = Vector3.Dot(rel, V);
        float r0 = Mathf.Sqrt(x * x + z * z);

        float R = Mathf.Max(1e-5f, rimRadius * globalScale);
        float H = Mathf.Max(1e-4f, height * globalScale);
        
        float ySurf;
        if (r0 <= R + 1e-5f) ySurf = ProfileY(r0);
        else
        {
            if (!capOutsideRim) return 0f;
            ySurf = 0f;
        }

        float theta = Mathf.Atan2(z, x);
        ySurf += RimThicknessUpOffset(r0, theta, R);

        float tHit = (ySurf - y0) / s;
        tHit += GetRimLengthRandomization(r0, theta);

        if (tHit < 0f) return 0f;

        // Calculate normalized positions for density
        float normalizedHeight = Mathf.Clamp01((ySurf + H) / H); // 0=tip, 1=rim
        float normalizedRadius = Mathf.Clamp01(r0 / R);

        // Apply all density factors (multiplicative)
        float density = 1f;
        density *= GetVerticalDensity(normalizedHeight, point.position);
        density *= GetRadialDensity(normalizedRadius);
        density *= GetClusterFactor(point.position);
        density *= GetScatterFactor(point.position, normalizedHeight);
        density *= GetSpiralFactor(r0, theta, R);

        // Apply density threshold for sharp cutoff
        if (density < 0.15f) return 0f;

        // Modulate final length
        return tHit * Mathf.Clamp01(density);
    }

    // ---------------- Gizmos ----------------
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 A = axis.normalized;
        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 U = Ortho(A);
        Vector3 V = Vector3.Cross(A, U);

        // Rim circle
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Vector3 prev = mid + (U * rimRadius * globalScale);
        for (int i = 1; i <= 24; i++)
        {
            float ang = (i / 24f) * Mathf.PI * 2f;
            Vector3 p = mid + (Mathf.Cos(ang) * U + Mathf.Sin(ang) * V) * rimRadius * globalScale;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        // Meridian curves
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        int meridianCount = enableSpiral ? spiralArms * 2 : 8;
        for (int k = 0; k < meridianCount; k++)
        {
            float ang = (k / (float)meridianCount) * Mathf.PI * 2f;
            Vector3 dirR = Mathf.Cos(ang) * U + Mathf.Sin(ang) * V;

            Vector3 last = mid;
            for (int i = 0; i <= 48; i++)
            {
                float t = i / 48f;
                float r = t * rimRadius * globalScale;
                float y = ProfileY(r);
                y += RimThicknessUpOffset(r, ang, rimRadius * globalScale);

                Vector3 p = mid + A * y + dirR * r;
                if (i > 0) Gizmos.DrawLine(last, p);
                last = p;
            }
        }

        if (points == null) return;

        Gizmos.color = Color.cyan;
        Vector3 d = hangDirection.normalized;
        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;
            float len = CalculateLength(pt, relativeTo);
            if (len <= 0f) continue;
            Vector3 O = pt.position;
            Vector3 end = O + d * len;
            Gizmos.DrawLine(O, end);
            Gizmos.DrawWireSphere(end, 0.01f);
        }
    }
}