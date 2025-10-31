// ============================================
// FILE 3: CrystalVariantUIBuilder.cs (Modified)
// Your existing script with slider integration
// ============================================

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class CrystalVariantUIBuilder : MonoBehaviour
{
    [Header("Data / Config")]
    [SerializeField] private UserConfig config;
    [SerializeField] private CrystalDataManager crystalDataManager;

    [Header("UI Targets")]
    [SerializeField] private RectTransform buttonParent;     // Where to spawn the buttons
    [SerializeField] private Button buttonPrefab;            // Prefab should have ProportionalSlider component!

    [SerializeField] private Vector2 iconSize = new Vector2(120, 120);

    [Header("Slider Manager")]
    [SerializeField] private ProportionalSliderManager sliderManager; // The manager component

    [Header("Options")]
    [SerializeField] private bool clearExisting = true;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private bool ensureLayout = true;

    private void Start()
    {
        BuildVariantButtons();
    }

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

        // Clear old children
        if (clearExisting)
        {
            ClearChildren(buttonParent);
        }

        // Load the data
        var variants = config.GetAllCrystalVariantData();
        Debug.Log($"[CrystalVariantUIBuilder] Building {variants.Count} crystal variant buttons.");

        foreach (var data in variants)
        {
            CreateOneButton(data);

            crystalDataManager.SetCrystalColor(0, data.color.ToString());
            crystalDataManager.SetSelectedCrystal(0, data.variantName);
        }
    }

    private void CreateOneButton(UserConfig.CrystalSpawnData data)
    {
        // Instantiate the button prefab (which includes slider)
        Button btnInstance = buttonPrefab != null
            ? Instantiate(buttonPrefab, buttonParent)
            : CreateRuntimeButton(buttonParent);

        btnInstance.gameObject.name = $"CrystalBtn_{data.variantName}";

        // Hook up icon
        Image iconImg = null;
        var iconTr = btnInstance.transform.Find("Icon");
        if (iconTr != null) iconImg = iconTr.GetComponent<Image>();

        if (iconImg == null)
        {
            foreach (var img in btnInstance.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject == btnInstance.gameObject) continue;
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

        TMP_Text nameValueLable = null;
        var nameValueLableTr = btnInstance.GetComponentInChildren<TMP_Text>(true); ; 
        nameValueLableTr.text = data.crystalType.ToString() ?? "Unnamed";


        // Hook up slider (ProportionalSlider component on the prefab)
        ProportionalSlider propSlider = btnInstance.GetComponent<ProportionalSlider>();
        if (propSlider == null)
        {
            propSlider = btnInstance.GetComponentInChildren<ProportionalSlider>();
        }

        if (propSlider != null && sliderManager != null)
        {
            sliderManager.RegisterSlider(propSlider, data.variantName, data.color);
        }
        else if (sliderManager == null)
        {
            Debug.LogWarning("[CrystalVariantUIBuilder] SliderManager is null. Assign it in the inspector!");
        }
        else
        {
            Debug.LogWarning($"[CrystalVariantUIBuilder] Button prefab for {data.variantName} doesn't have ProportionalSlider component!");
        }

        // Wire up click
        btnInstance.onClick.RemoveAllListeners();
        btnInstance.onClick.AddListener(() => OnCrystalVariantClicked(data));
    }

    private void OnCrystalVariantClicked(UserConfig.CrystalSpawnData data)
    {
        Debug.Log($"[CrystalVariantUIBuilder] Selected variant: {data.variantName}");
        
        // You can get the current ratios here
        if (sliderManager != null)
        {
            var ratios = sliderManager.GetCrystalRatios();
            if (ratios.ContainsKey(data.variantName))
            {
                Debug.Log($"Current spawn ratio for {data.variantName}: {ratios[data.variantName]}%");
            }
        }
    }

    private void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
#if UNITY_EDITOR
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    private Button CreateRuntimeButton(RectTransform parent)
    {
        var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = iconSize;

        var bg = go.GetComponent<Image>();
        bg.raycastTarget = true;

        var button = go.GetComponent<Button>();
        var colors = button.colors;
        colors.fadeDuration = 0.05f;
        button.colors = colors;

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.SetParent(rt, false);
        iconRT.anchorMin = new Vector2(0.15f, 0.35f);
        iconRT.anchorMax = new Vector2(0.85f, 0.95f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

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

    // Public method to get spawn ratios (use this in your crystal spawning code)
    public Dictionary<string, float> GetCrystalSpawnRatios()
    {
        if (sliderManager != null)
        {
            return sliderManager.GetNormalizedRatios();
        }
        return new Dictionary<string, float>();
    }

    // Public method to select random crystal based on ratios
    public string SelectRandomCrystalVariant()
    {
        if (sliderManager != null)
        {
            return sliderManager.SelectRandomCrystalVariant();
        }
        return null;
    }
}
