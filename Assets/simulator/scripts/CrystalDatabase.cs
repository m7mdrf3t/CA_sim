using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

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

        public CrystalMainTypes crystalMainTypes;
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

        [Tooltip("price of the crystal type")]
        public float price;
        
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
