using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

// ==================== CRYSTAL DATA ====================
/// <summary>
/// Defines a single crystal variant with its color, icon, and video
/// Prefab and material are inherited from the parent category
/// </summary>
[System.Serializable]
public class CrystalVariant
{
    public string variantName;
    public Color color = Color.white;
    
    [Tooltip("Icon/sprite for this variant (for UI display)")]
    public Sprite icon;
    
    [Tooltip("Video clip for this variant (for previews/demos)")]
    public VideoClip videoClip;

    public float price;
}

public enum DensityLevel { Low, Medium, High } // fixed spelling

public enum colorStyle { Coctail, fade }

// ==================== USER CONFIG ====================
/// <summary>
/// User's selected configuration - now references the database
/// </summary>
[CreateAssetMenu(fileName = "NewUserConfig", menuName = "Crystal System/User Configuration", order = 1)]
public class UserConfig : ScriptableObject
{
    public string clientName;
    public string clientEmail;

    [Header("Box Dimensions")]
    [Tooltip("Box width in meters")]
    [Min(0.01f)]
    public float xSize = 1f;

    [Tooltip("Box height in meters")]
    [Min(0.01f)]
    public float ySize = 1f;
    
    [Tooltip("Box depth in meters")]
    [Min(0.01f)]
    public float zSize = 1f;

    [Tooltip("Area in square meters")]
    [Min(0.01f)]
    public float area = 1f;

    public float BeadsHight = 1f;

    [Tooltip("Total Spots")]
    [Min(0.01f)]
    public float totalSpots = 1f;

    [Tooltip("Total Points")]
    [Min(0.01f)]
    public float totalPoints = 1f;

    public float finalPrice;

    [Header("Densities")]
    [Tooltip("Density level")]
    public DensityLevel densityLevel = DensityLevel.High;   

    public colorStyle colorStyle = colorStyle.Coctail;

    [Tooltip("Density percentage or custom value")]
    [Min(0.01f)]
    public float highDensity = 1f, mediumDensity = 1f, lowDensity = 1f , density = 1f;

    [Header("Visualization Settings")]
    [Tooltip("Show beads in the simulator")]
    public bool showBeads = true;
    
    public float beadCost;
    public bool WithBase = true;
    public float mirrorCost;


    [Header("Crystal Selection")]
    [Tooltip("Reference to the crystal database")]
    public CrystalDatabase crystalDatabase;

    public int GallaryIndex = 0;
    
    [Tooltip("Hanger surface type")]
    [SerializeField]
    public SurfaceType hangerSurface = new SurfaceType ();

    [Tooltip ("Base shape type")]
    [SerializeField]
    public ShapeType BaseType = ShapeType.Box;

    [Tooltip("Crystal selections (up to 4)")]
    [SerializeField]
    public List<CrystalSelection> crystalSelections = new List<CrystalSelection>();

    [System.Serializable]
    public class CrystalSelection
    {
        [Tooltip("Main Type")]
        public CrystalMainTypes mainTypes = CrystalMainTypes.Refraction;
 
        public bool enabled = true;
        public CrystalType crystalType = CrystalType.Galaxy;
        
        [Tooltip("Index of the color variant to use")]
        public int variantIndex = 0;
        
        [Range(0f, 1f)]
        [Tooltip("Weight/probability of this crystal appearing (0-1)")]
        public float spawnWeight = 1f;

        [Tooltip("Spawn ratio (0-100) for this crystal type")]
        [Range(0f, 1f)]
        public float spawnRatio = 0f;

        [Tooltip("Price for this crystal type")]
        public float price = 0f;

        public CrystalSelection()
        {
            enabled = true;
            mainTypes = CrystalMainTypes.Refraction; 
            crystalType = CrystalType.Galaxy;
            variantIndex = 0;
            spawnWeight = 1f;
            spawnRatio = 0f;
            price = 0f;

        }
    }

    [Header("Additional Settings")]
    [Tooltip("Custom name for this configuration")]
    public string configurationName = "Default Box";
    
    [Tooltip("Density of crystals per square meter")]
    [Range(1, 100)]
    public int crystalsPerSquareMeter = 16;

    /// <summary>
    /// Get all enabled crystal selections
    /// </summary>
    public List<CrystalSelection> GetEnabledSelections()
    {
        return crystalSelections.FindAll(s => s.enabled);
    }

