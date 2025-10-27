using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a circular ring of crystals with varied heights forming a chandelier cluster.
/// Crystals drop vertically and land on a torus-like ring surface with noise variations.
/// </summary>
[System.Serializable]
public class CircularRingChandelierSurfaceFull : HangerSurface
{
    // ---------------- Placement ----------------
    [Header("Placement")]
    [Tooltip("Center point of the ring (local space, converted with relativeTo.TransformPoint).")]
    public Vector3 center = Vector3.zero;

    [Tooltip("Up axis for the ring orientation.")]
    public Vector3 axis = Vector3.up;

    // ---------------- Ring Shape ----------------
    [Header("Ring Shape")]
    [Tooltip("Radius from center to the middle of the ring band (world units).")]
    public float ringRadius = 2f;

    [Tooltip("Width of the ring band (how thick the ring is radially).")]
    public float ringWidth = 1.2f;

    [Tooltip("Base height of the ring surface (negative = below center plane).")]
    public float baseHeight = -1f;

    [Tooltip("Additional height variation for the ring surface (creates waves/bumps).")]
    public float heightVariation = 0.5f;

    // ---------------- Height Variation / Noise ----------------
    [Header("Height Noise")]
    [Tooltip("Radial frequency (number of waves around the circle).")]
    public float radialFrequency = 8f;

    [Tooltip("Noise seed for randomization.")]
    public float noiseSeed = 42f;

    [Tooltip("Additional Perlin noise scale for organic variation.")]
    public float perlinScale = 3f;

    [Tooltip("Strength of the undulating wave pattern (0-1).")]
    [Range(0f, 1f)] public float waveStrength = 0.7f;

    // ---------------- Density Control ----------------
    [Header("Crystal Density")]
    [Tooltip("Inner radius of the ring (crystals inside this are rejected).")]
    public float innerRadius = 1.2f;

    [Tooltip("Outer radius of the ring (crystals outside this are rejected).")]
    public float outerRadius = 2.8f;

    [Tooltip("If true, only accept points within the ring band; if false, accept all points.")]
    public bool filterByRing = true;

    // ---------------- Random Length Extensions ----------------
    [Header("Random Crystal Extensions")]
    [Tooltip("Probability that a crystal extends below the surface (0-1). 0.3 = 30% of crystals.")]
    [Range(0f, 1f)] public float extensionProbability = 0.3f;

    [Tooltip("Minimum additional length to add (world units).")]
    public float minExtension = 0.2f;

    [Tooltip("Maximum additional length to add (world units).")]
    public float maxExtension = 1.5f;

    [Tooltip("Seed for random extension generation.")]
    public int extensionSeed = 789;

    // ---------------- Projection ----------------
    [Header("Projection")]
    [Tooltip("Direction crystals hang/drop from (should be parallel to -axis).")]
    public Vector3 hangDirection = Vector3.down;

    // ---------------- Helpers ----------------
    static Vector3 Ortho(Vector3 a)
    {
        return Vector3.Normalize(Vector3.Cross(Mathf.Abs(a.y) < 0.99f ? Vector3.up : Vector3.right, a));
    }

    // Calculate random extension for a point (deterministic based on position)
    float GetRandomExtension(Vector3 position)
    {
        // Use position as seed for deterministic random
        float hash = (position.x * 73.1f + position.y * 151.7f + position.z * 283.4f + extensionSeed);
        hash = Mathf.Abs(Mathf.Sin(hash * 127.1f) * 43758.5453f);
        float random01 = hash - Mathf.Floor(hash);
        
        // Check if this point should extend
        if (random01 > extensionProbability)
            return 0f;
        
        // Generate extension length using another hash
        float hash2 = Mathf.Abs(Mathf.Sin(hash * 269.5f) * 183.3f);
        float lengthRandom = hash2 - Mathf.Floor(hash2);
        
        return Mathf.Lerp(minExtension, maxExtension, lengthRandom);
    }

    // Calculate surface height at a given radial distance and angle
    float SurfaceHeight(float r, float theta)
    {
        // Base ring surface with cosine waves around the circle
        float wave = Mathf.Cos(theta * radialFrequency) * waveStrength;
        
        // Add Perlin noise for organic variation
        float theta01 = (theta + Mathf.PI) / (2f * Mathf.PI);
        float perlin = Mathf.PerlinNoise(
            theta01 * perlinScale + noiseSeed * 0.1f,
            r * 0.5f + noiseSeed * 0.2f
        );
        perlin = (perlin - 0.5f) * 2f; // Convert to [-1, 1]
        
        // Combine base height, wave pattern, and noise
        float height = baseHeight + (wave + perlin * (1f - waveStrength)) * heightVariation;
        
        // Optional: make the ring slightly curved (lower in the middle)
        float ringCenter = (innerRadius + outerRadius) * 0.5f;
        float ringHalfWidth = (outerRadius - innerRadius) * 0.5f;
        float distFromRingCenter = Mathf.Abs(r - ringCenter);
        float ringCurve = Mathf.Pow(distFromRingCenter / ringHalfWidth, 2f) * heightVariation * 0.2f;
        
        return height - ringCurve;
    }

