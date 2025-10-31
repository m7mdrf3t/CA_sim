using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Linear Options Surface - Creates butterfly/saddle-shaped option payoff visualizations
/// Produces the characteristic "wings" pattern seen in option spreads and Greek surfaces
/// </summary>
[System.Serializable]
public class LinearOptionsSurface : HangerSurface
{
    // ---------------- Placement ----------------
    [Header("Placement")]
    [Tooltip("Center point of the surface.")]
    public Vector3 center = Vector3.zero;

    [Tooltip("Primary axis (typically up/vertical for Greeks surface).")]
    public Vector3 primaryAxis = Vector3.up;

    [Header("Rotation")]
    [Tooltip("Rotation around the primary axis (degrees).")]
    [Range(0f, 360f)] public float rotationAngle = 0f;

    [Tooltip("Additional tilt angle (pitch) in degrees.")]
    [Range(-90f, 90f)] public float tiltAngle = 0f;

    [Tooltip("Roll angle around the forward axis (degrees).")]
    [Range(-90f, 90f)] public float rollAngle = 0f;

    // ---------------- Shape Parameters ----------------
    [Header("Shape & Size")]
    [Tooltip("Global scale multiplier for entire surface. 2.0 = double size, 0.5 = half size.")]
    [Range(0.1f, 10f)] public float globalScale = 1f;

    [Tooltip("Width of the butterfly wings (X-axis spread).")]
    public float wingSpan = 4f;

    [Tooltip("Depth of the butterfly (Z-axis spread).")]
    public float wingDepth = 3f;

    [Tooltip("Maximum height of the surface peaks.")]
    public float maxHeight = 2.5f;

    [Tooltip("Depth of the central valley/saddle point.")]
    public float valleyDepth = 1.5f;

    [Header("Butterfly Shape Control")]
    [Tooltip("Sharpness of the wing peaks. Higher = sharper, more defined peaks.")]
    [Range(0.5f, 4f)] public float peakSharpness = 1.8f;

    [Tooltip("How pronounced the central saddle/valley is.")]
    [Range(0f, 2f)] public float saddleIntensity = 1.2f;

    [Tooltip("Wing curvature. Higher = more curved wings.")]
    [Range(0.5f, 3f)] public float wingCurvature = 1.5f;

    [Tooltip("Asymmetry between left/right wings (0.5 = symmetric).")]
    [Range(0f, 1f)] public float wingAsymmetry = 0.5f;

    // ---------------- Surface Type ----------------
    [Header("Surface Type")]
    [Tooltip("Type of option surface to generate.")]
    public OptionSurfaceType surfaceType = OptionSurfaceType.ButterflySpread;

    public enum OptionSurfaceType
    {
        ButterflySpread,      // Classic butterfly with two peaks
        Saddle,               // Pure saddle point (hyperbolic paraboloid)
        Greeks,               // Option Greeks surface (Gamma/Vega)
        VolatilitySmile,      // Volatility smile/smirk surface
        Custom                // Use custom parameters
    }

    // ---------------- Strike/Price Parameters ----------------
    [Header("Option Parameters (For Realism)")]
    [Tooltip("Lower strike price position (normalized -1 to 1).")]
    [Range(-1f, 0f)] public float lowerStrike = -0.6f;

    [Tooltip("Middle strike price position.")]
    [Range(-0.5f, 0.5f)] public float middleStrike = 0f;

    [Tooltip("Upper strike price position (normalized -1 to 1).")]
    [Range(0f, 1f)] public float upperStrike = 0.6f;

    // ---------------- Projection Control ----------------
    [Header("Projection Control")]
    [Tooltip("Direction strings hang from surface.")]
    public Vector3 hangDirection = Vector3.down;

    [Tooltip("Only project points from this side of surface.")]
    public ProjectionSide projectionSide = ProjectionSide.Above;

    public enum ProjectionSide
    {
        Above,      // Only project points above the surface
        Below,      // Only project points below the surface
        Both        // Project from both sides (creates double layer)
    }

    [Tooltip("Project points outside surface boundary.")]
    public bool projectOutsideBounds = true;

    [Tooltip("Maximum projection distance. Points further away won't connect.")]
    public float maxProjectionDistance = 10f;

    // ---------------- Density Control ----------------
    [Header("Density Distribution")]
    [Tooltip("Enable variable density (denser near peaks/valleys).")]
    public bool enableDensityGradient = true;

    [Tooltip("Minimum density in flat regions.")]
    [Range(0f, 1f)] public float minDensity = 0.3f;

    [Tooltip("Density concentration near features.")]
    [Range(0.5f, 3f)] public float densityConcentration = 1.5f;

    // ---------------- Grid Pattern ----------------
    [Header("Grid Lines (Curtain Effect)")]
    [Tooltip("Enable vertical grid lines (curtain effect from images).")]
    public bool enableGridLines = true;

