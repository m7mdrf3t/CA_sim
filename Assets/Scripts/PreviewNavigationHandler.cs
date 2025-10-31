using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles navigation from CustomizeCrystal screen to Preview screens.
/// Attach this to a button to automatically wire up the navigation.
/// </summary>
public class PreviewNavigationHandler : MonoBehaviour
{
    [Header("Optional - Auto-finds if not assigned")]
    [SerializeField] private SelectedItemsUI selectedItemsUI;
    [SerializeField] private GameObject[] previewScreens = new GameObject[4];
    
    [Header("Settings")]
    [SerializeField] private bool autoFindPreviewScreens = true;
    [SerializeField] private string customizerScreenName = "5-CustomizedCrystal";
    [SerializeField] private string previewScreenName = "6- Preview X Component";
    
    private void Awake()
    {
        // Auto-find SelectedItemsUI if not assigned
        if (selectedItemsUI == null)
        {
            selectedItemsUI = FindObjectOfType<SelectedItemsUI>();
        }
        
        // Auto-find preview screens if not assigned
        if (autoFindPreviewScreens)
        {
            for (int i = 0; i < 4; i++)
            {
                if (previewScreens[i] == null)
                {
                    string searchName = $"Preview {i + 1}";
                    GameObject found = GameObject.Find(searchName);
                    if (found == null)
                        found = GameObject.Find($"PreviewComponentsScreen/{searchName}");
                    if (found == null)
                        found = GameObject.Find($"6- Preview {i + 1} Component");
                    
                    previewScreens[i] = found;
                }
            }
        }
        
        // Wire button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnNextClicked);
        }
        else
        {
            Debug.LogError("[PreviewNavigationHandler] No Button component found on this GameObject!");
        }
    }
    
    private void OnDestroy()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnNextClicked);
        }
    }
    
    public void OnNextClicked()
    {
        if (selectedItemsUI == null)
        {
            Debug.LogError("[PreviewNavigationHandler] SelectedItemsUI not found!");
            return;
        }
        
        int count = selectedItemsUI.SelectionCount;
        
        if (count == 0)
        {
            Debug.LogWarning("[PreviewNavigationHandler] No crystals selected!");
            return;
        }
        
        Debug.Log($"[PreviewNavigationHandler] Navigating with {count} selections.");
        
        // Activate/deactivate preview screens based on count
        for (int i = 0; i < previewScreens.Length; i++)
        {
            if (previewScreens[i] != null)
            {
                bool shouldBeActive = (i < count);
                if (previewScreens[i].activeSelf != shouldBeActive)
                {
                    previewScreens[i].SetActive(shouldBeActive);
                }
                
                // If activating, populate crystals
                if (shouldBeActive)
                {
                    var populator = previewScreens[i].GetComponentInChildren<PreviewCrystalPopulator>();
                    if (populator != null)
                    {
                        populator.PopulateCrystals();
                    }
                }
            }
        }
        
        // Optionally switch to preview screen
        // You can uncomment this if you have a parent screen container
        // SwitchToPreviewScreen();
    }
    
    private void SwitchToPreviewScreen()
    {
        // Example: Find and activate the preview screen container
        GameObject previewContainer = GameObject.Find("PreviewComponentsScreen");
        if (previewContainer != null)
        {
            previewContainer.SetActive(true);
        }
        
        GameObject customizerContainer = GameObject.Find(customizerScreenName);
        if (customizerContainer != null)
        {
            customizerContainer.SetActive(false);
        }
    }
}