    /// <summary>
    /// Get a specific crystal variant by selection
    /// </summary>
    public CrystalVariant GetVariantForSelection(CrystalSelection selection)
    {
        if (crystalDatabase == null || selection == null) return null;

        var variants = crystalDatabase.GetVariantsForType(selection.crystalType);
        if (variants == null || variants.Count == 0) return null;

        int index = Mathf.Clamp(selection.variantIndex, 0, variants.Count - 1);
        return variants[index];
    }

    private void Start() {
    
        Debug.Log("I CAN LOG!");
    }
    

    /// <summary>
    /// Get prefab for a specific selection (from category)
    /// </summary>
    public GameObject GetPrefabForSelection(CrystalSelection selection)
    {
        if (crystalDatabase == null || selection == null) return null;
        return crystalDatabase.GetPrefab(selection.crystalType);
    }

    /// <summary>
    /// Get material for a specific selection (from category)
    /// </summary>
    public Material GetMaterialForSelection(CrystalSelection selection)
    {
        if (crystalDatabase == null || selection == null) return null;
        return crystalDatabase.GetSharedMaterial(selection.crystalType);
    }

    /// <summary>
    /// Get icon for a specific selection
    /// </summary>
    public Sprite GetIconForSelection(CrystalSelection selection)
    {
        if (crystalDatabase == null || selection == null) return null;
        return crystalDatabase.GetVariantIcon(selection.crystalType, selection.variantIndex);
    }

    /// <summary>
    /// Get video clip for a specific selection
    /// </summary>
    public VideoClip GetVideoForSelection(CrystalSelection selection)
    {
        if (crystalDatabase == null || selection == null) return null;
        return crystalDatabase.GetVariantVideo(selection.crystalType, selection.variantIndex);
    }

    /// <summary>
    /// Get color for a specific selection
    /// </summary>
    public Color GetColorForSelection(CrystalSelection selection)
    {
        var variant = GetVariantForSelection(selection);
        return variant != null ? variant.color : Color.white;
    }

    /// <summary>
    /// Data structure containing all info needed to spawn a crystal
    /// </summary>
    public struct CrystalSpawnData
    {
        public GameObject prefab;
        public Material material;
        public Color color;
        public Sprite icon;
        public VideoClip videoClip;
        public CrystalType crystalType;
        public string variantName;
        public float price;
    }

    /// <summary>
    /// Get complete spawn data for a selection
    /// </summary>
    public CrystalSpawnData GetSpawnDataForSelection(CrystalSelection selection)
    {
        var variant = GetVariantForSelection(selection);
        
        return new CrystalSpawnData
        {
            prefab = GetPrefabForSelection(selection),
            material = GetMaterialForSelection(selection),
            color = variant?.color ?? Color.white,
            icon = GetIconForSelection(selection),
            videoClip = GetVideoForSelection(selection),
            crystalType = selection.crystalType,
            variantName = variant?.variantName ?? "Unknown",
            price = selection.price,
        };
    }

