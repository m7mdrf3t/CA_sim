using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple UI with 6 buttons to add crystals directly
/// Attach to your Canvas
/// </summary>
public class simpleCryistalUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UserConfig userConfig;
    
    [Header("Buttons (Assign in order 0-5)")]
    [SerializeField] private Button[] crystalButtons = new Button[6];
    
    [Header("Display (Optional)")]
    [SerializeField] private TMP_Text selectedCountText;
    [SerializeField] private Button clearAllButton;
    [SerializeField] private Button generateButton;

    [Header("Configuration")]
    [Tooltip("Which crystal type to use (default: Standard)")]
    [SerializeField] private CrystalType crystalTypeToUse = CrystalType.Breeze;

    private void Start()
    {
        SetupButtons();
    }

    void SetupButtons()
    {
        // Setup crystal buttons
        for (int i = 0; i < crystalButtons.Length; i++)
        {
            if (crystalButtons[i] != null)
            {
                int variantIndex = i; // Capture index for closure
                crystalButtons[i].onClick.AddListener(() => AddCrystalByVariantIndex(variantIndex));
            }
        }

        // Setup utility buttons
        if (clearAllButton != null)
        {
            clearAllButton.onClick.AddListener(ClearAllSelections);
        }

        if (generateButton != null)
        {
            //generateButton.onClick.AddListener(OnGenerateClicked);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// Main function: Add crystal by variant index
    /// </summary>
    public void AddCrystalByVariantIndex(int variantIndex)
    {
        if (userConfig == null)
        {
            Debug.LogError("UserConfig not assigned!");
            return;
        }

        if (userConfig.crystalDatabase == null)
        {
            Debug.LogError("Crystal Database not assigned in UserConfig!");
            return;
        }

        // Check if we can add more (max 4)
        if (userConfig.crystalSelections.Count >= 4)
        {
            Debug.LogWarning("Already at maximum crystals (4)!");
            return;
        }

        // Get variants for the specified type
        var variants = userConfig.crystalDatabase.GetVariantsForType(crystalTypeToUse);
        
        if (variants == null || variants.Count == 0)
        {
            Debug.LogError($"No variants found for type: {crystalTypeToUse}");
            return;
        }

        // Validate variant index
        if (variantIndex < 0 || variantIndex >= variants.Count)
        {
            Debug.LogError($"Invalid variant index: {variantIndex}. Available: 0-{variants.Count - 1}");
            return;
        }

        // Create new selection
        var newSelection = new UserConfig.CrystalSelection
        {
            enabled = true,
            crystalType = crystalTypeToUse,
            variantIndex = variantIndex,
            spawnWeight = 1f / (userConfig.crystalSelections.Count + 1) // Auto-balance weights
        };

        // Add to config
        userConfig.crystalSelections.Add(newSelection);

        // Re-balance all weights equally
        RebalanceWeights();

        Debug.Log($"Added crystal variant {variantIndex} ({variants[variantIndex].variantName})");
        
        UpdateDisplay();
    }

    /// <summary>
    /// Alternative: Add by button number (1-6 instead of 0-5)
    /// </summary>
    public void AddCrystalByButtonNumber(int buttonNumber)
    {
        AddCrystalByVariantIndex(buttonNumber - 1);
    }

    /// <summary>
    /// Remove last added crystal
    /// </summary>
    public void RemoveLastCrystal()
    {
        if (userConfig.crystalSelections.Count > 0)
        {
            userConfig.crystalSelections.RemoveAt(userConfig.crystalSelections.Count - 1);
            RebalanceWeights();
            UpdateDisplay();
            Debug.Log("Removed last crystal");
        }
    }

    /// <summary>
    /// Clear all selections
    /// </summary>
    public void ClearAllSelections()
    {
        userConfig.crystalSelections.Clear();
        userConfig.EnsureMinimumSelection(); // Ensures at least 1 exists
        UpdateDisplay();
        Debug.Log("Cleared all crystal selections");
    }

    /// <summary>
    /// Auto-balance spawn weights equally
    /// </summary>
    void RebalanceWeights()
    {
        int count = userConfig.crystalSelections.Count;
        if (count == 0) return;

        float equalWeight = 1f / count;
        foreach (var selection in userConfig.crystalSelections)
        {
            selection.spawnWeight = equalWeight;
        }
    }

    /// <summary>
    /// Update display text
    /// </summary>
    void UpdateDisplay()
    {
        if (selectedCountText != null)
        {
            int count = userConfig.crystalSelections.Count;
            selectedCountText.text = $"Selected: {count}/4 Crystals";
            
            // Show which variants are selected
            if (count > 0)
            {
                selectedCountText.text += "\n";
                for (int i = 0; i < count; i++)
                {
                    var sel = userConfig.crystalSelections[i];
                    var variants = userConfig.crystalDatabase.GetVariantsForType(sel.crystalType);
                    if (variants != null && sel.variantIndex < variants.Count)
                    {
                        string variantName = variants[sel.variantIndex].variantName;
                        selectedCountText.text += $"\n• {variantName} ({sel.spawnWeight * 100:F0}%)";
                    }
                }
            }
        }

        // Disable buttons if at max
        bool canAddMore = userConfig.crystalSelections.Count < 4;
        foreach (var button in crystalButtons)
        {
            if (button != null)
            {
                button.interactable = canAddMore;
            }
        }
    }

   
}

// ==================== USAGE EXAMPLE ====================
/*
 * HOW TO USE:
 * 
 * 1. Create 6 buttons in your Canvas (Button0, Button1... Button5)
 * 2. Add SimpleCrystalUI script to Canvas
 * 3. Assign:
 *    - UserConfig
 *    - The 6 buttons in order
 *    - (Optional) Display text and utility buttons
 * 4. Click any button to add that variant (0-5) to the selection
 * 
 * BUTTON CLICK METHODS:
 * - Each button calls: AddCrystalByVariantIndex(0-5)
 * - OR use AddCrystalByButtonNumber(1-6) if you prefer 1-based
 * 
 * EXAMPLE SETUP:
 * Button 0 → Calls AddCrystalByVariantIndex(0) → Adds "Blue Crystal"
 * Button 1 → Calls AddCrystalByVariantIndex(1) → Adds "Red Crystal"
 * Button 2 → Calls AddCrystalByVariantIndex(2) → Adds "Green Crystal"
 * etc...
 * 
 * The system automatically:
 * - Limits to 4 total crystals
 * - Balances spawn weights equally
 * - Disables buttons when full
 * - Shows what's selected
 */