using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master controller that orchestrates the entire generation process.
/// 1. Configures modules.
/// 2. Calls PointGenerator to calculate point data.
/// 3. (Optional) Tells PointGenerator to spawn its debug prefabs.
/// 4. (Optional) Passes point data to HangerBuilder to create hangers.
/// </summary>
public class DistributionSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField]
    private UserConfig configuration; // Your existing UserConfig asset

    [Header("Modules")]
    [SerializeField, Tooltip("The component responsible for calculating point positions.")]
    private PointGenerator pointGenerator;

    [SerializeField, Tooltip("The component responsible for building hangers (wires/prefabs) at points.")]
    private HangerBuilder hangerBuilder;

    [Header("Build Settings")]
    [SerializeField, Tooltip("Spawn the debug prefabs from the PointGenerator?")]
    private bool generateDebugPoints = true;
    
    [SerializeField, Tooltip("Build the final wires and prefabs using the HangerBuilder?")]
    private bool buildHangers = true;

    [ContextMenu("Build Full System")]
    public void BuildSystem()
    {
        if (!ValidateModules()) return;

        // 1. Clear previous build
        pointGenerator.ClearAllPoints();
        hangerBuilder.ClearAllHangers();

        // 2. Pass configuration to modules
        // This keeps your UserConfig logic intact
        pointGenerator.ApplyConfiguration(configuration); 
        hangerBuilder.config = configuration;

        // 3. Generate the point data
        // This is the core data that connects the two systems
        List<PointData> points = pointGenerator.GeneratePointsData();

        // 4. (Optional) Spawn debug points
        if (generateDebugPoints)
        {
            pointGenerator.SpawnPointPrefabs(points);
        }

        // 5. (Optional) Build final hangers
        if (buildHangers)
        {
            hangerBuilder.BuildHangers(points);
        }

        Debug.Log($"<b>[DistributionSystem]</b> Build Complete. Generated {points.Count} points.");
    }

    [ContextMenu("Clear Full System")]
    public void ClearSystem()
    {
        if (pointGenerator) pointGenerator.ClearAllPoints();
        if (hangerBuilder) hangerBuilder.ClearAllHangers();
        Debug.Log("<b>[DistributionSystem]</b> Cleared all generated objects.");
    }

    private bool ValidateModules()
    {
        if (pointGenerator == null)
        {
            Debug.LogError("<b>[DistributionSystem]</b> 'PointGenerator' is not assigned!", this);
            return false;
        }
        if (hangerBuilder == null)
        {
            Debug.LogError("<b>[DistributionSystem]</b> 'HangerBuilder' is not assigned!", this);
            return false;
        }
        return true;
    }

    private void OnValidate()
    {
        if (pointGenerator == null) pointGenerator = GetComponent<PointGenerator>();
        if (hangerBuilder == null) hangerBuilder = GetComponent<HangerBuilder>();
    }
}