    [Tooltip("Number of radial grid lines.")]
    [Range(24, 360)] public int gridLineCount = 120;

    [Tooltip("Grid line density threshold.")]
    [Range(0f, 1f)] public float gridDensity = 0.8f;

    // ---------------- Volume Variation ----------------
    [Header("Volume Variation (3D Thickness)")]
    [Tooltip("Enable random variation to give 3D volume/thickness.")]
    public bool enableVolumeVariation = true;

    [Tooltip("Maximum random offset perpendicular to hang direction.")]
    [Range(0f, 2f)] public float volumeVariationAmount = 0.3f;

    [Tooltip("Scale of noise pattern for volume variation.")]
    [Range(0.1f, 5f)] public float volumeNoiseScale = 1.5f;

    [Tooltip("Seed for volume randomization.")]
    public float volumeSeed = 456f;

    [Tooltip("Bias variation toward surface center (higher = more centered).")]
    [Range(0f, 1f)] public float volumeCenterBias = 0.3f;

    // ---------------- Helper Functions ----------------
    
    static Vector3 Ortho(Vector3 a)
    {
        return Vector3.Normalize(Vector3.Cross(Mathf.Abs(a.y) < 0.99f ? Vector3.up : Vector3.right, a));
    }

    float Noise(float x, float y, float seed = 0f)
    {
        return Mathf.PerlinNoise(x + seed, y + seed * 1.3f);
    }

    /// <summary>
    /// Calculate 3D volume variation offset perpendicular to hang direction
    /// </summary>
    Vector3 CalculateVolumeVariation(Vector3 worldPos, Vector3 hangDir)
    {
        if (!enableVolumeVariation || volumeVariationAmount <= 0f)
            return Vector3.zero;

        float scale = volumeNoiseScale;
        
        // 3D noise for organic variation
        float nx = Mathf.PerlinNoise(worldPos.x / scale + volumeSeed, worldPos.y / scale + volumeSeed * 1.3f);
        float ny = Mathf.PerlinNoise(worldPos.y / scale + volumeSeed * 1.7f, worldPos.z / scale + volumeSeed * 2.1f);
        float nz = Mathf.PerlinNoise(worldPos.z / scale + volumeSeed * 2.3f, worldPos.x / scale + volumeSeed * 1.9f);
        
        // Convert to [-1, 1] range
        nx = (nx - 0.5f) * 2f;
        ny = (ny - 0.5f) * 2f;
        nz = (nz - 0.5f) * 2f;
        
        // Create perpendicular offset vector
        Vector3 offset = new Vector3(nx, ny, nz);
        
        // Remove component along hang direction to keep it perpendicular
        offset = offset - hangDir * Vector3.Dot(offset, hangDir);
        offset = offset.normalized;
        
        // Apply variation amount with center bias
        float variation = volumeVariationAmount;
        
        // Reduce variation near center if center bias is active
        if (volumeCenterBias > 0f)
        {
            float distFromCenter = offset.magnitude;
            float centerFactor = Mathf.Lerp(1f, 0.2f, volumeCenterBias);
            variation *= Mathf.Lerp(centerFactor, 1f, distFromCenter);
        }
        
        return offset * variation * (Mathf.Abs(nx) + Mathf.Abs(ny) + Mathf.Abs(nz)) / 3f;
    }

    /// <summary>
    /// Get rotation matrix for the surface
    /// </summary>
    Matrix4x4 GetRotationMatrix()
    {
        // Create rotation around primary axis
        Quaternion rotation = Quaternion.AngleAxis(rotationAngle, primaryAxis.normalized);
        
        // Apply tilt (pitch)
        if (Mathf.Abs(tiltAngle) > 0.01f)
        {
            Vector3 right = Ortho(primaryAxis.normalized);
            rotation = Quaternion.AngleAxis(tiltAngle, right) * rotation;
        }
        
        // Apply roll
        if (Mathf.Abs(rollAngle) > 0.01f)
        {
            rotation = Quaternion.AngleAxis(rollAngle, Vector3.forward) * rotation;
        }
        
        return Matrix4x4.Rotate(rotation);
    }

    /// <summary>
    /// Transform a direction vector by the surface rotation
    /// </summary>
    Vector3 RotateVector(Vector3 v)
    {
        Matrix4x4 rotMatrix = GetRotationMatrix();
        return rotMatrix.MultiplyVector(v);
    }

    // ---------------- Surface Height Calculation ----------------
    
