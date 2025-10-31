// ============================================
// GalleryPagination.cs
// Displays items in pages with left/right navigation
// Each page shows 6 items in a 3x2 grid
// ============================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class GalleryPagination : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentParent; // Where items are displayed
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    
    [Header("Page Info Display (Optional)")]
    [SerializeField] private Text pageInfoText;
#if TMP_PRESENT
    [SerializeField] private TMP_Text pageInfoTextTMP;
#endif

    [Header("Grid Settings")]
    [SerializeField] private int itemsPerPage = 6;
    [SerializeField] private int columns = 3;
    [SerializeField] private int rows = 2;
    [SerializeField] private Vector2 cellSize = new Vector2(150, 150);
    [SerializeField] private Vector2 spacing = new Vector2(10, 10);

    [Header("Options")]
    [SerializeField] private bool loopPages = false; // Enable to loop from last to first page
    [SerializeField] private bool hideButtonsWhenDisabled = true;

    // Internal state
    private List<GameObject> allItems = new List<GameObject>();
    private int currentPage = 0;
    private int totalPages = 0;

    private void Start()
    {
        // Setup buttons
        if (leftButton != null)
        {
            leftButton.onClick.AddListener(PreviousPage);
        }

        if (rightButton != null)
        {
            rightButton.onClick.AddListener(NextPage);
        }

        // Setup grid layout on content parent
        SetupGridLayout();
    }

    /// <summary>
    /// Initialize gallery with items
    /// </summary>
    public void Initialize(List<GameObject> items)
    {
        ClearGallery();
        
        allItems = new List<GameObject>(items);
        
        // Calculate total pages
        totalPages = Mathf.CeilToInt((float)allItems.Count / itemsPerPage);
        
        Debug.Log($"[GalleryPagination] Initialized with {allItems.Count} items, {totalPages} pages");
        
        // Parent all items to content
        foreach (var item in allItems)
        {
            item.transform.SetParent(contentParent, false);
        }
        
        // Show first page
        currentPage = 0;
        ShowCurrentPage();
    }

    /// <summary>
    /// Add a single item to the gallery
    /// </summary>
    public void AddItem(GameObject item)
    {
        allItems.Add(item);
        item.transform.SetParent(contentParent, false);
        
        // Recalculate pages
        totalPages = Mathf.CeilToInt((float)allItems.Count / itemsPerPage);
        
        ShowCurrentPage();
    }

    /// <summary>
    /// Remove an item from the gallery
    /// </summary>
    public void RemoveItem(GameObject item)
    {
        if (allItems.Remove(item))
        {
            Destroy(item);
            
            // Recalculate pages
            totalPages = Mathf.CeilToInt((float)allItems.Count / itemsPerPage);
            
            // Clamp current page
            if (currentPage >= totalPages && totalPages > 0)
            {
                currentPage = totalPages - 1;
            }
            
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// Clear all items from gallery
    /// </summary>
    public void ClearGallery()
    {
        foreach (var item in allItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        
        allItems.Clear();
        currentPage = 0;
        totalPages = 0;
        
        UpdateUI();
    }

    /// <summary>
    /// Navigate to next page
    /// </summary>
    public void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            ShowCurrentPage();
        }
        else if (loopPages && totalPages > 0)
        {
            currentPage = 0;
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// Navigate to previous page
    /// </summary>
    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowCurrentPage();
        }
        else if (loopPages && totalPages > 0)
        {
            currentPage = totalPages - 1;
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// Go to a specific page (0-indexed)
    /// </summary>
    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < totalPages)
        {
            currentPage = pageIndex;
            ShowCurrentPage();
        }
    }

    /// <summary>
    /// Show items for the current page
    /// </summary>
    private void ShowCurrentPage()
    {
        if (allItems.Count == 0)
        {
            UpdateUI();
            return;
        }

        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allItems.Count);

        // Hide all items
        for (int i = 0; i < allItems.Count; i++)
        {
            if (allItems[i] != null)
            {
                allItems[i].SetActive(false);
            }
        }

        // Show items for current page
        for (int i = startIndex; i < endIndex; i++)
        {
            if (allItems[i] != null)
            {
                allItems[i].SetActive(true);
            }
        }

        Debug.Log($"[GalleryPagination] Showing page {currentPage + 1}/{totalPages} (items {startIndex + 1}-{endIndex})");

        UpdateUI();
    }

    /// <summary>
    /// Update navigation buttons and page info
    /// </summary>
    private void UpdateUI()
    {
        // Update left button
        if (leftButton != null)
        {
            bool canGoLeft = currentPage > 0 || (loopPages && totalPages > 1);
            leftButton.interactable = canGoLeft;
            
            if (hideButtonsWhenDisabled)
            {
                leftButton.gameObject.SetActive(canGoLeft);
            }
        }

        // Update right button
        if (rightButton != null)
        {
            bool canGoRight = currentPage < totalPages - 1 || (loopPages && totalPages > 1);
            rightButton.interactable = canGoRight;
            
            if (hideButtonsWhenDisabled)
            {
                rightButton.gameObject.SetActive(canGoRight);
            }
        }

        // Update page info text
        string pageInfo = totalPages > 0 ? $"Page {currentPage + 1} / {totalPages}" : "No items";
        
        if (pageInfoText != null)
        {
            pageInfoText.text = pageInfo;
        }

#if TMP_PRESENT
        if (pageInfoTextTMP != null)
        {
            pageInfoTextTMP.text = pageInfo;
        }
#endif
    }

    /// <summary>
    /// Setup grid layout on content parent
    /// </summary>
    private void SetupGridLayout()
    {
        if (contentParent == null) return;

        // Remove existing layout group
        var existingLayout = contentParent.GetComponent<LayoutGroup>();
        if (existingLayout != null)
        {
            Destroy(existingLayout);
        }

        // Add GridLayoutGroup
        var grid = contentParent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = cellSize;
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        Debug.Log($"[GalleryPagination] Setup grid: {columns}x{rows}, cell size: {cellSize}");
    }

    /// <summary>
    /// Get current page index (0-indexed)
    /// </summary>
    public int GetCurrentPage()
    {
        return currentPage;
    }

    /// <summary>
    /// Get total number of pages
    /// </summary>
    public int GetTotalPages()
    {
        return totalPages;
    }

    /// <summary>
    /// Get items on current page
    /// </summary>
    public List<GameObject> GetCurrentPageItems()
    {
        List<GameObject> pageItems = new List<GameObject>();
        
        int startIndex = currentPage * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, allItems.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            if (allItems[i] != null)
            {
                pageItems.Add(allItems[i]);
            }
        }

        return pageItems;
    }

    /// <summary>
    /// Refresh the current page display
    /// </summary>
    [ContextMenu("Refresh Page")]
    public void RefreshPage()
    {
        ShowCurrentPage();
    }

    /// <summary>
    /// Get all items in the gallery
    /// </summary>
    public List<GameObject> GetAllItems()
    {
        return new List<GameObject>(allItems);
    }

    // Keyboard shortcuts (optional)
}