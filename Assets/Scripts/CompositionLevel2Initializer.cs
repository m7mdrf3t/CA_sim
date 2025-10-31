using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class CompositionLevel2Initializer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI headerTitle; // "Central"
    [SerializeField] private Image heroPreview;           // big image

    [Header("Optional: initialize gallery for the chosen category")]
    [SerializeField] private GalleryVideoController gallery;
    [SerializeField] private CategoryClips[] categoryClipSets;

    [System.Serializable]
    public class CategoryClips
    {
        public string categoryName;
        public VideoClip[] clips;
        public Sprite[] buttonSprites; // optional (thumbnails for gallery buttons)
    }

    private void OnEnable()
    {
        Debug.Log($"[Level2Initializer] Initializing with category: {SelectionBus.SelectedCategoryTitle} (index: {SelectionBus.SelectedCategoryIndex})");
        
        // Set header title
        if (headerTitle) 
        {
            headerTitle.text = SelectionBus.SelectedCategoryTitle ?? "Category";
            Debug.Log($"[Level2Initializer] Header title set to: {headerTitle.text}");
        }
        
        // Set hero preview image
        if (heroPreview && SelectionBus.SelectedCategorySprite) 
        {
            heroPreview.sprite = SelectionBus.SelectedCategorySprite;
            Debug.Log($"[Level2Initializer] Hero image set to: {SelectionBus.SelectedCategorySprite.name}");
        }
        else if (heroPreview)
        {
            Debug.LogWarning("[Level2Initializer] Hero preview image not set - no sprite available");
        }

        // Initialize gallery for the chosen category
        if (gallery != null &&
            SelectionBus.SelectedCategoryIndex >= 0 &&
            SelectionBus.SelectedCategoryIndex < categoryClipSets.Length)
        {
            var set = categoryClipSets[SelectionBus.SelectedCategoryIndex];
            gallery.SetClips(set.clips, set.buttonSprites);
            Debug.Log($"[Level2Initializer] Gallery initialized with {set.clips.Length} clips for category: {set.categoryName}");
        }
        else if (gallery != null)
        {
            Debug.LogWarning($"[Level2Initializer] Gallery not initialized - invalid category index: {SelectionBus.SelectedCategoryIndex}");
        }
    }
}