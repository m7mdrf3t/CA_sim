using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;

/// <summary>
/// Automatically initializes 8-composition 2 screen with selected category data
/// Supports BOTH old (CompositionLevel2Initializer) and new (CompositionGalleryPopulator) systems
/// </summary>
public class CompositionCategoryAutoInitializer : MonoBehaviour
{
    [Header("Hero Image (placeholder from carousel)")]
    [SerializeField] private Image heroImage;
    [SerializeField] private CrystalDataManager crystalDataManager;
    [SerializeField] private UserConfig config;
    
    [Header("Gallery Title")]
    [SerializeField] private TextMeshProUGUI galleryTitle;
    
    [Header("Gallery Grid")]
    [Tooltip("Parent where gallery items will be instantiated")]
    [SerializeField] private Transform galleryGridParent;
    [Tooltip("Prefab to instantiate for each gallery item (must have Image component)")]
    [SerializeField] private GameObject galleryItemPrefab;
    
    [Header("Navigation Buttons")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    
    private System.Collections.Generic.List<GameObject> instantiatedGalleryItems = new System.Collections.Generic.List<GameObject>();
    private CompositionConfig.CategoryData currentCategoryData;
    private int currentGalleryIndex = 0;
    private string lastInitializedTitle = "";
    
    private void OnEnable()
    {
        // Prevent double initialization for SAME category
        if (!string.IsNullOrEmpty(lastInitializedTitle) && lastInitializedTitle == SelectionBus.SelectedCategoryTitle)
        {
            return;
        }

        // Wire up navigation buttons
        SetupNavigationButtons();
        
        // Initialize with the selected category data
        if ((SelectionBus.SelectedCategoryData.gallerySprites != null && SelectionBus.SelectedCategoryData.gallerySprites.Length > 0) || 
            !string.IsNullOrEmpty(SelectionBus.SelectedCategoryData.title))
        {
            lastInitializedTitle = SelectionBus.SelectedCategoryTitle;
            InitializeScreen(SelectionBus.SelectedCategoryData);
        }
        else
        {
            Debug.LogError($"[CompositionCategoryAutoInitializer] No category data found! title={SelectionBus.SelectedCategoryTitle}, sprites={SelectionBus.SelectedCategoryData.gallerySprites?.Length ?? -1}");
        }
    }
    
    private void OnDisable()
    {        
        // Unwire navigation buttons
        if (previousButton != null)
            previousButton.onClick.RemoveListener(NavigatePrevious);
        if (nextButton != null)
            nextButton.onClick.RemoveListener(NavigateNext);
    }
    
    private void InitializeScreen(CompositionConfig.CategoryData data)
    {
        
        // Store current category data for navigation
        currentCategoryData = data;
        currentGalleryIndex = 0; // Start at first gallery item
        
        // Set hero image to placeholder initially
        if (heroImage != null && data.placeholder != null)
        {
            heroImage.sprite = data.placeholder;
        }
        else if (heroImage != null)
        {
            Debug.LogWarning("[CompositionCategoryAutoInitializer] Hero Image reference is assigned but no placeholder!");
        }
        
        // Set gallery title
        if (galleryTitle != null)
        {
            galleryTitle.text = data.title;
        }
        
        // Populate gallery grid
        if (galleryGridParent != null && galleryItemPrefab != null)
        {
            PopulateGalleryGrid(data);
        }
        else
        {
            if (galleryGridParent == null) Debug.LogWarning("  - Gallery Grid Parent is NULL");
            if (galleryItemPrefab == null) Debug.LogWarning("  - Gallery Item Prefab is NULL");
        }
        
        // Update navigation buttons state
        UpdateNavigationButtons();
    }
    
    /// <summary>
    /// Setup navigation button listeners
    /// </summary>
    private void SetupNavigationButtons()
    {
        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(NavigatePrevious); // Remove first to avoid duplicates
            previousButton.onClick.AddListener(NavigatePrevious);
        }
        
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(NavigateNext);
            nextButton.onClick.AddListener(NavigateNext);
        }
    }
    
    /// <summary>
    /// Navigate to previous gallery item (no wrapping)
    /// </summary>
    public void NavigatePrevious()
    {
        if (currentCategoryData.gallerySprites == null || currentCategoryData.gallerySprites.Length == 0)
        {
            return;
        }
        
        currentGalleryIndex--;
        if (currentGalleryIndex < 0)
            currentGalleryIndex = 0; // Stay at first, no wrapping
        
        UpdateHeroImage();
        UpdateNavigationButtons();
        
    }
    
    /// <summary>
    /// Navigate to next gallery item (no wrapping)
    /// </summary>
    public void NavigateNext()
    {
        if (currentCategoryData.gallerySprites == null || currentCategoryData.gallerySprites.Length == 0)
        {
            return;
        }
        
        currentGalleryIndex++;
        if (currentGalleryIndex >= currentCategoryData.gallerySprites.Length)
            currentGalleryIndex = currentCategoryData.gallerySprites.Length - 1; // Stay at last, no wrapping
        
        UpdateHeroImage();
        UpdateNavigationButtons();
        
    }
    
    /// <summary>
    /// Navigate to specific gallery item by index (called when clicking gallery items)
    /// </summary>
    public void NavigateToIndex(int index)
    {
        if (currentCategoryData.gallerySprites == null || currentCategoryData.gallerySprites.Length == 0)
        {
            return;
        }
        
        if (index < 0 || index >= currentCategoryData.gallerySprites.Length)
        {
            return;
        }
        
        currentGalleryIndex = index;
        UpdateHeroImage();
        UpdateNavigationButtons();
        
    }
    
    /// <summary>
    /// Update hero image based on current gallery index
    /// </summary>
    private void UpdateHeroImage()
    {
        if (heroImage == null)
        {
            Debug.LogWarning("[CompositionCategoryAutoInitializer] Hero image is null!");
            return;
        }
        
        if (currentCategoryData.gallerySprites == null || currentGalleryIndex < 0 || currentGalleryIndex >= currentCategoryData.gallerySprites.Length)
        {
            Debug.LogWarning("[CompositionCategoryAutoInitializer] Invalid gallery index or sprites array!");
            return;
        }
        
        Sprite currentSprite = currentCategoryData.gallerySprites[currentGalleryIndex];
        if (currentSprite != null)
        {
            heroImage.sprite = currentSprite;
            Debug.Log($"[CompositionCategoryAutoInitializer] Hero image updated: {currentSprite.name}");
        }
        else
        {
            Debug.LogWarning($"[CompositionCategoryAutoInitializer] Gallery sprite at index {currentGalleryIndex} is null!");
        }
    }
    
    /// <summary>
    /// Update navigation buttons interactable state (no wrapping - disable at boundaries)
    /// </summary>
    private void UpdateNavigationButtons()
    {
        if (currentCategoryData.gallerySprites == null || currentCategoryData.gallerySprites.Length <= 1)
        {
            // Disable buttons if no items or only 1 item
            if (previousButton != null) previousButton.interactable = false;
            if (nextButton != null) nextButton.interactable = false;
            return;
        }
        
        // Update Previous button - disabled at first item
        if (previousButton != null)
        {
            previousButton.interactable = currentGalleryIndex > 0;
        }
        
        // Update Next button - disabled at last item
        if (nextButton != null)
        {
            nextButton.interactable = currentGalleryIndex < currentCategoryData.gallerySprites.Length - 1;
        }
    }
    
    private void PopulateGalleryGrid(CompositionConfig.CategoryData data)
    {
        // Clear existing gallery items
        ClearGalleryGrid();
        if (data.gallerySprites != null && data.gallerySprites.Length > 0)
        {            
            for (int i = 0; i < data.gallerySprites.Length; i++)
            {
                var sprite = data.gallerySprites[i];
                if (sprite != null)
                {
                    var item = CreateGalleryItemFromSprite(sprite, i);
                    if (item != null)
                    {
                        instantiatedGalleryItems.Add(item);
                    }
                }
            }
    
        }
        else
        {
            Debug.LogWarning($"[CompositionCategoryAutoInitializer] Category '{data.title}' has no gallery sprites!");
        }
    }
    
    private GameObject CreateGalleryItemFromSprite(Sprite sprite, int index)
    {
        if (galleryItemPrefab == null)
        {
            Debug.LogError("[CompositionCategoryAutoInitializer] Gallery Item Prefab is NULL!");
            return null;
        }
        
        GameObject itemObj = Instantiate(galleryItemPrefab, galleryGridParent);
        
        // Make sure it's active and visible
        itemObj.SetActive(true);
        var canvasGroup = itemObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        
        // Set image
        Image img = itemObj.GetComponentInChildren<Image>(true);
        if (img != null)
        {
            img.sprite = sprite;
            img.color = Color.white; // Ensure it's visible
            Debug.Log($"[CompositionCategoryAutoInitializer] Item {index} sprite set: {sprite.name}");
            Debug.Log($"[CompositionCategoryAutoInitializer] Item {index} GameObject active: {itemObj.activeSelf}, name: {itemObj.name}");
        }
        else
        {
            Debug.LogWarning($"[CompositionCategoryAutoInitializer] Gallery item prefab '{galleryItemPrefab.name}' has no Image component!");
        }
        
        // Add click listener to gallery item
        Button btn = itemObj.GetComponent<Button>();
        if (btn == null)
        {
            btn = itemObj.AddComponent<Button>();
        }
        
        // Capture index in local variable for closure
        int galleryIndex = index;
        btn.onClick.RemoveAllListeners(); // Clear any existing listeners
        btn.onClick.AddListener(() => OnGalleryItemClicked(galleryIndex));
        
        Debug.Log($"[CompositionCategoryAutoInitializer] Added click listener to item {index}");
        
        return itemObj;
    }
    
    private void ClearGalleryGrid()
    {
        foreach (GameObject item in instantiatedGalleryItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        instantiatedGalleryItems.Clear();
        
        // Also clear any lingering children
        if (galleryGridParent != null)
        {
            for (int i = galleryGridParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(galleryGridParent.GetChild(i).gameObject);
            }
        }
    }
    
    private GameObject CreateGalleryItem(CompositionCarouselData.GalleryItem item, int index)
    {
        if (galleryItemPrefab == null)
        {
            Debug.LogError("[CompositionCategoryAutoInitializer] Gallery Item Prefab is NULL!");
            return null;
        }
        
        GameObject itemObj = Instantiate(galleryItemPrefab, galleryGridParent);
        
        // Set image
        Image img = itemObj.GetComponentInChildren<Image>(true);
        if (img != null && item.imageSprite != null)
        {
            img.sprite = item.imageSprite;
            Debug.Log($"[CompositionCategoryAutoInitializer] Item {index} sprite set: {item.imageSprite.name}");
        }
        else if (img == null)
        {
            Debug.LogWarning($"[CompositionCategoryAutoInitializer] Gallery item prefab '{galleryItemPrefab.name}' has no Image component!");
        }
        
        // Set label if available
        TMPro.TextMeshProUGUI label = itemObj.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null && !string.IsNullOrEmpty(item.imageName))
        {
            label.text = item.imageName;
        }
        
        // Wire button
        Button btn = itemObj.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            int capturedIndex = index;
            btn.onClick.AddListener(() => OnGalleryItemClicked(capturedIndex, item));
        }
        
        return itemObj;
    }
    
    /// <summary>
    /// Called when a gallery item is clicked from the grid
    /// </summary>
    private void OnGalleryItemClicked(int index)
    {
        config.BaseType = currentCategoryData.shapeType;
        config.hangerSurface = currentCategoryData.surfaceType[index];
        
        Debug.Log($"[CompositionCategoryAutoInitializer] Gallery item clicked: index={index}, shapeType={currentCategoryData.shapeType}, surfaceType={currentCategoryData.surfaceType[index]}");
        
        currentGalleryIndex = index;
        config.GallaryIndex = currentGalleryIndex;

        NavigateToIndex(index);
        crystalDataManager.SetBaseShape(currentCategoryData.surfaceType[index].ToString());
        crystalDataManager.SetCompStyle(currentCategoryData.shapeType.ToString());
        
    }
    
    private void OnGalleryItemClicked(int index, CompositionCarouselData.GalleryItem item)
    {
        Debug.Log($"[CompositionCategoryAutoInitializer] Gallery item clicked: {item.imageName}");
    }
    
    /// <summary>
    /// Helper method to get the full hierarchy path of a GameObject
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
}

