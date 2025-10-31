using UnityEngine;
using System.Collections;


public class LightLayoutCalculator : MonoBehaviour
{

    [Header("Inputs (Green Section)")]
    public float length = 2.45f;   // K10
    public float width = 0.9f;     // L10

    [Header("Constants (Red Section)")]
    public float baseArea = 1f;        // D10
    public float baseSpots = 14f;      // F10
    public float basePoints = 120f;    // G10

    [Header("Results (Blue Section)")]
    private float area;          // M10
    private float totalSpots;    // P10
    private float totalPoints;   // Q10
    private float density;       // R10 (percentage or custom)

    void Start()
    {
        CalculateLayout();
    }

    [ContextMenu("Recalculate Layout")]
    public void CalculateLayout()
    {
        // Calculate area
        area = length * width;

        // Calculate total spots and points
        totalSpots = (baseSpots * area) / baseArea;
        totalPoints = (basePoints * area) / baseArea;

        // Calculate density (example formula — adjust as needed)
        density = (totalPoints / (basePoints * area)) * 100f;

        // Calculate high, medium, and low densities
        float highDensity = totalPoints;
        float mediumDensity = totalPoints * 0.75f;
        float lowDensity = totalPoints * 0.375f;

        // Print results
        Debug.Log($"Area: {area:F2} m²");
        Debug.Log($"Total Spots: {totalSpots:F0}");
        Debug.Log($"Total Points: {totalPoints:F0}");
        Debug.Log($"Density: {density:F2}%");

    }
}