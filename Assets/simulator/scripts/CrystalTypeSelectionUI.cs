using UnityEngine;
using UnityEngine.UI;
// ==================== CRYSTAL TYPE SELECTION UI ====================
/// <summary>
/// Creates buttons for each crystal TYPE (main categories) from the database
/// Attach this to a UI Panel
/// </summary>
public class CrystalTypeSelectionUI : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private CrystalDatabase crystalDatabase;
    
    [Header("UI Settings")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject buttonPrefab;
    
    [Header("Button Spacing (Optional)")]
    [SerializeField] private float spacing = 10f;
    
    void Start()
    {
        GenerateTypeButtons();
    }
    
    /// <summary>
    /// Generate buttons for each crystal type in the database
    /// </summary>
    public void GenerateTypeButtons()
    {
        if (crystalDatabase == null)
        {
            Debug.LogError("CrystalDatabase not assigned!");
            return;
        }
        
        if (buttonContainer == null)
        {
            Debug.LogError("Button container not assigned!");
            return;
        }
        
        if (buttonPrefab == null)
        {
            Debug.LogError("Button prefab not assigned!");
            return;
        }
        
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create button for each category (main type)
        foreach (var category in crystalDatabase.categories)
        {
            CreateTypeButton(category);
        }
        
        Debug.Log($"Generated {crystalDatabase.categories.Count} type buttons");
    }
    
    /// <summary>
    /// Create a button for a crystal type
    /// </summary>
    private void CreateTypeButton(CrystalDatabase.CrystalCategory category)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        
        // Setup button click
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnTypeButtonClick(category));
        }
        
        // Set button text
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = category.categoryName;
        }
        
        // Set button icon (if available)
        Image buttonImage = buttonObj.GetComponent<Image>();
        if (buttonImage != null && category.defaultSprite != null)
        {
            buttonImage.sprite = category.defaultSprite;
        }
        
        // Alternative: Set icon on a child Image
        Image[] images = buttonObj.GetComponentsInChildren<Image>();
        foreach (var img in images)
        {
            if (img.gameObject != buttonObj && category.defaultSprite != null)
            {
                img.sprite = category.defaultSprite;
                break;
            }
        }
    }
    
    /// <summary>
    /// Called when a type button is clicked - opens variant selection
    /// </summary>
    private void OnTypeButtonClick(CrystalDatabase.CrystalCategory category)
    {
        Debug.Log($"Type selected: {category.categoryName}");
        
        // If only one variant, add directly
        if (category.variants.Count == 1)
        {
            ConfigurationManager.Instance.AddCrystalSelection(category.type, 0);
            return;
        }
        
        // If multiple variants, show variant selection UI
        CrystalVariantSelectionUI variantUI = FindFirstObjectByType<CrystalVariantSelectionUI>();
        if (variantUI != null)
        {
            variantUI.ShowVariantsForType(category);
        }
        else
        {
            Debug.Log("there are no selcetionUI founded in the app ");
            // No variant UI found, add first variant by default
            ConfigurationManager.Instance.AddCrystalSelection(category.type, 0);
        }
    }
    
    /// <summary>
    /// Refresh buttons (call after database changes)
    /// </summary>
    public void RefreshButtons()
    {
        GenerateTypeButtons();
    }
}
