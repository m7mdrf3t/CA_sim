using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;

/// <summary>
/// Automatically initializes 8-composition 2 screen with selected category data
/// Supports BOTH old (CompositionLevel2Initializer) and new (CompositionGalleryPopulator) systems
/// </summary>
public class MiniiHeroImageCreation : MonoBehaviour
{
    [Header("Hero Image (placeholder from carousel)")]
    [SerializeField] private Image heroImage;
    [SerializeField] private CrystalDataManager crystalDataManager;
    [SerializeField] private UserConfig config;

    private CompositionConfig.CategoryData currentCategoryData;
    private string lastInitializedTitle = "";
    
    private void OnEnable()
    {
        // Prevent double initialization for SAME category
        if (!string.IsNullOrEmpty(lastInitializedTitle) && lastInitializedTitle == SelectionBus.SelectedCategoryTitle)
        {
            return;
        }
        
        // Initialize with the selected category data
        if ((SelectionBus.SelectedCategoryData.gallerySprites != null && SelectionBus.SelectedCategoryData.gallerySprites.Length > 0) || 
            !string.IsNullOrEmpty(SelectionBus.SelectedCategoryData.title))
        {
            lastInitializedTitle = SelectionBus.SelectedCategoryTitle;
            InitializeScreen(SelectionBus.SelectedCategoryData);
        }
        else
        {
            Debug.LogError($"No category data found! title={SelectionBus.SelectedCategoryTitle}, sprites={SelectionBus.SelectedCategoryData.gallerySprites?.Length ?? -1}");
        }
    }
    
    private void Start() {

        if ((SelectionBus.SelectedCategoryData.gallerySprites != null && SelectionBus.SelectedCategoryData.gallerySprites.Length > 0) || 
            !string.IsNullOrEmpty(SelectionBus.SelectedCategoryData.title))
        {
            lastInitializedTitle = SelectionBus.SelectedCategoryTitle;
            InitializeScreen(SelectionBus.SelectedCategoryData);
        }
        else
        {
            Debug.LogError($"No category data found! title={SelectionBus.SelectedCategoryTitle}, sprites={SelectionBus.SelectedCategoryData.gallerySprites?.Length ?? -1}");
        }
    }

    private void InitializeScreen(CompositionConfig.CategoryData data)
    {
        int GallaryIndex = config.GallaryIndex;

        Debug.Log($"[CompositionCategoryAutoInitializer] Initializing hero image for category: {data.title}, gallery index: {GallaryIndex}");
        
        // Store current category data for navigation
        currentCategoryData = data;
        
        // Set hero image to placeholder initially
        if (heroImage != null )
        {
            heroImage.sprite = data.gallerySprites[GallaryIndex];
        }
        else if (heroImage != null)
        {
            Debug.LogWarning("[CompositionCategoryAutoInitializer] Hero Image reference is assigned but no placeholder!");
        }
            
    }

}

