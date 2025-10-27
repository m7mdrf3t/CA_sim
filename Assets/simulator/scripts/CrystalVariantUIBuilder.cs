using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class CrystalVariantUIBuilder : MonoBehaviour
{
    [Header("Data / Config")]
    [SerializeField] private UserConfig config;

    [Header("UI Targets")]
    [SerializeField] private RectTransform buttonParent;     // Where to spawn the buttons
    [SerializeField] private Button buttonPrefab;            // (Optional) Prefab with an Image named "Icon" and a Text/TMP named "Label"

    [Header("Options")]
    [SerializeField] private bool clearExisting = true;      // Clear existing children before building
    [SerializeField] private Sprite fallbackIcon;            // Used if data.icon is null
    [SerializeField] private bool ensureLayout = true;       // Adds a GridLayoutGroup if none present

    /// <summary>
    /// Call this to (re)build all variant buttons.
    /// </summary>
    /// 
    [ContextMenu("Build Variant Buttons")]
    public void BuildVariantButtons()
    {
        if (config == null)
        {
            Debug.LogError("[CrystalVariantUIBuilder] Missing UserConfig reference.");
            return;
        }

        if (buttonParent == null)
        {
            Debug.LogError("[CrystalVariantUIBuilder] Missing buttonParent RectTransform.");
            return;
        }

        // Optional: clear old children
        if (clearExisting)
        {
            for (int i = buttonParent.childCount - 1; i >= 0; i--)
            {
                var child = buttonParent.GetChild(i);
#if UNITY_EDITOR
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
#else
                Destroy(child.gameObject);
#endif
            }
        }

        // Optional: make sure the parent has a layout so things look neat
        if (ensureLayout && buttonParent.GetComponent<LayoutGroup>() == null)
        {
            var grid = buttonParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(120, 120);
            grid.spacing = new Vector2(8, 8);
            grid.childAlignment = TextAnchor.UpperLeft;
        }

        // Load the data
        var variants = config.GetAllCrystalVariantData();
        Debug.Log($"[CrystalVariantUIBuilder] Building {variants.Count} crystal variant buttons.");

        foreach (var data in variants)
        {
            CreateOneButton(data);
        }
    }

    private void CreateOneButton(UserConfig.CrystalSpawnData data)
    {
        // 1) Create or use prefab
        Button btnInstance = buttonPrefab != null
            ? Instantiate(buttonPrefab, buttonParent)
            : CreateRuntimeButton(buttonParent);

        btnInstance.gameObject.name = $"CrystalBtn_{data.variantName}";

        // 2) Hook up the icon
        Image iconImg = null;

        // Preferred: a child explicitly named "Icon"
        var iconTr = btnInstance.transform.Find("Icon");
        if (iconTr != null) iconImg = iconTr.GetComponent<Image>();

        // Fallback: first Image under the button that is not the background
        if (iconImg == null)
        {
            foreach (var img in btnInstance.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject == btnInstance.gameObject) continue; // likely background
                iconImg = img;
                break;
            }
        }

        if (iconImg != null)
        {
            iconImg.sprite = data.icon != null ? data.icon : fallbackIcon;
            iconImg.enabled = iconImg.sprite != null;
            iconImg.preserveAspect = true;
        }

        // 3) Hook up the label
#if TMP_PRESENT
        TMP_Text label = null;
        var labelTr = btnInstance.transform.Find("Label");
        if (labelTr != null) label = labelTr.GetComponent<TMP_Text>();
        if (label == null) label = btnInstance.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = data.varientName ?? "Unnamed";
#else
        Text label = null;
        var labelTr = btnInstance.transform.Find("Label");
        if (labelTr != null) label = labelTr.GetComponent<Text>();
        if (label == null) label = btnInstance.GetComponentInChildren<Text>(true);
        if (label != null) label.text = data.variantName ?? "Unnamed";
#endif

        // 4) Wire up click
        btnInstance.onClick.RemoveAllListeners();
        btnInstance.onClick.AddListener(() => OnCrystalVariantClicked(data));
    }

    private void OnCrystalVariantClicked(UserConfig.CrystalSpawnData data)
    {
        // Do whatever you want when a variant is chosen
        // Example: log + call your existing creation flow
        Debug.Log($"[CrystalVariantUIBuilder] Selected variant: {data.variantName}");
        // You can invoke your CreateEndWeight or another handler here if needed.
        // e.g. SomeOtherComponent.SpawnFromVariant(data);
    }

    private Button CreateRuntimeButton(RectTransform parent)
    {
        // Background
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(120, 120);

        var bg = go.GetComponent<Image>();
        bg.raycastTarget = true;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        // Icon
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = new Vector2(0.15f, 0.35f);
        iconRT.anchorMax = new Vector2(0.85f, 0.95f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

        // Label
#if TMP_PRESENT
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var tmp = labelGO.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.fontSize = 18;
#else
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var tmp = labelGO.GetComponent<Text>();
        tmp.alignment = TextAnchor.MiddleCenter;
        tmp.resizeTextForBestFit = true;
        tmp.resizeTextMinSize = 10;
        tmp.resizeTextMaxSize = 20;
#endif
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.SetParent(rt, false);
        labelRT.anchorMin = new Vector2(0.05f, 0.0f);
        labelRT.anchorMax = new Vector2(0.95f, 0.30f);
        labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;

        return button;
    }
}