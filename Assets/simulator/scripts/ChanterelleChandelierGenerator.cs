using UnityEngine;
using System.Collections.Generic;

public class ChanterelleChandelierGenerator : MonoBehaviour
{
    [Header("Chandelier Dimensions")]
    [Range(1f, 10f)] public float topRadius = 5f;
    [Range(0.1f, 2f)] public float bottomIndentationRadius = 0.5f;
    [Range(1f, 15f)] public float totalHeight = 8f;
    [Range(0.1f, 5f)] public float indentationDepth = 2f; // Y_base - Y_min

    [Header("Spore Properties (using Crystal Prefab)")]
    public GameObject crystalSporePrefab; // Assign your crystal prefab here
    [Range(100, 50000)] public int numberOfSpores = 5000; // Lower count due to GameObject overhead
    [Range(0.01f, 0.5f)] public float minCrystalScale = 0.05f;
    [Range(0.01f, 0.5f)] public float maxCrystalScale = 0.2f;
    [Range(0.1f, 5f)] public float sporeDensityPower = 2f; // Higher = more dense at bottom-center

    [Header("Thread Properties (using Cylinder Prefab)")]
    public GameObject cylinderThreadPrefab; // Assign a thin cylinder prefab here
    [Range(100, 2000)] public int numberOfThreads = 1000; // Keep reasonable for GameObject overhead
    [Range(0.5f, 5f)] public float threadExtensionAboveCloud = 2f; // How far threads go above the total height
    [Range(0.1f, 1f)] public float threadTerminationRatio = 0.5f; // Where threads end as a ratio of totalHeight (e.g., 0.5 = middle)
    [Range(0.1f, 10f)] public float threadPlacementRadius = 4.5f; // Radius where threads start at the top

    // Internal constants for clarity
    private float Y_min => 0f; // The lowest point of the indentation
    private float Y_base => indentationDepth; // The height where the central indentation starts to widen

    // Parent transforms to keep the Hierarchy clean
    private Transform _sporesParent;
    private Transform _threadsParent;

    void OnValidate()
    {
        // Ensure values are logical, e.g., threadRadius <= topRadius
        threadPlacementRadius = Mathf.Min(threadPlacementRadius, topRadius);
        if (bottomIndentationRadius >= topRadius) bottomIndentationRadius = topRadius * 0.5f;
        if (indentationDepth >= totalHeight) indentationDepth = totalHeight * 0.8f;
    }

    void Start()
    {
        GenerateChandelier();
    }

    [ContextMenu("Generate Chandelier")] // Add a right-click option in editor
    void GenerateChandelier()
    {
        // Clear previous generation
        if (_sporesParent != null) DestroyImmediate(_sporesParent.gameObject);
        if (_threadsParent != null) DestroyImmediate(_threadsParent.gameObject);

        _sporesParent = new GameObject("Spores_Container").transform;
        _sporesParent.SetParent(this.transform);

        _threadsParent = new GameObject("Threads_Container").transform;
        _threadsParent.SetParent(this.transform);

        // --- Generate Spores (Crystal Prefabs) ---
        if (crystalSporePrefab == null)
        {
            Debug.LogError("Crystal Spore Prefab is not assigned! Cannot generate spores.");
        }
        else
        {
            for (int i = 0; i < numberOfSpores; i++)
            {
                Vector3 sporePos = GetRandomSporePosition();
                if (sporePos != Vector3.zero) // Check if a valid position was found
                {
                    GameObject spore = Instantiate(crystalSporePrefab, sporePos, Random.rotation, _sporesParent);
                    float randomScale = Random.Range(minCrystalScale, maxCrystalScale);
                    spore.transform.localScale = Vector3.one * randomScale;
                }
            }
        }

        // --- Generate Threads (Cylinder Prefabs) ---
        if (cylinderThreadPrefab == null)
        {
            Debug.LogError("Cylinder Thread Prefab is not assigned! Cannot generate threads.");
        }
        else
        {
            for (int i = 0; i < numberOfThreads; i++)
            {
                float angle = Random.Range(0f, 360f);
                float currentRadius = Random.Range(0f, threadPlacementRadius); // Distribute threads within a radius
                float x = Mathf.Cos(angle * Mathf.Deg2Rad) * currentRadius;
                float z = Mathf.Sin(angle * Mathf.Deg2Rad) * currentRadius;

                Vector3 startPoint = new Vector3(x, totalHeight + threadExtensionAboveCloud, z);
                // End point slightly varied within the upper part of the spore cloud
                Vector3 endPoint = new Vector3(x, totalHeight * threadTerminationRatio + Random.Range(-totalHeight * 0.1f, totalHeight * 0.1f), z);

                // Calculate the position and rotation for the cylinder prefab
                Vector3 threadPosition = (startPoint + endPoint) / 2f;
                Vector3 threadDirection = (endPoint - startPoint).normalized;
                float threadLength = Vector3.Distance(startPoint, endPoint);

                // Instantiate and adjust
                GameObject thread = Instantiate(cylinderThreadPrefab, threadPosition, Quaternion.identity, _threadsParent);
                thread.transform.up = threadDirection; // Orient the cylinder (assuming default cylinder 'up' is Y-axis)
                // Assuming your cylinder prefab has a default height of 1 unit. Adjust scale.y
                thread.transform.localScale = new Vector3(thread.transform.localScale.x, threadLength / 2f, thread.transform.localScale.z);
                // Note: You might need to adjust initial scale.x and scale.z for your specific cylinder prefab
                // For example, if your prefab is a default Unity cylinder, its radius might be 0.5, so scale.x and scale.z would need to be very small, e.g., 0.05.
                thread.transform.localScale = new Vector3(0.05f, threadLength / 2f, 0.05f); // Example for a thin thread
            }
        }
    }

    Vector3 GetRandomSporePosition()
    {
        for (int i = 0; i < 50; i++) // Try a few times to find a valid position
        {
            float randX = Random.Range(-topRadius, topRadius);
            float randZ = Random.Range(-topRadius, topRadius);
            float distToCenterSq = randX * randX + randZ * randZ;
            float distToCenter = Mathf.Sqrt(distToCenterSq);

            if (distToCenter > topRadius) continue; // Outside the main circular footprint

            float y_lower_bound_at_xz;
            if (distToCenter <= bottomIndentationRadius)
            {
                float b_inner = indentationDepth / (bottomIndentationRadius * bottomIndentationRadius);
                y_lower_bound_at_xz = b_inner * distToCenterSq + Y_min;
            }
            else
            {
                y_lower_bound_at_xz = Y_base;
            }

            float a_outer = totalHeight / (topRadius * topRadius);
            float y_upper_bound_at_xz = totalHeight - a_outer * distToCenterSq;

            y_lower_bound_at_xz = Mathf.Min(y_lower_bound_at_xz, y_upper_bound_at_xz);

            float randY = Random.Range(y_lower_bound_at_xz, y_upper_bound_at_xz);

            float verticalDensityFactor = Mathf.Pow(1f - (randY - Y_min) / (totalHeight - Y_min), sporeDensityPower);
            float horizontalDensityFactor = Mathf.Pow(1f - (distToCenter / topRadius), sporeDensityPower);
            float overallDensityFactor = (verticalDensityFactor + horizontalDensityFactor) / 2f;

            if (Random.value < overallDensityFactor)
            {
                return new Vector3(randX, randY, randZ);
            }
        }
        return Vector3.zero;
    }
}