using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedItemsUI : MonoBehaviour
{
    [Header("Data source")]
    [SerializeField] private GalleryVideoController gallery;

    [Tooltip("Clip names shown in the list (same order as GalleryVideoController.clips).")]
    [SerializeField] private string[] crystalNames;

    [Tooltip("Icons shown in the list (same order as names/clips).")]
    [SerializeField] private Sprite[] crystalIcons;

    [Header("UI Wiring")]
    [SerializeField] private Button selectButton;
    [SerializeField] private Transform listParent;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private TextMeshProUGUI counterLabel;

    [Header("Rules")]
    [SerializeField] private int maxSelections = 4;
    [SerializeField] private bool preventDuplicates = true;

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

        int idx = gallery.ResolveCurrentIndex(); // ✅ always valid now
        if (idx < 0)
        {
            Debug.LogWarning("[SelectedItemsUI] No current or last gallery index found.");
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

        string name = SafeName(idx);
        Sprite icon = SafeIcon(idx) ?? gallery.GetButtonSprite(idx);

        GameObject row = Instantiate(itemPrefab, listParent);

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
        }

        var nameText = row.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null) nameText.text = name;

        var xBtn = row.transform.Find("X")?.GetComponent<Button>();
        if (xBtn != null)
        {
            xBtn.onClick.AddListener(() =>
            {
                int listIndex = selectedIndices.IndexOf(idx);
                selectedIndices.Remove(idx);
                
                // Sync with SelectionBus
                if (listIndex >= 0 && listIndex < SelectionBus.SelectedCrystalIndices.Count)
                {
                    SelectionBus.SelectedCrystalIndices.RemoveAt(listIndex);
                    if (listIndex < SelectionBus.SelectedCrystalSprites.Count)
                        SelectionBus.SelectedCrystalSprites.RemoveAt(listIndex);
                    if (listIndex < SelectionBus.SelectedCrystalNames.Count)
                        SelectionBus.SelectedCrystalNames.RemoveAt(listIndex);
                }
                
                // Save to persistent storage
                SelectionBus.SaveSelections();
                
                Destroy(row);
                UpdateCounter();
            });
        }

        selectedIndices.Add(idx);
        
        // Clear first to avoid accumulation
        // This ensures we only save the currently selected items
        SelectionBus.SelectedCrystalIndices.Clear();
        SelectionBus.SelectedCrystalSprites.Clear();
        SelectionBus.SelectedCrystalNames.Clear();
        
        // Rebuild the lists from current selections
        foreach (int selectionIdx in selectedIndices)
        {
            string selName = SafeName(selectionIdx);
            Sprite selIcon = SafeIcon(selectionIdx) ?? gallery.GetButtonSprite(selectionIdx);
            
            SelectionBus.SelectedCrystalIndices.Add(selectionIdx);
            SelectionBus.SelectedCrystalSprites.Add(selIcon);
            SelectionBus.SelectedCrystalNames.Add(selName);
        }
        
        // Save to persistent storage
        SelectionBus.SaveSelections();
        
        UpdateCounter();
    }

    private string SafeName(int index)
    {
        if (crystalNames != null && index >= 0 && index < crystalNames.Length && !string.IsNullOrEmpty(crystalNames[index]))
            return crystalNames[index];

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

        OnSelectionCountChanged?.Invoke(selectedIndices.Count);
    }

    // 🔹 Accessors & events
    public int SelectionCount => selectedIndices.Count;
    public IReadOnlyList<int> SelectedIndices => selectedIndices;
    public event System.Action<int> OnSelectionCountChanged;
}