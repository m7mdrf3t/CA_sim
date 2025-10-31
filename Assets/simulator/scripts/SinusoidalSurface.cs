using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An implementation of HangerSurface that creates two separate intersecting waves
/// to form a chandelier with dramatic peaks and valleys.
/// </summary>
[System.Serializable]
public class SinusoidalSurface : HangerSurface
{
    [Header("Ceiling Configuration")]
    [SerializeField, Tooltip("Ceiling height (Y) where anchors are")]
    private float ceilingHeight = 3.0f;

    [SerializeField, Tooltip("Surface center X,Z position")]
    private Vector2 surfaceCenter = Vector2.zero;

    [Header("Wave 1 Configuration")]
    [SerializeField, Tooltip("Enable first wave")]
    private bool useWave1 = true;

    [SerializeField, Tooltip("Wave 1 amplitude (depth)")]
    private float wave1Amplitude = 2.5f;

    [SerializeField, Tooltip("Wave 1 rotation angle in degrees")]
    private float wave1Angle = 45f;

    [SerializeField, Tooltip("Wave 1 frequency (how many peaks)")]
    private float wave1Frequency = 1.0f;

    [SerializeField, Tooltip("Wave 1 phase offset")]
    private float wave1Phase = 0f;

    [SerializeField, Tooltip("Wave 1 sharpness (1=sine, higher=sharper)")]
    private float wave1Sharpness = 1.0f;

    [Header("Wave 2 Configuration")]
    [SerializeField, Tooltip("Enable second wave")]
    private bool useWave2 = true;

    [SerializeField, Tooltip("Wave 2 amplitude (depth)")]
    private float wave2Amplitude = 2.5f;

    [SerializeField, Tooltip("Wave 2 rotation angle in degrees")]
    private float wave2Angle = -45f;

    [SerializeField, Tooltip("Wave 2 frequency (how many peaks)")]
    private float wave2Frequency = 1.0f;

    [SerializeField, Tooltip("Wave 2 phase offset")]
    private float wave2Phase = 0f;

    [SerializeField, Tooltip("Wave 2 sharpness (1=sine, higher=sharper)")]
    private float wave2Sharpness = 1.0f;

    [Header("Wave Combination")]
    [SerializeField, Tooltip("How to combine waves: Multiply, Add, Min, Max")]
    private WaveCombineMode combineMode = WaveCombineMode.Multiply;

    [SerializeField, Tooltip("Overall depth scale")]
    private float depthScale = 1.0f;

    [SerializeField, Tooltip("Falloff from center (0=no falloff, higher=more)")]
    private float edgeFalloff = 0.5f;

    [SerializeField, Tooltip("Maximum distance for falloff effect")]
    private float falloffRadius = 5.0f;

    [Header("Randomization")]
    [SerializeField, Tooltip("Add random variation to lengths")]
    public bool addRandomization = false;

    [SerializeField, Tooltip("Random variation amount")]
    private float randomVariation = 0.1f;

    public enum WaveCombineMode
    {
        Multiply,
        Add,
        Min,
        Max,
        Average
    }

    /// <summary>
    /// Calculates the hanger length using two intersecting waves.
    /// </summary>
    public override float CalculateLength(PointData point, Transform relativeTo)
    {
        Vector3 anchorPos = point.position;
        
        // Get position relative to surface center
        float x = anchorPos.x - surfaceCenter.x;
        float z = anchorPos.z - surfaceCenter.y;

        // Calculate wave heights
        float wave1Height = 0f;
        float wave2Height = 0f;

        if (useWave1)
        {
            wave1Height = CalculateWaveHeight(x, z, wave1Angle, wave1Frequency, 
                                               wave1Amplitude, wave1Phase, wave1Sharpness);
        }

        if (useWave2)
        {
            wave2Height = CalculateWaveHeight(x, z, wave2Angle, wave2Frequency, 
                                               wave2Amplitude, wave2Phase, wave2Sharpness);
        }

        // Combine the two waves
        float combinedHeight = CombineWaves(wave1Height, wave2Height);

        // Apply depth scale
        combinedHeight *= depthScale;

        // Apply edge falloff
        if (edgeFalloff > 0f && falloffRadius > 0f)
        {
            float distanceFromCenter = Mathf.Sqrt(x * x + z * z);
            float falloffFactor = 1f - Mathf.Pow(Mathf.Clamp01(distanceFromCenter / falloffRadius), edgeFalloff);
            combinedHeight *= falloffFactor;
        }

        // Add randomization if enabled
        float randomOffset = addRandomization ? 
            Random.Range(-randomVariation, randomVariation) : 0f;

        // Calculate final surface height
        float surfaceHeight = ceilingHeight - combinedHeight + randomOffset;

        // Calculate hanger length
        float length = Mathf.Abs(ceilingHeight - surfaceHeight);

        return ApplyHeightOffset(Mathf.Max(0.1f, length));
    }

