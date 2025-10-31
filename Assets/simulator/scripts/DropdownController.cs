// ============================================
// SimpleDropdownController.cs
// Shows/hides different text groups based on dropdown selection
// ============================================

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DropdownController : MonoBehaviour
{
    [Header("Dropdown Reference")]
    [SerializeField] private TMP_Dropdown dropdown;

    [Header("Text Groups")]
    [Tooltip("List of GameObject groups to show/hide. Index matches dropdown option index.")]
    [SerializeField] private List<GameObject> textGroups = new List<GameObject>();

    [Header("Options")]
    [SerializeField] private bool hideAllOnStart = true;
    [SerializeField] private int defaultSelection = 0;

    private void Start()
    {
        if (dropdown == null)
        {
            Debug.LogError("[SimpleDropdownController] Dropdown not assigned!");
            return;
        }

        // Subscribe to dropdown value changes
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        // Hide all groups on start if enabled
        if (hideAllOnStart)
        {
            HideAllGroups();
        }

        // Show default selection
        ShowGroupAtIndex(defaultSelection);
        dropdown.value = defaultSelection;
    }

    /// <summary>
    /// Called when dropdown value changes
    /// </summary>
    private void OnDropdownValueChanged(int index)
    {
        Debug.Log($"[SimpleDropdownController] Dropdown changed to index: {index}");
        
        // Hide all groups
        HideAllGroups();
        
        // Show the selected group
        ShowGroupAtIndex(index);
    }

    /// <summary>
    /// Hide all text groups
    /// </summary>
    private void HideAllGroups()
    {
        foreach (var group in textGroups)
        {
            if (group != null)
            {
                group.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Show a specific group by index
    /// </summary>
    private void ShowGroupAtIndex(int index)
    {
        if (index >= 0 && index < textGroups.Count)
        {
            if (textGroups[index] != null)
            {
                textGroups[index].SetActive(true);
                Debug.Log($"[SimpleDropdownController] Showing group at index {index}: {textGroups[index].name}");
            }
            else
            {
                Debug.LogWarning($"[SimpleDropdownController] Text group at index {index} is null!");
            }
        }
        else
        {
            Debug.LogWarning($"[SimpleDropdownController] Index {index} out of range (0-{textGroups.Count - 1})");
        }
    }

    /// <summary>
    /// Manually change dropdown value (will trigger show/hide)
    /// </summary>
    public void SetDropdownValue(int index)
    {
        if (dropdown != null)
        {
            dropdown.value = index;
        }
    }

    /// <summary>
    /// Add a new text group dynamically
    /// </summary>
    public void AddTextGroup(GameObject group)
    {
        if (group != null && !textGroups.Contains(group))
        {
            textGroups.Add(group);
            group.SetActive(false); // Hide by default
            Debug.Log($"[SimpleDropdownController] Added text group: {group.name}");
        }
    }

    /// <summary>
    /// Remove a text group
    /// </summary>
    public void RemoveTextGroup(GameObject group)
    {
        if (textGroups.Remove(group))
        {
            Debug.Log($"[SimpleDropdownController] Removed text group: {group.name}");
        }
    }

    /// <summary>
    /// Get currently visible group
    /// </summary>
    public GameObject GetActiveGroup()
    {
        foreach (var group in textGroups)
        {
            if (group != null && group.activeSelf)
            {
                return group;
            }
        }
        return null;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }
    }

    // Editor helper
    [ContextMenu("Debug - Show All Groups")]
    private void DebugShowAll()
    {
        foreach (var group in textGroups)
        {
            if (group != null)
            {
                group.SetActive(true);
            }
        }
    }

    [ContextMenu("Debug - Hide All Groups")]
    private void DebugHideAll()
    {
        HideAllGroups();
    }
}