    /// <summary>
    /// Get a random crystal variant based on spawn weights
    /// </summary>
    public CrystalVariant GetRandomWeightedVariant()
    {
        var enabled = GetEnabledSelections();
        if (enabled.Count == 0) return null;

        Debug.Log($"[UserConfig] Selecting from {enabled.Count} enabled selections.");
        
        // Calculate total weight
        float totalWeight = 0f;
        foreach (var sel in enabled)
        {
            totalWeight += sel.spawnWeight;
        }

        if (totalWeight <= 0f) return null;

        // Random selection
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var sel in enabled)
        {
            currentWeight += sel.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return GetVariantForSelection(sel);
            }
        }

        // Fallback to first enabled
        return GetVariantForSelection(enabled[0]);
    }

    /// <summary>
    /// Get random crystal selection based on spawn weights
    /// </summary>
    public CrystalSelection GetRandomWeightedSelection()
    {
        var enabled = GetEnabledSelections();
        if (enabled.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var sel in enabled)
        {
            totalWeight += sel.spawnWeight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var sel in enabled)
        {
            currentWeight += sel.spawnWeight;
            if (randomValue <= currentWeight)
            {
                return sel;
            }
        }

        return enabled[0];
    }

    /// <summary>
    /// Get random spawn data based on spawn weights
    /// </summary>
    public CrystalSpawnData GetRandomWeightedSpawnData()
    {
        var selection = GetRandomWeightedSelection();
        if (selection == null)
        {
            return new CrystalSpawnData(); // Return default/empty
        }

        return GetSpawnDataForSelection(selection);
    }

    public float GetTotalPrice()
    {
        float total = 0f;
        foreach (var sel in crystalSelections)
        {
            float selPointsSum = (sel.spawnRatio/100f) * sel.price * totalPoints;
            total += selPointsSum;
        }

        if (showBeads)
        {
            float beadsCost = totalPoints * 7 * 5 * area * BeadsHight;
            Debug.Log($"[UserConfig] Adding bead cost {beadsCost} for {totalPoints} points and {area} m2 area.");
            total += beadsCost;
        }

        if(densityLevel == DensityLevel.High)
        {
            return total ;
        }
        else if(densityLevel == DensityLevel.Medium)
        {
            return total * 0.75f;
        }
        else
        {
            return total * 0.50f;
        }
    }

    public List<CrystalSpawnData> GetAllCrystalVariantData()
    {
        var Enabled = GetEnabledSelections();

        List<CrystalSpawnData> allData = new List<CrystalSpawnData>();

        foreach (var sel in Enabled)
        {
            var spawnData = GetSpawnDataForSelection(sel);
            if(spawnData.prefab != null)
            {
                allData.Add(spawnData);
            }
        }
        return allData;

    }

    /// <summary>
    /// Add a new crystal selection (max 4)
    /// </summary>
    public void AddCrystalSelection()
    {
        if (crystalSelections.Count < 5)
        {
            crystalSelections.Add(new CrystalSelection());
        }
    }

    /// <summary>
    /// Remove a crystal selection at index
    /// </summary>
    public void RemoveCrystalSelection(int index)
    {
        if (index >= 0 && index < crystalSelections.Count)
        {
            crystalSelections.RemoveAt(index);
        }
    }

    /// <summary>
    /// Initialize with default selection if empty
    /// </summary>
    public void EnsureMinimumSelection()
    {
        if (crystalSelections.Count == 0)
        {
            crystalSelections.Add(new CrystalSelection());
        }
    }

    /// <summary>
    /// Validates the configuration data
    /// </summary>
    public bool IsValid()
    {
        if (xSize <= 0 || ySize <= 0 || zSize <= 0)
        {
            Debug.LogError($"UserConfig '{name}': Invalid dimensions. All sizes must be greater than 0.");
            return false;
        }

        if (crystalDatabase == null)
        {
            Debug.LogError($"UserConfig '{name}': No crystal database assigned.");
            return false;
        }

        var enabledSelections = GetEnabledSelections();
        if (enabledSelections.Count == 0)
        {
            Debug.LogWarning($"UserConfig '{name}': No crystal selections enabled.");
            return false;
        }

        // Check if at least one selection has a valid prefab
        bool hasValidPrefab = false;
        foreach (var sel in enabledSelections)
        {
            var prefab = GetPrefabForSelection(sel);
            if (prefab != null)
            {
                hasValidPrefab = true;
                break;
            }
        }

        if (!hasValidPrefab)
        {
            Debug.LogWarning($"UserConfig '{name}': No valid crystal prefabs found in selections.");
            return false;
        }

        return true;
    }

