using System.Collections.Generic;
using UnityEngine;

public static class SelectionBus
{
    public static int    SelectedCrystalIndex   = -1;
    public static string SelectedCrystalName    = null;
    public static Sprite  SelectedCrystalSprite  = null;

    public static int    SelectedCategoryIndex  = -1;
    public static string SelectedCategoryTitle  = null;
    public static Sprite  SelectedCategorySprite = null;
    public static CompositionConfig.CategoryData SelectedCategoryData; // Category data from config
    
    /// <summary>
    /// Safely assign category data without losing values
    /// </summary>
    public static void SetCategoryData(CompositionConfig.CategoryData data)
    {
        SelectedCategoryData = data;
        Debug.Log($"[SelectionBus] Set category data: {data.title}, sprites={data.gallerySprites?.Length ?? 0}");
    }
    public static CompositionCarouselData SelectedCompositionCategory; // Old system support

    // Multiple crystal selections for preview screens
    public static List<int> SelectedCrystalIndices = new List<int>();
    public static List<Sprite> SelectedCrystalSprites = new List<Sprite>();
    public static List<string> SelectedCrystalNames = new List<string>();
    
    // Persistent storage keys
    private const string KEY_SELECTION_COUNT = "SelectedCrystal_Count";
    private const string KEY_SELECTION_INDEX = "SelectedCrystal_Index_";
    private const string KEY_SELECTION_NAME = "SelectedCrystal_Name_";
    
    /// <summary>
    /// Save current selections to persistent storage (PlayerPrefs)
    /// </summary>
    public static void SaveSelections()
    {
        PlayerPrefs.SetInt(KEY_SELECTION_COUNT, SelectedCrystalIndices.Count);
        
        // Save each selection (indices and names, sprites are reference-only so we skip them)
        for (int i = 0; i < SelectedCrystalIndices.Count; i++)
        {
            PlayerPrefs.SetInt(KEY_SELECTION_INDEX + i, SelectedCrystalIndices[i]);
            
            string name = i < SelectedCrystalNames.Count ? SelectedCrystalNames[i] : $"Crystal {i + 1}";
            PlayerPrefs.SetString(KEY_SELECTION_NAME + i, name);
        }
        
        PlayerPrefs.Save();
        Debug.Log($"[SelectionBus] Saved {SelectedCrystalIndices.Count} selections to persistent storage.");
    }
    
    /// <summary>
    /// Load selections from persistent storage
    /// Note: Sprites need to be loaded from resources based on indices
    /// </summary>
    public static void LoadSelections()
    {
        // Clear current selections first
        SelectedCrystalIndices.Clear();
        SelectedCrystalNames.Clear();
        SelectedCrystalSprites.Clear();
        
        int count = PlayerPrefs.GetInt(KEY_SELECTION_COUNT, 0);
        
        if (count == 0)
        {
            Debug.Log("[SelectionBus] No saved selections found.");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            int index = PlayerPrefs.GetInt(KEY_SELECTION_INDEX + i, -1);
            if (index >= 0)
            {
                SelectedCrystalIndices.Add(index);
            }
            
            string name = PlayerPrefs.GetString(KEY_SELECTION_NAME + i, $"Crystal {i + 1}");
            SelectedCrystalNames.Add(name);
            
            // Sprites need to be loaded separately based on index
            // This will be populated when needed
            SelectedCrystalSprites.Add(null);
        }
        
        Debug.Log($"[SelectionBus] Loaded {count} selections from persistent storage.");
    }
    
    /// <summary>
    /// Clear all saved selections (both runtime and persistent)
    /// </summary>
    public static void ClearSelections()
    {
        SelectedCrystalIndices.Clear();
        SelectedCrystalNames.Clear();
        SelectedCrystalSprites.Clear();
        
        PlayerPrefs.DeleteKey(KEY_SELECTION_COUNT);
        for (int i = 0; i < 10; i++) // Clear up to 10 possible entries
        {
            PlayerPrefs.DeleteKey(KEY_SELECTION_INDEX + i);
            PlayerPrefs.DeleteKey(KEY_SELECTION_NAME + i);
        }
        PlayerPrefs.Save();
        
        Debug.Log("[SelectionBus] Cleared all selections.");
    }
}