using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level2HeroGalleryController : MonoBehaviour
{
    [Header("Left: Hero area")]
    [SerializeField] private Image heroImage;
    [SerializeField] private Button heroClickToNext;   // can be the heroImage button (or a transparent button on top)
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Right: Gallery area")]
    [SerializeField] private TextMeshProUGUI galleryTitle;  // e.g., "Crystal X — Vertical"
    [SerializeField] private Transform galleryGridParent;   // parent container
    [SerializeField] private GameObject galleryItemPrefab;  // a prefab with an Image and an optional label/button

    [Header("Data")]
    [SerializeField] private HeroCategory[] heroCategories; // list of categories for the hero

    [Serializable]
    public class HeroCategory
    {
        public string displayName;         // e.g., "Vertical"
        public Sprite heroSprite;          // the big left image
        public ShapeItem[] shapes;         // thumbnails to show in right grid
    }

    [Serializable]
    public class ShapeItem
    {
        public string shapeName;           // e.g., "Shape A"
        public Sprite shapeSprite;         // thumbnail sprite
        // (optionally add id or scene/model reference)
    }

    private int _heroIndex = 0;
    private string _baseCrystalName = "Crystal";

    private void Awake()
    {
        // Pull the crystal name from Level 1 selection
        if (!string.IsNullOrEmpty(SelectionBus.SelectedCrystalName))
            _baseCrystalName = SelectionBus.SelectedCrystalName;

        // Wire buttons
        if (prevButton) prevButton.onClick.AddListener(PrevHero);
        if (nextButton) nextButton.onClick.AddListener(NextHero);
        if (heroClickToNext) heroClickToNext.onClick.AddListener(NextHero);
    }

    private void OnEnable()
    {
        Debug.Log($"[Level2HeroGallery] OnEnable - SelectedCategoryIndex: {SelectionBus.SelectedCategoryIndex}");
        
        // Use the carousel selection instead of crystal selection
        if (SelectionBus.SelectedCategoryIndex >= 0)
        {
            _heroIndex = Mathf.Clamp(SelectionBus.SelectedCategoryIndex, 0, Mathf.Max(0, heroCategories.Length - 1));
            Debug.Log($"[Level2HeroGallery] Using carousel selection, heroIndex: {_heroIndex}");
        }
        else
        {
            _heroIndex = Mathf.Clamp(SelectionBus.SelectedCrystalIndex, 0, Mathf.Max(0, heroCategories.Length - 1));
            Debug.Log($"[Level2HeroGallery] Using crystal selection, heroIndex: {_heroIndex}");
        }
        
        ApplyHero(_heroIndex, rebuildGrid: true);
    }

    private void OnDestroy()
    {
        if (prevButton) prevButton.onClick.RemoveListener(PrevHero);
        if (nextButton) nextButton.onClick.RemoveListener(NextHero);
        if (heroClickToNext) heroClickToNext.onClick.RemoveListener(NextHero);
    }

    private void PrevHero()
    {
        if (heroCategories == null || heroCategories.Length == 0) return;
        _heroIndex = (_heroIndex - 1 + heroCategories.Length) % heroCategories.Length;
        ApplyHero(_heroIndex, rebuildGrid: true);
    }

    private void NextHero()
    {
        if (heroCategories == null || heroCategories.Length == 0) return;
        _heroIndex = (_heroIndex + 1) % heroCategories.Length;
        ApplyHero(_heroIndex, rebuildGrid: true);
    }

    private void ApplyHero(int index, bool rebuildGrid)
    {
        if (heroCategories == null || heroCategories.Length == 0) 
        {
            Debug.LogWarning("[Level2HeroGallery] No hero categories configured!");
            return;
        }
        index = Mathf.Clamp(index, 0, heroCategories.Length - 1);

        var cat = heroCategories[index];
        Debug.Log($"[Level2HeroGallery] Applying hero {index}: {cat.displayName}, sprite: {(cat.heroSprite != null ? cat.heroSprite.name : "NULL")}");

        // Set left big image
        if (heroImage) 
        {
            heroImage.sprite = cat.heroSprite;
            Debug.Log($"[Level2HeroGallery] Hero image sprite set to: {(cat.heroSprite != null ? cat.heroSprite.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning("[Level2HeroGallery] Hero image component not assigned!");
        }

        // Title: "<CrystalName> — <Category>"
        if (galleryTitle)
        {
            string catName = string.IsNullOrEmpty(cat.displayName) ? $"Type {index + 1}" : cat.displayName;
            galleryTitle.text = $"{_baseCrystalName} — {catName}";
            Debug.Log($"[Level2HeroGallery] Gallery title set to: {galleryTitle.text}");
        }

        // Rebuild right grid
        if (rebuildGrid) RebuildGrid(cat);
    }

    private void RebuildGrid(HeroCategory cat)
    {
        if (galleryGridParent == null || galleryItemPrefab == null) return;

        // Clear existing
        for (int i = galleryGridParent.childCount - 1; i >= 0; i--)
            Destroy(galleryGridParent.GetChild(i).gameObject);

        // Add shapes
        if (cat.shapes == null) return;

        for (int i = 0; i < cat.shapes.Length; i++)
        {
            var s = cat.shapes[i];
            var go = Instantiate(galleryItemPrefab, galleryGridParent);

            // Try find Image + TMP on the prefab
            var img = go.GetComponentInChildren<Image>(true);
            if (img) img.sprite = s.shapeSprite;

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label) label.text = s.shapeName;

            // If the prefab has a Button, wire a click
            var btn = go.GetComponentInChildren<Button>(true);
            if (btn)
            {
                int captured = i;
                btn.onClick.AddListener(() => OnShapeClicked(cat, captured));
            }
        }
    }

    private void OnShapeClicked(HeroCategory cat, int shapeIndex)
    {
        // TODO: Open shape detail, add to cart, preview, etc.
        Debug.Log($"Clicked shape '{cat.shapes[shapeIndex].shapeName}' in category '{cat.displayName}' of '{_baseCrystalName}'");
    }
}