    /// <summary>
    /// Calculate surface height based on normalized coordinates
    /// </summary>
    float CalculateSurfaceHeight(float x, float z)
    {
        // Normalize coordinates
        float nx = x / (wingSpan * globalScale);
        float nz = z / (wingDepth * globalScale);

        float height = 0f;

        switch (surfaceType)
        {
            case OptionSurfaceType.ButterflySpread:
                height = CalculateButterflyHeight(nx, nz);
                break;
            
            case OptionSurfaceType.Saddle:
                height = CalculateSaddleHeight(nx, nz);
                break;
            
            case OptionSurfaceType.Greeks:
                height = CalculateGreeksHeight(nx, nz);
                break;
            
            case OptionSurfaceType.VolatilitySmile:
                height = CalculateVolatilityHeight(nx, nz);
                break;
            
            case OptionSurfaceType.Custom:
                height = CalculateCustomHeight(nx, nz);
                break;
        }

        return height * globalScale;
    }

    /// <summary>
    /// Classic butterfly spread payoff surface
    /// Creates two symmetric peaks with central valley
    /// </summary>
    float CalculateButterflyHeight(float x, float z)
    {
        // Two Gaussian peaks for butterfly wings
        float leftPeak = Mathf.Exp(-peakSharpness * Mathf.Pow(x - lowerStrike, 2f) - wingCurvature * z * z);
        float rightPeak = Mathf.Exp(-peakSharpness * Mathf.Pow(x - upperStrike, 2f) - wingCurvature * z * z);
        
        // Central valley (saddle point)
        float centralValley = -saddleIntensity * Mathf.Exp(-peakSharpness * Mathf.Pow(x - middleStrike, 2f) - wingCurvature * 0.5f * z * z);
        
        // Combine with asymmetry
        float leftWeight = wingAsymmetry;
        float rightWeight = 1f - wingAsymmetry;
        
        float combinedHeight = (leftPeak * leftWeight + rightPeak * rightWeight) * maxHeight + centralValley * valleyDepth;
        
        // Add subtle base curvature for natural flow
        float baseCurve = -0.1f * (x * x + z * z * 0.5f);
        
        return combinedHeight + baseCurve;
    }

    /// <summary>
    /// Pure hyperbolic paraboloid (saddle surface)
    /// </summary>
    float CalculateSaddleHeight(float x, float z)
    {
        float height = saddleIntensity * (x * x - z * z) * maxHeight;
        
        // Add wing curvature modulation
        float modulation = Mathf.Exp(-wingCurvature * (x * x + z * z));
        
        return height * modulation;
    }

    /// <summary>
    /// Option Greeks surface (Gamma, Vega, etc.)
    /// Typically shows peak near at-the-money
    /// </summary>
    float CalculateGreeksHeight(float x, float z)
    {
        // Peak at-the-money (center)
        float atmPeak = Mathf.Exp(-peakSharpness * (x * x + z * z * wingCurvature));
        
        // Time decay effect (z-axis could represent time)
        float timeDecay = Mathf.Exp(-Mathf.Abs(z) * 0.5f);
        
        // Moneyness effect (x-axis represents strike/spot ratio)
        float moneyness = 1f - Mathf.Abs(x) * 0.3f;
        
        return atmPeak * timeDecay * moneyness * maxHeight;
    }

    /// <summary>
    /// Volatility smile/smirk surface
    /// U-shaped or skewed curve
    /// </summary>
    float CalculateVolatilityHeight(float x, float z)
    {
        // Quadratic smile
        float smile = (x * x * peakSharpness + Mathf.Abs(x) * wingAsymmetry) * maxHeight;
        
        // Depth modulation
        float depthEffect = Mathf.Exp(-wingCurvature * z * z);
        
        // Minimum volatility floor
        float floor = valleyDepth * 0.3f;
        
        return (smile * depthEffect + floor);
    }

    /// <summary>
    /// Custom height using all parameters
    /// </summary>
    float CalculateCustomHeight(float x, float z)
    {
        return CalculateButterflyHeight(x, z);
    }

    /// <summary>
    /// Calculate density for a point (denser near interesting features)
    /// </summary>
    float CalculateDensity(float x, float z, float height)
    {
        if (!enableDensityGradient) return 1f;

        // Normalize coordinates
        float nx = x / (wingSpan * globalScale);
        float nz = z / (wingDepth * globalScale);
        
        // Distance from center
        float distFromCenter = Mathf.Sqrt(nx * nx + nz * nz);
        
        // Height-based density (denser near peaks and valleys)
        float heightFactor = Mathf.Abs(height) / (maxHeight * globalScale);
        heightFactor = Mathf.Pow(heightFactor, 1f / densityConcentration);
        
        // Radial density (denser near center)
        float radialFactor = Mathf.Exp(-distFromCenter * 0.5f);
        
        // Combine factors
        float density = Mathf.Lerp(minDensity, 1f, heightFactor * radialFactor);
        
        // Grid line effect
        if (enableGridLines)
        {
            float theta = Mathf.Atan2(nz, nx);
            float thetaNormalized = (theta + Mathf.PI) / (2f * Mathf.PI);
            float gridPhase = Mathf.Repeat(thetaNormalized * gridLineCount, 1f);
            
            // Make grid lines by increasing density along certain angles
            if (gridPhase < gridDensity)
            {
                density = Mathf.Max(density, 0.9f);
            }
        }
        
        return density;
    }

