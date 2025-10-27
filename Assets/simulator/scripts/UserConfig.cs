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
}

// ==================== CRYSTAL DATABASE ====================
/// <summary>
/// Central database holding all available crystal types
/// Create one instance: Assets/Create/Crystal System/Crystal Database
/// </summary>
[CreateAssetMenu(fileName = "CrystalDatabase", menuName = "Crystal System/Crystal Database", order = 0)]
public class CrystalDatabase : ScriptableObject
{
    [System.Serializable]
    public class CrystalCategory
    {
        public string categoryName;
        public CrystalType type;
        
        [Header("Shared Resources (used by all variants)")]
        [Tooltip("Prefab shared by all variants in this category")]
        public GameObject prefab;
        
        [Tooltip("Material shared by all variants (color will be modified per variant)")]
        public Material sharedMaterial;
        
        [Header("Category Defaults")]
        [Tooltip("Default sprite for the category")]
        public Sprite defaultSprite;
        
        [Tooltip("Default video for the category")]
        public VideoClip defaultVideoClip;
        
        [Header("Variants")]
        [Tooltip("Available color variants for this crystal type")]
        public List<CrystalVariant> variants = new List<CrystalVariant>();
    }

    public List<CrystalCategory> categories = new List<CrystalCategory>();

    /// <summary>
    /// Get all variants for a specific crystal type
    /// </summary>
    public List<CrystalVariant> GetVariantsForType(CrystalType type)
    {
        foreach (var category in categories)
        {
            if (category.type == type)
                return category.variants;
        }
        return new List<CrystalVariant>();
    }

    /// <summary>
    /// Get a specific variant by name and type
    /// </summary>
    public CrystalVariant GetVariant(CrystalType type, string variantName)
    {
        var variants = GetVariantsForType(type);
        return variants.Find(v => v.variantName == variantName);
    }

    /// <summary>
    /// Get category for a specific crystal type
    /// </summary>
    public CrystalCategory GetCategory(CrystalType type)
    {
        return categories.Find(c => c.type == type);
    }

    /// <summary>
    /// Get the shared prefab for a crystal type
    /// </summary>
    public GameObject GetPrefab(CrystalType type)
    {
        var category = GetCategory(type);
        return category?.prefab;
    }

    /// <summary>
    /// Get the shared material for a crystal type
    /// </summary>
    public Material GetSharedMaterial(CrystalType type)
    {
        var category = GetCategory(type);
        return category?.sharedMaterial;
    }

    /// <summary>
    /// Get icon for a specific variant (falls back to category default)
    /// </summary>
    public Sprite GetVariantIcon(CrystalType type, int variantIndex)
    {
        var category = GetCategory(type);
        if (category == null) return null;

        if (variantIndex >= 0 && variantIndex < category.variants.Count)
        {
            var variant = category.variants[variantIndex];
            if (variant.icon != null)
                return variant.icon;
        }

        return category.defaultSprite;
    }

    /// <summary>
    /// Get video clip for a specific variant (falls back to category default)
    /// </summary>
    public VideoClip GetVariantVideo(CrystalType type, int variantIndex)
    {
        var category = GetCategory(type);
        if (category == null) return null;

        if (variantIndex >= 0 && variantIndex < category.variants.Count)
        {
            var variant = category.variants[variantIndex];
            if (variant.videoClip != null)
                return variant.videoClip;
        }

        return category.defaultVideoClip;
    }
}

// ==================== USER CONFIG ====================
/// <summary>
/// User's selected configuration - now references the database
/// </summary>
[CreateAssetMenu(fileName = "NewUserConfig", menuName = "Crystal System/User Configuration", order = 1)]
public class UserConfig : ScriptableObject
{
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

    [Header("Crystal Selection")]
    [Tooltip("Reference to the crystal database")]
    public CrystalDatabase crystalDatabase;
    
    [Tooltip("Hanger surface type")]
    [SerializeField]
    public HangerBuilder.SurfaceType hangerSurface = new HangerBuilder.SurfaceType ();

    [Tooltip ("Base shape type")]
    [SerializeField]
    public ShapeType BaseType = ShapeType.Box;

    [Tooltip("Crystal selections (up to 4)")]
    [SerializeField]
    public List<CrystalSelection> crystalSelections = new List<CrystalSelection>();

    [System.Serializable]
    public class CrystalSelection
    {
        public bool enabled = true;
        public CrystalType crystalType = CrystalType.Breeze;
        
        [Tooltip("Index of the color variant to use")]
        public int variantIndex = 0;
        
        [Range(0f, 1f)]
        [Tooltip("Weight/probability of this crystal appearing (0-1)")]
        public float spawnWeight = 1f;

        public CrystalSelection()
        {
            enabled = true;
            crystalType = CrystalType.Breeze;
            variantIndex = 0;
            spawnWeight = 1f;
        }
    }

    [Header("Additional Settings")]
    [Tooltip("Custom name for this configuration")]
    public string configurationName = "Default Box";
    
    [Tooltip("Density of crystals per square meter")]
    [Range(1, 100)]
    public int crystalsPerSquareMeter = 16;

    // ==================== HELPER METHODS ====================
    
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
            variantName = variant?.variantName ?? "Unknown"
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
        if (crystalSelections.Count < 4)
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

// ==================== CRYSTAL TYPE ENUM ====================
public enum CrystalType
{
    Breeze,
    Refraction,
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
    WhireWind

}