// <summary>
/// Save spawn ratios from slider manager - SIMPLIFIED VERSION
/// Matches by variant name directly
/// </summary>
    public void SaveSpawnRatios(Dictionary<string, float> ratios)
{
    Debug.Log($"[UserConfig] SaveSpawnRatios called with {ratios.Count} ratios");
    Debug.Log($"[UserConfig] Available keys: {string.Join(", ", ratios.Keys)}");
    
    int matchCount = 0;
    
    for (int i = 0; i < crystalSelections.Count; i++)
    {
        var selection = crystalSelections[i];
        
        // Get the variant name for this selection
        var variant = GetVariantForSelection(selection);
        string variantName = variant != null ? variant.variantName : selection.crystalType.ToString();
        
        Debug.Log($"[UserConfig] Selection {i}: Looking for key '{variantName}'");
        
        // Try to match by variant name
        if (ratios.ContainsKey(variantName))
        {
            float oldRatio = selection.spawnRatio;
            selection.spawnRatio = ratios[variantName];
            selection.spawnWeight = ratios[variantName] / 100f;
            matchCount++;
            Debug.Log($"[UserConfig] ✓ Matched! Updated {variantName}: {oldRatio:F2} -> {selection.spawnRatio:F2}");
        }
        else
        {
            Debug.LogWarning($"[UserConfig] ✗ No match for '{variantName}'");
        }
    }

    #if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.AssetDatabase.SaveAssets();
    UnityEditor.AssetDatabase.Refresh();
    Debug.Log($"[UserConfig] ✓ Asset marked dirty and saved to disk");
    #endif

    Debug.Log($"[UserConfig] ===== SAVE COMPLETE: {matchCount}/{crystalSelections.Count} ratios updated =====");
}

    /// <summary>
    /// Get spawn ratios as dictionary - SIMPLIFIED VERSION
    /// </summary>
    public Dictionary<string, float> GetSpawnRatios()
{
    Dictionary<string, float> ratios = new Dictionary<string, float>();
    
    foreach (var selection in crystalSelections)
    {
        var variant = GetVariantForSelection(selection);
        string variantName = variant != null ? variant.variantName : selection.crystalType.ToString();
        ratios[variantName] = selection.spawnRatio;
    }
    
    Debug.Log($"[UserConfig] GetSpawnRatios returning {ratios.Count} ratios");
    return ratios;
}

    /// <summary>
    /// Alternative: Save by index (if variant names don't match)
    /// Use this if the simplified version above doesn't work
    /// </summary>
    public void SaveSpawnRatiosByIndex(List<float> ratios)
{
    Debug.Log($"[UserConfig] SaveSpawnRatiosByIndex called with {ratios.Count} ratios");
    
    int count = Mathf.Min(ratios.Count, crystalSelections.Count);
    
    for (int i = 0; i < count; i++)
    {
        float oldRatio = crystalSelections[i].spawnRatio;
        crystalSelections[i].spawnRatio = ratios[i];
        crystalSelections[i].spawnWeight = ratios[i] / 100f;
        Debug.Log($"[UserConfig] Updated selection {i}: {oldRatio:F2} -> {ratios[i]:F2}");
    }

    #if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.AssetDatabase.SaveAssets();
    UnityEditor.AssetDatabase.Refresh();
    #endif

    Debug.Log($"[UserConfig] Saved {count} ratios by index");
}

    /// <summary>
    /// Returns a summary of this configuration
    /// </summary>
    public string GetSummary()
    {
        var enabledSelections = GetEnabledSelections();
        string crystalInfo = enabledSelections.Count > 0 
            ? $"{enabledSelections.Count} crystal type(s)" 
            : "None";
        
        return $"{configurationName}: {xSize}m × {ySize}m × {zSize}m, {crystalInfo}, " +
               $"~{Mathf.RoundToInt(xSize * zSize * crystalsPerSquareMeter)} crystals";
    }

    private void OnValidate()
    {
        xSize = Mathf.Max(0.01f, xSize);
        ySize = Mathf.Max(0.01f, ySize);
        zSize = Mathf.Max(0.01f, zSize);
        crystalsPerSquareMeter = Mathf.Max(1, crystalsPerSquareMeter);

        // Ensure at least one selection exists
        EnsureMinimumSelection();

        // Limit to max 4 selections
        while (crystalSelections.Count > 4)
        {
            crystalSelections.RemoveAt(crystalSelections.Count - 1);
        }

        // Normalize weights if needed
        float totalWeight = 0f;
        foreach (var sel in crystalSelections)
        {
            totalWeight += sel.spawnWeight;
        }
        
        // Warn if all weights are 0
        if (totalWeight <= 0.001f && crystalSelections.Count > 0)
        {
            Debug.LogWarning($"UserConfig '{name}': All spawn weights are 0. Setting first selection to 1.");
            if (crystalSelections.Count > 0)
            {
                crystalSelections[0].spawnWeight = 1f;
            }
        }
    }
}
public enum CrystalType
{
    Custom,
    Aurora_panel,
    Galaxy,
    Aurora_frame,
    Aurora_piller,
    AzureShell,
    Crystal_Bloom,
    Cystal_pumpkin,
    Crystal_starfish,
    Crystal_Angle,
    Dove_feather,
    FaceDrop,
    Galaxy_stone,
    North_start,
    Rock_crystal,
    WhireWind,
    Crystel_Rosset,
    springCanopy,
    wingpopen,
    wave,
    koifish,
    heritage_drop,
    heritage_spher,
    desert_lily,
    crystal_bubble,
    crystal_butterfly,
    wingOpen,
    wingClose


}

public enum CrystalMainTypes
{
    Breeze,
    Refraction,
}