    // ---------------- Main Projection Method ----------------
    
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 A = primaryAxis.normalized;
        Vector3 d = hangDirection.normalized;

        // Check if hang direction is valid
        float s = Vector3.Dot(d, A);
        if (Mathf.Abs(s) < 1e-4f) return 0f;

        // Transform center to world space
        Vector3 mid = relativeTo.TransformPoint(center);
        
        // Create local coordinate system with rotation
        Vector3 U = RotateVector(Ortho(A));  // X-axis (rotated)
        Vector3 V = RotateVector(Vector3.Cross(A, Ortho(A)));  // Z-axis (rotated)
        Vector3 rotatedA = RotateVector(A);  // Y-axis (rotated)

        // Get point position relative to surface center
        Vector3 rel = point.position - mid;
        float y0 = Vector3.Dot(rel, rotatedA);  // Height of point
        float x = Vector3.Dot(rel, U);   // X coordinate
        float z = Vector3.Dot(rel, V);   // Z coordinate

        // Check if point is within bounds
        float maxX = wingSpan * globalScale;
        float maxZ = wingDepth * globalScale;
        
        if (!projectOutsideBounds)
        {
            if (Mathf.Abs(x) > maxX || Mathf.Abs(z) > maxZ)
                return 0f;
        }

        // Calculate surface height at this X,Z position
        float surfaceHeight = CalculateSurfaceHeight(x, z);

        // Calculate intersection distance
        float tHit = (surfaceHeight - y0) / Vector3.Dot(d, rotatedA);

        // Check projection side to avoid double layer
        switch (projectionSide)
        {
            case ProjectionSide.Above:
                // Only project if point is above surface
                if (y0 < surfaceHeight) return 0f;
                break;
            
            case ProjectionSide.Below:
                // Only project if point is below surface
                if (y0 > surfaceHeight) return 0f;
                break;
            
            case ProjectionSide.Both:
                // Allow both (creates double layer)
                break;
        }

        // Check valid intersection
        if (tHit < 0f || tHit > maxProjectionDistance) return 0f;

        // Calculate density for this point
        float density = CalculateDensity(x, z, surfaceHeight);

        // Apply density threshold
        if (density < 0.1f) return 0f;

        // Modulate length by density
        float finalLength = tHit * density;

        return ApplyHeightOffset(finalLength);
    }

    // ---------------- Gizmos for Visualization ----------------
    
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        Vector3 A = primaryAxis.normalized;
        Vector3 mid = relativeTo.TransformPoint(center);
        Vector3 U = RotateVector(Ortho(A));
        Vector3 V = RotateVector(Vector3.Cross(A, Ortho(A)));
        Vector3 rotatedA = RotateVector(A);

        // Draw boundary
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        float maxX = wingSpan * globalScale;
        float maxZ = wingDepth * globalScale;
        
        Vector3[] corners = new Vector3[4]
        {
            mid + U * maxX + V * maxZ,
            mid - U * maxX + V * maxZ,
            mid - U * maxX - V * maxZ,
            mid + U * maxX - V * maxZ
        };
        
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
        }

        // Draw surface contour lines
        Gizmos.color = new Color(1f, 0.7f, 0f, 0.3f);
        int resolution = 32;
        
        // Draw grid
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            float x = Mathf.Lerp(-maxX, maxX, t);
            
            Vector3 lastPoint = Vector3.zero;
            for (int j = 0; j <= resolution; j++)
            {
                float u = j / (float)resolution;
                float z = Mathf.Lerp(-maxZ, maxZ, u);
                
                float h = CalculateSurfaceHeight(x, z);
                Vector3 p = mid + U * x + V * z + rotatedA * h;
                
                if (j > 0) Gizmos.DrawLine(lastPoint, p);
                lastPoint = p;
            }
        }

        // Draw center axis
        Gizmos.color = Color.red;
        Gizmos.DrawLine(mid, mid + rotatedA * maxHeight * globalScale);

        // Draw strings from points
        if (points == null) return;

        Gizmos.color = Color.yellow;
        Vector3 d = hangDirection.normalized;
        
        foreach (var pt in points)
        {
            if (!pt.isAccepted) continue;
            
            float len = CalculateLength(pt, relativeTo);
            if (len <= 0f) continue;
            
            Vector3 end = pt.position + d * len;
            Gizmos.DrawLine(pt.position, end);
            Gizmos.DrawWireSphere(end, 0.02f);
        }
    }
}