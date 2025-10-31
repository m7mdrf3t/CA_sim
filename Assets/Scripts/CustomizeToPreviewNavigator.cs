using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple navigator for moving from "5-CustomizedCrystal" to "6- Preview X Component" screens.
/// Add this component directly to your "Next" button GameObject in the 5-CustomizedCrystal screen.
/// </summary>
public class CustomizeToPreviewNavigator : MonoBehaviour
{
    [Header("Current Screen - ASSIGN IN INSPECTOR")]
    [Tooltip("Drag the '5-CustomizedCryrstel' GameObject from the scene hierarchy here")]
    public GameObject customizeScreen; // "5-CustomizedCrystal" screen - FORCE ASSIGNED
    
    [Header("Preview Screen")]
    [SerializeField] private GameObject previewScreen; // "6- Preview 1 Component"
    
    [Header("Data Source")]
    [SerializeField] private SelectedItemsUI selectedItemsUI; // Auto-finds if null
    
    [Header("Debug")]
    [SerializeField] private bool logDebugInfo = true;
    
    private void Awake()
    {
        // Ensure we're on a button
        if (GetComponent<Button>() == null)
        {
            Debug.LogError("[CustomizeToPreviewNavigator] Component must be on a GameObject with a Button component!");
            enabled = false;
            return;
        }
        
        // Auto-find SelectedItemsUI if not assigned
        if (selectedItemsUI == null)
        {
            selectedItemsUI = FindObjectOfType<SelectedItemsUI>();
            if (selectedItemsUI == null)
            {
                Debug.LogError("[CustomizeToPreviewNavigator] Could not find SelectedItemsUI!");
            }
        }
        
        // Wire button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnNextButtonClicked);
        }
    }
    
    private void OnDestroy()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnNextButtonClicked);
        }
    }
    
    public void OnNextButtonClicked()
    {
        Debug.Log("[CustomizeToPreviewNavigator] === NEXT BUTTON CLICKED ===");
        
        if (selectedItemsUI == null)
        {
            Debug.LogError("[CustomizeToPreviewNavigator] Cannot navigate - SelectedItemsUI not found!");
            return;
        }
        
        int selectionCount = selectedItemsUI.SelectionCount;
        
        if (logDebugInfo)
        {
            Debug.Log($"[CustomizeToPreviewNavigator] Next clicked! Selection count: {selectionCount}");
        }
        
        if (selectionCount == 0)
        {
            Debug.LogWarning("[CustomizeToPreviewNavigator] No crystals selected! Cannot proceed.");
            return;
        }
        
        // Hide customize screen
        if (customizeScreen == null)
        {
            Debug.LogError("[CustomizeToPreviewNavigator] 'Customize Screen' field is NOT assigned in the Inspector! Please drag the '5-CustomizedCryrstel' GameObject to this field.");
            Debug.LogError("[CustomizeToPreviewNavigator] CANNOT HIDE SCREEN WITHOUT THIS REFERENCE!");
            return;
        }
        
        Debug.Log($"[CustomizeToPreviewNavigator] Before hiding: customizeScreen.activeSelf = {customizeScreen.activeSelf}");
        customizeScreen.SetActive(false);
        Debug.Log($"[CustomizeToPreviewNavigator] After hiding: customizeScreen.activeSelf = {customizeScreen.activeSelf}");
        if (logDebugInfo)
        {
            Debug.Log($"[CustomizeToPreviewNavigator] Hid customize screen: {customizeScreen.name}");
        }
        
        // Show preview screen and populate it
        if (previewScreen != null)
        {
            previewScreen.SetActive(true);
            PopulatePreviewScreen(previewScreen);
        }
        else
        {
            // Try to find by name
            previewScreen = GameObject.Find("6- Preview 1 Component");
            if (previewScreen != null)
            {
                previewScreen.SetActive(true);
                PopulatePreviewScreen(previewScreen);
            }
        }
        
        if (logDebugInfo)
        {
            Debug.Log($"[CustomizeToPreviewNavigator] Navigated to preview screen with {selectionCount} crystals.");
        }
    }
    
    private void PopulatePreviewScreen(GameObject previewScreen)
    {
        if (previewScreen == null) return;
        
        var populator = previewScreen.GetComponentInChildren<PreviewCrystalPopulator>();
        if (populator != null)
        {
            populator.PopulateCrystals();
        }
        else
        {
            if (logDebugInfo)
            {
                Debug.LogWarning($"[CustomizeToPreviewNavigator] No PreviewCrystalPopulator found on {previewScreen.name}");
            }
        }
    }
}

