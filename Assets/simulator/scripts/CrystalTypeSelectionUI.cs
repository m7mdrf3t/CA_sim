using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

// ==================== CRYSTAL TYPE SELECTION UI ====================
/// <summary>
/// Creates buttons for each crystal TYPE (main categories) from the database
/// Attach this to a UI Panel
/// </summary>
public class CrystalTypeSelectionUI : MonoBehaviour
{
  [Header("Database Reference")]
    [SerializeField] private CrystalDatabase crystalDatabase;
    [SerializeField] private CrystalVariantSelectionUI _CrystalVariantUIBuilder;
    [Header("UI References")]
    [SerializeField] private GameObject buttonPrefab;
    [SerializeField] private Sprite fallbackIcon;
    
    [Header("Gallery Components")]
    [SerializeField] private GalleryPagination galleryPagination;
    
    [Header("Button Settings")]
    [SerializeField] private Vector2 buttonSize = new Vector2(150, 150);
    
    private List<GameObject> createdButtons = new List<GameObject>();
    
    [SerializeField] private bool refractionToggle;

    void Start()
    {
        if (crystalDatabase == null)
        {
            crystalDatabase = Resources.Load<CrystalDatabase>("Databases/CrystalDatabase");
        }
        
        GenerateTypeButtonsGallery();
    }
    
    /// <summary>
    /// Generate buttons for each crystal type and add to gallery
    /// </summary>
    [ContextMenu("Generate Type Buttons Gallery")]
    public void GenerateTypeButtonsGallery()
    {
        if (crystalDatabase == null)
        {
            Debug.LogError("[CrystalTypeGalleryUI] CrystalDatabase not assigned!");
            return;
        }
        
        if (galleryPagination == null)
        {
            Debug.LogError("[CrystalTypeGalleryUI] GalleryPagination not assigned!");
            return;
        }
        
        if (buttonPrefab == null)
        {
            Debug.LogError("[CrystalTypeGalleryUI] Button prefab not assigned!");
            return;
        }
        
        // Clear existing buttons
        ClearGallery();
        
        // Create button for each category (main type)
        foreach (var category in crystalDatabase.categories)
        {
            GameObject buttonObj = CreateTypeButton(category);
            if (buttonObj != null)
            {
                createdButtons.Add(buttonObj);
            }
        }
        
        // Initialize gallery with all buttons
        galleryPagination.Initialize(createdButtons);
        
        Debug.Log($"[CrystalTypeGalleryUI] Generated {createdButtons.Count} type buttons in gallery");
    }
    
    /// <summary>
    /// Create a button for a crystal type
    /// </summary>
    private GameObject CreateTypeButton(CrystalDatabase.CrystalCategory category)
    {

        if(category.crystalMainTypes == CrystalMainTypes.Refraction && refractionToggle)
        {
            return null;
        }

        if(category.crystalMainTypes == CrystalMainTypes.Breeze && !refractionToggle)
        {
            return null;
        }

        
        GameObject buttonObj = Instantiate(buttonPrefab);
        buttonObj.name = $"TypeBtn_{category.categoryName}";
        
        // Set button size
        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        if (btnRect != null)
        {
            btnRect.sizeDelta = buttonSize;
        }
        
        // Setup button click
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnTypeButtonClick(category));
        }
        
        // Set button text/label
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = category.categoryName;
        }
        
#if TMP_PRESENT
        // Try TextMeshPro if Text not found
        TMPro.TMP_Text tmpText = buttonObj.GetComponentInChildren<TMPro.TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = category.categoryName;
        }
#endif
        
        // Set button icon - try main button image first
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null && category.defaultSprite != null)
        {
            // Only set if this is a background image, not the button itself
            if (buttonImage.GetComponent<Button>() == null)
            {
                buttonImage.sprite = category.defaultSprite;
                buttonImage.preserveAspect = true;
            }
        }
        
        // Look for Icon child object
        Transform iconTransform = buttonObj.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = category.defaultSprite != null ? category.defaultSprite : fallbackIcon;
                iconImage.enabled = iconImage.sprite != null;
                iconImage.preserveAspect = true;
            }
        }
        else
        {
            // Alternative: Find first child Image that's not the button itself
            Image[] images = buttonObj.GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img.gameObject != buttonObj && img.GetComponent<Button>() == null)
                {
                    img.sprite = category.defaultSprite != null ? category.defaultSprite : fallbackIcon;
                    img.enabled = img.sprite != null;
                    img.preserveAspect = true;
                    break;
                }
            }
        }
        
        return buttonObj;
    }
    
    /// <summary>
    /// Called when a type button is clicked - opens variant selection
    /// </summary>
    private void OnTypeButtonClick(CrystalDatabase.CrystalCategory category)
    {
        Debug.Log($"[CrystalTypeGalleryUI] Type selected: {category.categoryName}");
        
        CrystalVariantSelectionUI variantUI = FindFirstObjectByType<CrystalVariantSelectionUI>();
        variantUI.SetCrystalDefaultVideo(category.defaultVideoClip);

        // If only one variant, add directly
        if (category.variants.Count == 1)
        {
            if (variantUI != null)
            {
                variantUI.ShowVariantsForType(category);
            }
            else
            {
                // No variant UI, add first variant directly
                 ConfigurationManager.Instance.AddCrystalSelection(category.type, 0 , 0, category.price);

            }
            return;
        }
        
        // If multiple variants, show variant selection UI
        if (variantUI != null)
        {
            variantUI.ShowVariantsForType(category);
        }
        else
        {
            Debug.LogWarning("[CrystalTypeGalleryUI] No CrystalVariantSelectionUI found in scene!");
            // No variant UI found, add first variant by default
            ConfigurationManager.Instance.AddCrystalSelection(category.type, 0 , 0 , category.price);
        }
    }
    
    /// <summary>
    /// Clear all buttons from gallery
    /// </summary>
    public void ClearGallery()
    {
        foreach (var btn in createdButtons)
        {
            if (btn != null)
            {
                Destroy(btn);
            }
        }
        createdButtons.Clear();
        
        if (galleryPagination != null)
        {
            galleryPagination.ClearGallery();
        }
    }
    
    /// <summary>
    /// Refresh buttons (call after database changes)
    /// </summary>
    public void RefreshButtons()
    {
        GenerateTypeButtonsGallery();
    }
    
    /// <summary>
    /// Navigate to next page
    /// </summary>
    public void NextPage()
    {
        if (galleryPagination != null)
        {
            galleryPagination.NextPage();
        }
    }
    
    /// <summary>
    /// Navigate to previous page
    /// </summary>
    public void PreviousPage()
    {
        if (galleryPagination != null)
        {
            galleryPagination.PreviousPage();
        }
    }
    
    /// <summary>
    /// Go to specific page
    /// </summary>
    public void GoToPage(int pageIndex)
    {
        if (galleryPagination != null)
        {
            galleryPagination.GoToPage(pageIndex);
        }
    }
    
    /// <summary>
    /// Get current page info
    /// </summary>
    public string GetPageInfo()
    {
        if (galleryPagination != null)
        {
            int current = galleryPagination.GetCurrentPage() + 1;
            int total = galleryPagination.GetTotalPages();
            return $"Page {current} / {total}";
        }
        return "No pages";
    }
}