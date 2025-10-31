using UnityEngine;
using TMPro;

/// <summary>
/// Handles category selection in the composition carousel (7-composition 1)
/// and navigates to the gallery view (8-composition 2) with the selected category data
/// </summary>
public class CarouselCategoryRouter : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private UICarouselSnap carousel;
    [SerializeField] private TextMeshProUGUI caption;   // Displays current category title

    [Header("Category Data")]
    [Tooltip("Single config file containing all category data (like GatekeeperConfig)")]
    [SerializeField] private CompositionConfig config;
    
    [Tooltip("Fallback: If config not assigned, use these simple arrays")]
    [SerializeField] private string[] categoryTitles;
    [SerializeField] private Sprite[] categorySprites;

    [Header("Navigation (7 → 8)")]
    [SerializeField] private GameObject currentScreen; // "7-composition 1"
    [SerializeField] private GameObject nextScreen;   // "8-composition 2"

    private void Awake()
    {
        Debug.Log($"[CarouselCategoryRouter] Awake - Config is {(config != null ? "ASSIGNED" : "NULL")}");
        
        if (carousel != null)
        {
            carousel.OnCenteredIndexChanged += OnCenteredChanged;
            carousel.OnCenterItemClicked += OnCenterClicked;
        }
    }

    private void OnDestroy()
    {
        if (carousel != null)
        {
            carousel.OnCenteredIndexChanged -= OnCenteredChanged;
            carousel.OnCenterItemClicked -= OnCenterClicked;
        }
    }

    private void OnCenteredChanged(int index)
    {
        // Update caption text when carousel changes centered item
        if (caption != null)
        {
            // Try config file first
            if (config != null)
            {
                var data = config.GetCategoryData(index);
                caption.text = data.title;
                Debug.Log($"[CarouselCategoryRouter] Caption updated to: {data.title}");
                return;
            }
            
            // Fallback to simple array approach
            if (categoryTitles != null && index >= 0 && index < categoryTitles.Length)
            {
                caption.text = categoryTitles[index];
                Debug.Log($"[CarouselCategoryRouter] Caption updated to: {categoryTitles[index]}");
            }
        }
    }

    private void OnCenterClicked(int index)
    {
        Debug.Log($"[CarouselCategoryRouter] Category clicked: index {index}");
        Debug.Log($"[CarouselCategoryRouter] Config is null: {config == null}");
        
        if (config != null)
        {
            // Use config file
            var data = config.GetCategoryData(index);
            Debug.Log($"[CarouselCategoryRouter] Got category data: title={data.title}, sprites={data.gallerySprites?.Length ?? 0}");
            
            SelectionBus.SelectedCategoryIndex = index;
            SelectionBus.SelectedCategoryTitle = data.title;
            SelectionBus.SelectedCategorySprite = data.placeholder;
            SelectionBus.SetCategoryData(data); // Store config data safely
            
            Debug.Log($"[CarouselCategoryRouter] Selected from config: {data.title}");
        }
        else if (categoryTitles != null && index >= 0 && index < categoryTitles.Length)
        {
            // Fallback to simple arrays
            SelectionBus.SelectedCategoryIndex = index;
            SelectionBus.SelectedCategoryTitle = categoryTitles[index];
            SelectionBus.SelectedCategorySprite = (categorySprites != null && index < categorySprites.Length) ? categorySprites[index] : null;
            SelectionBus.SelectedCategoryData = new CompositionConfig.CategoryData { title = categoryTitles[index] };
            
            Debug.Log($"[CarouselCategoryRouter] Selected from arrays: {categoryTitles[index]}");
        }
        else
        {
            Debug.LogError("[CarouselCategoryRouter] No config or categoryTitles assigned!");
            return;
        }

        // Navigate to 8-composition 2
        if (nextScreen != null)
        {
            if (currentScreen) 
            {
                Debug.Log("[CarouselCategoryRouter] Hiding 7-composition 1");
                currentScreen.SetActive(false);
            }
            
            Debug.Log("[CarouselCategoryRouter] Showing 8-composition 2");
            nextScreen.SetActive(true);
            
            // The auto initializer will run automatically via OnEnable when the screen activates
            Debug.Log("[CarouselCategoryRouter] Screen activated - auto initializer will run via OnEnable");
            
            // Also try old system initialization
            var level2Initializer = nextScreen.GetComponent<CompositionLevel2Initializer>();
            if (level2Initializer == null)
            {
                level2Initializer = nextScreen.GetComponentInChildren<CompositionLevel2Initializer>();
            }
            
            if (level2Initializer != null)
            {
                // Force refresh the Level 2 screen with selected data
                level2Initializer.gameObject.SetActive(false);
                level2Initializer.gameObject.SetActive(true);
                Debug.Log($"[CarouselCategoryRouter] Level 2 composition initialized");
            }
        }
        else
        {
            Debug.LogWarning("[CarouselCategoryRouter] nextScreen is not assigned!");
        }
    }
}