    // ---------------- Projection ----------------
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 A = axis.normalized;
        Vector3 d = hangDirection.normalized;

        // Check if drop direction is roughly parallel to axis
        float s = Vector3.Dot(d, A);
        if (Mathf.Abs(s) < 1e-4f)
        {
            Debug.LogWarning("Hang direction not parallel to axis!");
            return 0f;
        }

        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 U = Ortho(A);
        Vector3 V = Vector3.Cross(A, U);

        // Get point position relative to ring center
        Vector3 rel = point.position - mid;
        float y0 = Vector3.Dot(rel, A);  // Height above/below center plane
        float x = Vector3.Dot(rel, U);
        float z = Vector3.Dot(rel, V);
        float r = Mathf.Sqrt(x * x + z * z);  // Radial distance from center
        float theta = Mathf.Atan2(z, x);      // Angle around the ring

        // Filter by ring boundaries if enabled
        if (filterByRing)
        {
            if (r < innerRadius || r > outerRadius)
            {
                // Debug.Log($"Point filtered: r={r:F2}, inner={innerRadius}, outer={outerRadius}");
                return 0f;
            }
        }

        // Calculate surface height at this position
        float ySurf = SurfaceHeight(r, theta);

        // Calculate intersection distance along hang direction
        // If s is negative (hanging down with axis up), we need y0 > ySurf
        float tHit = (ySurf - y0) / s;
        
        if (tHit < 0f) return 0f;
        
        // Add random extension for some crystals
        float extension = GetRandomExtension(point.position);
        
        // Debug for first few points
        // Debug.Log($"Point: y0={y0:F2}, ySurf={ySurf:F2}, s={s:F2}, tHit={tHit:F2}, extension={extension:F2}, r={r:F2}");
        
        return tHit + extension;
    }

    // ---------------- Gizmos ----------------
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 A = axis.normalized;
        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 U = Ortho(A);
        Vector3 V = Vector3.Cross(A, U);

        // Draw inner and outer ring boundaries
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        DrawCircle(mid, A, U, V, innerRadius, 32);
        DrawCircle(mid, A, U, V, outerRadius, 32);

        // Draw ring surface with height variations
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        int radialSteps = 36;
        int ringSteps = 12;
        
        for (int i = 0; i < radialSteps; i++)
        {
            float theta1 = (i / (float)radialSteps) * Mathf.PI * 2f;
            float theta2 = ((i + 1) / (float)radialSteps) * Mathf.PI * 2f;
            
            Vector3 dir1 = Mathf.Cos(theta1) * U + Mathf.Sin(theta1) * V;
            Vector3 dir2 = Mathf.Cos(theta2) * U + Mathf.Sin(theta2) * V;
            
            for (int j = 0; j < ringSteps; j++)
            {
                float t = j / (float)ringSteps;
                float r = Mathf.Lerp(innerRadius, outerRadius, t);
                
                float h1 = SurfaceHeight(r, theta1);
                float h2 = SurfaceHeight(r, theta2);
                
                Vector3 p1 = mid + dir1 * r + A * h1;
                Vector3 p2 = mid + dir2 * r + A * h2;
                
                Gizmos.DrawLine(p1, p2);
            }
            
            // Draw radial lines
            for (int j = 0; j <= ringSteps; j++)
            {
                float t = j / (float)ringSteps;
                float r = Mathf.Lerp(innerRadius, outerRadius, t);
                float h = SurfaceHeight(r, theta1);
                Vector3 p = mid + dir1 * r + A * h;
                
                if (j > 0)
                {
                    float rPrev = Mathf.Lerp(innerRadius, outerRadius, (j - 1) / (float)ringSteps);
                    float hPrev = SurfaceHeight(rPrev, theta1);
                    Vector3 pPrev = mid + dir1 * rPrev + A * hPrev;
                    Gizmos.DrawLine(pPrev, p);
                }
            }
        }

        // Draw crystal drop lines
        if (points == null) return;

        Gizmos.color = Color.yellow;
        Vector3 d = hangDirection.normalized;
        
        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;
            float len = CalculateLength(pt, relativeTo);
            if (len <= 0f) continue;
            
            Vector3 O = pt.position;
            Vector3 end = O + d * len;
            Gizmos.DrawLine(O, end);
            Gizmos.DrawWireSphere(end, 0.02f);
        }
    }

    void DrawCircle(Vector3 center, Vector3 normal, Vector3 u, Vector3 v, float radius, int segments)
    {
        Vector3 prev = center + u * radius;
        for (int i = 1; i <= segments; i++)
        {
            float ang = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 p = center + (Mathf.Cos(ang) * u + Mathf.Sin(ang) * v) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}