using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ==================== CRYSTAL VARIANT SELECTION UI ====================
/// <summary>
/// Shows variants for a selected crystal type
/// This opens as a popup/panel when user selects a type
/// </summary>
public class CrystalVariantSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject variantPanel;
    [SerializeField] private Transform variantButtonContainer;
    [SerializeField] private GameObject variantButtonPrefab;
    [SerializeField] private TMP_Text titleText;

    [Header("Crystal Colors Sprite Icons")]
    [SerializeField] private Sprite[] crystalColors;
    
    private CrystalDatabase.CrystalCategory currentCategory;
    
    void Start()
    {
    
    }


    
    /// <summary>
    /// Show variants for a specific crystal type
    /// </summary>
    public void ShowVariantsForType(CrystalDatabase.CrystalCategory category)
    {

        variantPanel.SetActive(true);

        currentCategory = category;
        
        if (variantPanel != null)
        {
            variantPanel.SetActive(true);
        }
        
        if (titleText != null)
        {
            titleText.text = $"Select {category.categoryName} Variant";
        }
        
        GenerateVariantButtons();
    }
    
    /// <summary>
    /// Generate buttons for each variant
    /// </summary>
    private void GenerateVariantButtons()
    {
        if (variantButtonContainer == null || variantButtonPrefab == null || currentCategory == null)
            return;
        
        // Clear existing buttons
        foreach (Transform child in variantButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create button for each variant
        for (int i = 0; i < currentCategory.variants.Count; i++)
        {
            int variantIndex = i; // Capture for closure
            var variant = currentCategory.variants[i];
            
            GameObject buttonObj = Instantiate(variantButtonPrefab, variantButtonContainer);
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnVariantButtonClick(variantIndex));
            }
            
            // Set variant name
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = variant.variantName;
            }
            
            // Set variant icon
            Image buttonImage = buttonObj.GetComponent<Image>();
            
            if (buttonImage != null)
            {
                switch (variant.variantName.ToLower())
                {
                    case "red":
                        buttonImage.sprite = crystalColors.Length > 0 ? crystalColors[0] : null;
                        break;
                    case "blue":
                        buttonImage.sprite = crystalColors.Length > 1 ? crystalColors[1] : null;
                        break;
                    case "green":
                        buttonImage.sprite = crystalColors.Length > 2 ? crystalColors[2] : null;
                        break;
                    case "yellow":
                        buttonImage.sprite = crystalColors.Length > 3 ? crystalColors[3] : null;
                        break;
                    case "magnet":
                        buttonImage.sprite = crystalColors.Length > 4 ? crystalColors[4] : null;
                        break;
                    case "orange":
                        buttonImage.sprite = crystalColors.Length > 5 ? crystalColors[5] : null;
                        break;
                    default:
                        buttonImage.sprite = null;
                        break;
                }
                // buttonImage.color = variant.color;
            }
        }
    }
    
    /// <summary>
    /// Called when a variant button is clicked
    /// </summary>
    private void OnVariantButtonClick(int variantIndex)
    {
        if (currentCategory == null) return;
        
        // Add the selection to config
        ConfigurationManager.Instance.AddCrystalSelection(
            currentCategory.type, 
            variantIndex
        );
        
        Debug.Log($"Added: {currentCategory.categoryName}, Variant {variantIndex}");
        
        // Close panel
        ClosePanel();
        
        // Refresh selected crystals list
        SelectedCrystalsUI selectedUI = FindFirstObjectByType<SelectedCrystalsUI>();
        if (selectedUI != null)
        {
            selectedUI.RefreshList();
        }
    }
    
    /// <summary>
    /// Close the variant selection panel
    /// </summary>
    public void ClosePanel()
    {
        if (variantPanel != null)
        {
        //   variantPanel.SetActive(false);
        }
    }
}
