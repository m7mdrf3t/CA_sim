using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates the gallery grid in 8-composition 2 with images from the selected category
/// </summary>
public class CompositionGalleryPopulator : MonoBehaviour
{
    [Header("Hero Image")]
    [SerializeField] private Image heroImage; // The big image at the top
    
    [Header("Gallery Grid")]
    [SerializeField] private Transform galleryGridParent; // Where to instantiate gallery items
    [SerializeField] private GameObject galleryItemPrefab; // Prefab to instantiate for each image
    [SerializeField] private int columns = 3; // Number of columns in the grid
    
    private CompositionCarouselData currentCategory;
    
    /// <summary>
    /// Call this from CarouselCategoryRouter when navigating to 8-composition 2
    /// </summary>
    public void PopulateGallery(CompositionCarouselData categoryData)
    {
        currentCategory = categoryData;
        
        if (categoryData == null)
        {
            Debug.LogError("[CompositionGalleryPopulator] No category data provided!");
            return;
        }
        
        Debug.Log($"[CompositionGalleryPopulator] Populating gallery with category: {categoryData.displayTitle}");
        
        // Set hero image
        if (heroImage != null && categoryData.placeholderImage != null)
        {
            heroImage.sprite = categoryData.placeholderImage;
            Debug.Log($"[CompositionGalleryPopulator] Hero image set to: {categoryData.placeholderImage.name}");
        }
        else if (heroImage != null)
        {
            Debug.LogWarning("[CompositionGalleryPopulator] Hero image not set - no sprite available");
        }
        
        // Populate gallery grid - use gallerySprites if available (simpler!)
        if (galleryGridParent != null && galleryItemPrefab != null)
        {
            ClearGallery();
            
            // Try simple gallerySprites first
            if (categoryData.gallerySprites != null && categoryData.gallerySprites.Length > 0)
            {
                for (int i = 0; i < categoryData.gallerySprites.Length; i++)
                {
                    CreateSimpleGalleryItem(categoryData.gallerySprites[i], i);
                }
                Debug.Log($"[CompositionGalleryPopulator] Created {categoryData.gallerySprites.Length} gallery items from gallerySprites");
            }
            // Fallback to galleryItems
            else if (categoryData.galleryItems != null && categoryData.galleryItems.Length > 0)
            {
                for (int i = 0; i < categoryData.galleryItems.Length; i++)
                {
                    CreateGalleryItem(categoryData.galleryItems[i], i);
                }
                Debug.Log($"[CompositionGalleryPopulator] Created {categoryData.galleryItems.Length} gallery items");
            }
            else
            {
                Debug.LogWarning("[CompositionGalleryPopulator] No gallery items in category data!");
            }
        }
        else
        {
            Debug.LogWarning("[CompositionGalleryPopulator] Gallery grid parent or prefab not assigned!");
        }
    }
    
    /// <summary>
    /// Called automatically when screen enables if SelectionBus has data
    /// </summary>
    private void OnEnable()
    {
        // If we already have the category from previous call, don't repopulate
        if (currentCategory == null && SelectionBus.SelectedCompositionCategory != null)
        {
            PopulateGallery(SelectionBus.SelectedCompositionCategory);
        }
        else if (currentCategory == null && SelectionBus.SelectedCategoryData.gallerySprites != null)
        {
            // Support new config system
            PopulateGalleryFromConfigData(SelectionBus.SelectedCategoryData);
        }
    }
    
    private void PopulateGalleryFromConfigData(CompositionConfig.CategoryData data)
    {
        if (heroImage != null && data.placeholder != null)
        {
            heroImage.sprite = data.placeholder;
        }
        
        if (galleryGridParent != null && galleryItemPrefab != null)
        {
            ClearGallery();
            
            for (int i = 0; i < data.gallerySprites.Length; i++)
            {
                CreateSimpleGalleryItem(data.gallerySprites[i], i);
            }
        }
    }
    
    private void CreateSimpleGalleryItem(Sprite sprite, int index)
    {
        GameObject itemObj = Instantiate(galleryItemPrefab, galleryGridParent);
        Image img = itemObj.GetComponentInChildren<Image>(true);
        if (img != null && sprite != null)
        {
            img.sprite = sprite;
        }
    }
    
    private void ClearGallery()
    {
        if (galleryGridParent == null) return;
        
        for (int i = galleryGridParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(galleryGridParent.GetChild(i).gameObject);
        }
    }
    
    private void CreateGalleryItem(CompositionCarouselData.GalleryItem item, int index)
    {
        GameObject itemObj = Instantiate(galleryItemPrefab, galleryGridParent);
        
        // Find Image component and set sprite
        Image img = itemObj.GetComponentInChildren<Image>(true);
        if (img != null && item.imageSprite != null)
        {
            img.sprite = item.imageSprite;
        }
        
        // Optional: Find and set label text
        TMPro.TextMeshProUGUI label = itemObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null && !string.IsNullOrEmpty(item.imageName))
        {
            label.text = item.imageName;
        }
        
        // Wire up button click if it exists
        Button btn = itemObj.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            int capturedIndex = index; // Capture for closure
            btn.onClick.AddListener(() => OnGalleryItemClicked(capturedIndex, item));
        }
    }
    
    private void OnGalleryItemClicked(int index, CompositionCarouselData.GalleryItem item)
    {
        Debug.Log($"[CompositionGalleryPopulator] Gallery item clicked: {item.imageName} (index: {index})");
        // TODO: Add your logic here (e.g., show detail, add to cart, etc.)
    }
}

