using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Quick helper to set up gallery grid for 8-composition 2
/// Attach this to the GameObject that should become your gallery grid parent
/// Then click the Context Menu option to auto-configure
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class QuickGallerySetup : MonoBehaviour
{
    [Header("Setup Helper")]
    [SerializeField] private GameObject galleryItemPrefab;
    [SerializeField] private int columns = 3;
    [SerializeField] private float spacing = 10f;
    
    [ContextMenu("Setup Grid Layout")]
    public void SetupGridLayout()
    {
        var gridLayout = GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        }
        
        var rectTransform = GetComponent<RectTransform>();
        float cellSize = (rectTransform.rect.width - (columns - 1) * spacing) / columns;
        
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        
        Debug.Log($"Grid Layout configured: {columns} columns, cell size: {cellSize}x{cellSize}");
    }
    
    [ContextMenu("Create Simple Gallery Item Prefab")]
    public void CreateSimplePrefab()
    {
        if (galleryItemPrefab != null)
        {
            Debug.Log($"Gallery item prefab already exists: {galleryItemPrefab.name}");
            return;
        }
        
        // Create a simple image prefab
        GameObject prefabObj = new GameObject("GalleryItem");
        var rectTransform = prefabObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 200);
        
        var image = prefabObj.AddComponent<Image>();
        image.color = Color.white;
        
        // TODO: Save as prefab (user needs to manually do this in Unity)
        Debug.Log("Created GalleryItem GameObject. Please drag it to your Prefabs folder to make it a prefab, then assign it to 'Old Gallery Item Prefab' in CompositionCategoryAutoInitializer.");
        
        #if UNITY_EDITOR
        if (Selection.activeGameObject != prefabObj)
        {
            // Select the created prefab in hierarchy
            EditorUtility.FocusProjectWindow();
            Selection.activeGameObject = prefabObj;
        }
        #endif
    }
}

