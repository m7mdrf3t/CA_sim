using UnityEngine;

/// <summary>
/// Clears crystal selections when the game starts or a new user begins.
/// Attach this to a GameObject in your starting scene.
/// </summary>
public class SelectionClearOnStart : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool clearOnStart = true;
    [SerializeField] private bool clearOnAwake = false;
    
    private void Awake()
    {
        if (clearOnAwake)
        {
            ClearSelections();
        }
    }
    
    private void Start()
    {
        if (clearOnStart)
        {
            ClearSelections();
        }
    }
    
    private void ClearSelections()
    {
        SelectionBus.ClearSelections();
        Debug.Log("[SelectionClearOnStart] Cleared all crystal selections.");
    }
    
    /// <summary>
    /// Public method to manually clear selections (can be called from UI button, etc.)
    /// </summary>
    public void ClearSelectionsManual()
    {
        ClearSelections();
    }
}

