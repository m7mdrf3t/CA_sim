using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedItemsUI : MonoBehaviour
{
    [Header("Data source")]
    [SerializeField] private GalleryVideoController gallery;  // reference to the gallery controller

    [Tooltip("Clip names shown in the list (same order as GalleryVideoController.clips).")]
    [SerializeField] private string[] crystalNames;

    [Tooltip("Icons shown in the list (same order as names/clips).")]
    [SerializeField] private Sprite[] crystalIcons;

    [Header("UI Wiring")]
    [SerializeField] private Button selectButton;               // your big 'Select' button
    [SerializeField] private Transform listParent;              // Selected_Items (the container)
    [SerializeField] private GameObject itemPrefab;             // your Item_0 prefab
    [SerializeField] private TextMeshProUGUI counterLabel;      // 'Selected 0/4'

    [Header("Rules")]
    [SerializeField] private int maxSelections = 4;
    [SerializeField] private bool preventDuplicates = true;

    // internal state
    private readonly List<int> selectedIndices = new List<int>();

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);

        UpdateCounter();
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(OnSelectClicked);
    }

    private void OnSelectClicked()
    {
        if (gallery == null)
        {
            Debug.LogWarning("[SelectedItemsUI] No GalleryVideoController assigned.");
            return;
        }

        int idx = gallery.CurrentIndex;
        if (idx < 0)
        {
            Debug.LogWarning("[SelectedItemsUI] No current gallery index (maybe external clip).");
            return;
        }

        if (preventDuplicates && selectedIndices.Contains(idx))
        {
            Debug.Log("[SelectedItemsUI] Item already selected.");
            return;
        }

        if (selectedIndices.Count >= maxSelections)
        {
            Debug.Log("[SelectedItemsUI] Reached max selections.");
            return;
        }

        // Display data
        string name = SafeName(idx);

        // Prefer explicit array; if null, use gallery button sprite
        Sprite icon = SafeIcon(idx);
        if (icon == null)
        {
            icon = gallery.GetButtonSprite(idx);
        }

        // Instantiate row
        GameObject row = Instantiate(itemPrefab, listParent);

        // Fill icon (supports Image or RawImage) + text
        var iconObj = row.transform.Find("Crystal_Icon");
        if (iconObj != null)
        {
            var img = iconObj.GetComponent<Image>();
            var raw = iconObj.GetComponent<RawImage>();

            if (img != null)
            {
                img.sprite = icon;
                img.preserveAspect = true;
            }
            else if (raw != null)
            {
                if (icon != null && icon.texture != null)
                {
                    raw.texture = icon.texture;
                    raw.SetNativeSize();
                }
            }
            else
            {
                Debug.LogWarning("[SelectedItemsUI] 'Crystal_Icon' needs an Image or RawImage.");
            }
        }
        else
        {
            Debug.LogWarning("[SelectedItemsUI] Could not find 'Crystal_Icon' under the item prefab.");
        }

        var nameText = row.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null) nameText.text = name;

        // Wire X to remove
        var xBtn = row.transform.Find("X")?.GetComponent<Button>();
        if (xBtn != null)
        {
            xBtn.onClick.AddListener(() =>
            {
                selectedIndices.Remove(idx);
                Destroy(row);
                UpdateCounter();
            });
        }

        selectedIndices.Add(idx);
        UpdateCounter();
    }

    private string SafeName(int index)
    {
        if (crystalNames != null && index >= 0 && index < crystalNames.Length && !string.IsNullOrEmpty(crystalNames[index]))
            return crystalNames[index];

        // Fallback to gallery clip name
        string clipName = gallery != null ? gallery.GetClipName(index) : null;
        if (!string.IsNullOrEmpty(clipName)) return clipName;

        return $"Crystal {index + 1}";
    }

    private Sprite SafeIcon(int index)
    {
        if (crystalIcons != null && index >= 0 && index < crystalIcons.Length)
            return crystalIcons[index];
        return null;
    }

    private void UpdateCounter()
    {
        if (counterLabel != null)
            counterLabel.text = $"Selected {selectedIndices.Count}/{maxSelections}";
    }
}