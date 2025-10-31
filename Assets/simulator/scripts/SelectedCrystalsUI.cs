
using TMPro;
using UnityEngine;
using UnityEngine.UI;


// ==================== SELECTED CRYSTALS DISPLAY UI ====================
/// <summary>
/// Displays the currently selected crystals (up to 4)
/// Shows what's in the config
/// </summary>
public class SelectedCrystalsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform selectedContainer;
    [SerializeField] private GameObject selectedItemPrefab;
    


    void Start()
    {
        ConfigurationManager.Instance.ClearAllSelections();
        RefreshList();
    }

    
    
    /// <summary>
    /// Refresh the list of selected crystals
    /// </summary>
    public void RefreshList()
    {
        if (selectedContainer == null || selectedItemPrefab == null) return;
        
        // Clear existing items
        foreach (Transform child in selectedContainer)
        {
            Destroy(child.gameObject);
        }
        
        var selections = ConfigurationManager.Instance.GetCurrentSelections();
        var config = ConfigurationManager.Instance.GetCurrentConfig();
        
        if (config == null) return;
        
        // Create item for each selection
        for (int i = 0; i < selections.Count; i++)
        {
            int index = i; // Capture for closure
            var selection = selections[i];
            
            GameObject item = Instantiate(selectedItemPrefab, selectedContainer);
            
            // Set info text
            TMPro.TMP_Text infoText = item.GetComponentInChildren<TMP_Text>();
            if (infoText != null)
            {
                infoText.text = ConfigurationManager.Instance.GetSelectionInfo(index);
            }
            
            // Set icon
            var variant = config.GetVariantForSelection(selection);
            Image icon = item.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && variant != null && variant.icon != null)
            {
                icon.sprite = variant.icon;
            }
            
            // Set color indicator
            Image colorIndicator = item.transform.Find("ColorIndicator")?.GetComponent<Image>();
            if (colorIndicator != null && variant != null)
            {
                colorIndicator.color = variant.color;
            }
            
            // Setup remove button
            Button removeButton = item.GetComponentInChildren<Button>();
            if (removeButton != null)
            {
                removeButton.onClick.AddListener(() => 
                {
                    ConfigurationManager.Instance.RemoveCrystalSelection(index);
                    RefreshList();
                });
            }
        }
   
    }
    
    /// <summary>
    /// Clear all selections button callback
    /// </summary>
    public void OnClearAllClick()
    {
        ConfigurationManager.Instance.ClearAllSelections();
        RefreshList();
    }
}