    /// <summary>
    /// Calculates a single wave height based on rotation and parameters.
    /// </summary>
    private float CalculateWaveHeight(float x, float z, float angleDegrees, 
                                      float frequency, float amplitude, 
                                      float phase, float sharpness)
    {
        // Convert angle to radians
        float angleRad = angleDegrees * Mathf.Deg2Rad;

        // Rotate the coordinate system
        float rotatedX = x * Mathf.Cos(angleRad) - z * Mathf.Sin(angleRad);

        // Calculate wave along the rotated axis
        float waveInput = frequency * rotatedX + phase;
        float waveValue = Mathf.Sin(waveInput);

        // Apply sharpness to create more dramatic peaks/valleys
        if (sharpness != 1.0f)
        {
            float sign = Mathf.Sign(waveValue);
            waveValue = sign * Mathf.Pow(Mathf.Abs(waveValue), 1f / sharpness);
        }

        return amplitude * waveValue;
    }

    /// <summary>
    /// Combines two wave heights based on the selected mode.
    /// </summary>
    private float CombineWaves(float wave1, float wave2)
    {
        if (!useWave1) return wave2;
        if (!useWave2) return wave1;

        switch (combineMode)
        {
            case WaveCombineMode.Multiply:
                // Normalize to -1 to 1 range first, multiply, then scale back
                float norm1 = wave1 / Mathf.Max(0.001f, wave1Amplitude);
                float norm2 = wave2 / Mathf.Max(0.001f, wave2Amplitude);
                return (norm1 * norm2) * Mathf.Max(wave1Amplitude, wave2Amplitude);

            case WaveCombineMode.Add:
                return wave1 + wave2;

            case WaveCombineMode.Min:
                return Mathf.Min(wave1, wave2);

            case WaveCombineMode.Max:
                return Mathf.Max(wave1, wave2);

            case WaveCombineMode.Average:
                return (wave1 + wave2) * 0.5f;

            default:
                return wave1 + wave2;
        }
    }

    /// <summary>
    /// Draws the gizmos for the calculated surface points.
    /// </summary>
    public override void DrawGizmos(IEnumerable<PointData> points, Transform relativeTo)
    {
        if (points == null) return;

        Gizmos.color = Color.cyan;

        foreach (var pt in points)
        {
            Vector3 anchorPos = pt.position;
            float x = anchorPos.x - surfaceCenter.x;
            float z = anchorPos.z - surfaceCenter.y;

            // Calculate without randomization for stable gizmo
            float wave1Height = useWave1 ? 
                CalculateWaveHeight(x, z, wave1Angle, wave1Frequency, wave1Amplitude, wave1Phase, wave1Sharpness) : 0f;
            
            float wave2Height = useWave2 ? 
                CalculateWaveHeight(x, z, wave2Angle, wave2Frequency, wave2Amplitude, wave2Phase, wave2Sharpness) : 0f;

            float combinedHeight = CombineWaves(wave1Height, wave2Height) * depthScale;

            // Apply edge falloff
            if (edgeFalloff > 0f && falloffRadius > 0f)
            {
                float distanceFromCenter = Mathf.Sqrt(x * x + z * z);
                float falloffFactor = 1f - Mathf.Pow(Mathf.Clamp01(distanceFromCenter / falloffRadius), edgeFalloff);
                combinedHeight *= falloffFactor;
            }

            float surfaceHeight = ceilingHeight - combinedHeight;

            // Draw surface point and connection line
            Vector3 surfacePos = new Vector3(pt.position.x, surfaceHeight, pt.position.z);
            
            Gizmos.DrawWireSphere(surfacePos, 0.03f);
            Gizmos.DrawLine(pt.position, surfacePos);
        }

        // Draw wave direction indicators at center
        Vector3 center = new Vector3(surfaceCenter.x, ceilingHeight, surfaceCenter.y);
        
        if (useWave1)
        {
            Gizmos.color = Color.red;
            Vector3 wave1Dir = Quaternion.Euler(0, wave1Angle, 0) * Vector3.forward;
            Gizmos.DrawRay(center, wave1Dir * 0.5f);
        }

        if (useWave2)
        {
            Gizmos.color = Color.blue;
            Vector3 wave2Dir = Quaternion.Euler(0, wave2Angle, 0) * Vector3.forward;
            Gizmos.DrawRay(center, wave2Dir * 0.5f);
        }
    }
}