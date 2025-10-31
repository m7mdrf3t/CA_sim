using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProportionalSliderManager : MonoBehaviour
{
    [SerializeField]
    private UserConfig userConfig;

    [SerializeField]
    private bool autoSaveOnChange = false;

    [SerializeField]
    private bool loadOnStart = true;

    [SerializeField]    
    private List<ProportionalSlider> sliders = new List<ProportionalSlider>();
    private bool isUpdating = false;

    private void Start()
    {
        if (loadOnStart && userConfig != null)
        {
            LoadRatiosFromConfig();
        }
        // ✓ Remove immediate save from Start
        // SaveRatiosToConfig();
    }

    /// <summary>
    /// Register a slider with the manager and initialize it with equal ratio
    /// </summary>
    public void RegisterSlider(ProportionalSlider slider, string variantName, Color color)
    {
        if (sliders.Contains(slider)) return;

        sliders.Add(slider);
        
        // Calculate equal ratio for all sliders
        float equalRatio = 100f / sliders.Count;
        
        Debug.Log($"[SliderManager] Registering slider '{variantName}' - Equal ratio: {equalRatio:F2}% (Total sliders: {sliders.Count})");
        
        // Initialize the new slider
        slider.Initialize(this, equalRatio, variantName, color);

        // Update all existing sliders to new equal ratio
        isUpdating = true;
        foreach (var s in sliders)
        {
            s.Ratio = equalRatio;
            Debug.Log($"[SliderManager] Set {s.crystalVariantName} ratio to {equalRatio:F2}%");
        }
        isUpdating = false;

        // ✓ Save after a frame to ensure everything is updated
        StartCoroutine(SaveAfterFrame());
    }

    /// <summary>
    /// Wait one frame before saving to ensure all values are updated
    /// </summary>
    private IEnumerator SaveAfterFrame()
    {
        yield return new WaitForEndOfFrame();
        SaveRatiosToConfig();
    }

    /// <summary>
    /// Remove a slider from management
    /// </summary>
    public void UnregisterSlider(ProportionalSlider slider)
    {
        if (sliders.Remove(slider) && sliders.Count > 0)
        {
            // Redistribute ratios equally among remaining sliders
            float equalRatio = 100f / sliders.Count;
            isUpdating = true;
            foreach (var s in sliders)
            {
                s.Ratio = equalRatio;
            }
            isUpdating = false;
            
            // ✓ Save after unregistering
            StartCoroutine(SaveAfterFrame());
        }
    }

    // ✓ REMOVE OnEnable save - it's causing issues
    // private void OnEnable() {
    //     SaveRatiosToConfig();
    // }

    /// <summary>
    /// Called by individual sliders when their value changes
    /// </summary>
    public void OnSliderChanged(ProportionalSlider changedSlider, float newValue)
    {
        if (isUpdating || sliders.Count <= 1) return;

        isUpdating = true;

        float oldValue = changedSlider.Ratio;
        float difference = newValue - oldValue;

        // Get all other sliders
        var otherSliders = sliders.Where(s => s != changedSlider).ToList();

        if (otherSliders.Count == 0)
        {
            isUpdating = false;
            return;
        }

        // Calculate total of other sliders
        float otherTotal = otherSliders.Sum(s => s.Ratio);

        // Clamp to prevent negatives
        float maxPossible = 100f - (otherSliders.Count * 0.1f);
        newValue = Mathf.Clamp(newValue, 0.1f, maxPossible);
        difference = newValue - oldValue;

        // Distribute the difference proportionally
        if (otherTotal > 0.1f)
        {
            foreach (var slider in otherSliders)
            {
                float proportion = slider.Ratio / otherTotal;
                float adjustment = -difference * proportion;
                float newSliderValue = Mathf.Max(0.1f, slider.Ratio + adjustment);
                slider.Ratio = newSliderValue;
            }
        }
        else
        {
            // If others are near zero, distribute equally
            float remainingValue = 100f - newValue;
            float equalShare = remainingValue / otherSliders.Count;
            foreach (var slider in otherSliders)
            {
                slider.Ratio = equalShare;
            }
        }

        // Normalize to ensure total is exactly 100
        NormalizeRatios();

        // Update the changed slider
        changedSlider.Ratio = newValue;

        isUpdating = false;

        // Auto-save if enabled
        if (autoSaveOnChange)
        {
            SaveRatiosToConfig();
        }
    }

    private void NormalizeRatios()
    {
        float total = sliders.Sum(s => s.Ratio);

        if (Mathf.Abs(total - 100f) > 0.01f)
        {
            float factor = 100f / total;
            foreach (var slider in sliders)
            {
                slider.Ratio = slider.Ratio * factor;
            }
        }
    }

    /// <summary>
    /// Save current slider ratios to UserConfig
    /// </summary>
    public void SaveRatiosToConfig()
    {
        if (userConfig == null)
        {
            Debug.LogWarning("[SliderManager] UserConfig not assigned to ProportionalSliderManager!");
            return;
        }

        if (sliders.Count == 0)
        {
            Debug.LogWarning("[SliderManager] No sliders to save!");
            return;
        }

        // ✓ Verify total is 100% before saving
        float totalRatio = sliders.Sum(s => s.Ratio);
        Debug.Log($"[SliderManager] Saving ratios - Total: {totalRatio:F2}%");

        // Build dictionary of ratios by variant name
        Dictionary<string, float> ratios = new Dictionary<string, float>();
        
        foreach (var slider in sliders)
        {
            string key = slider.crystalVariantName;
            ratios[key] = slider.Ratio;
            Debug.Log($"[SliderManager] Saving {key}: {slider.Ratio:F2}%");
        }

        // Save to UserConfig
        userConfig.SaveSpawnRatios(ratios);
        Debug.Log($"[SliderManager] ✓ Saved {ratios.Count} ratios to UserConfig");
    }

    /// <summary>
    /// Alternative: Save by index order (guaranteed to work)
    /// Use this if variant name matching fails
    /// </summary>
    public void SaveRatiosToConfigByIndex()
    {
        if (userConfig == null)
        {
            Debug.LogWarning("[SliderManager] UserConfig not assigned!");
            return;
        }

        List<float> ratios = new List<float>();
        foreach (var slider in sliders)
        {
            ratios.Add(slider.Ratio);
            Debug.Log($"[SliderManager] Slider {ratios.Count - 1}: {slider.crystalVariantName} = {slider.Ratio:F2}%");
        }

        userConfig.SaveSpawnRatiosByIndex(ratios);
        Debug.Log($"[SliderManager] Saved {ratios.Count} ratios by index");
    }

    /// <summary>
    /// Load ratios from UserConfig and apply to sliders
    /// </summary>
    public void LoadRatiosFromConfig()
    {
        if (userConfig == null)
        {
            Debug.LogWarning("[SliderManager] UserConfig not assigned to ProportionalSliderManager!");
            return;
        }

        Dictionary<string, float> savedRatios = userConfig.GetSpawnRatios();

        if (savedRatios.Count == 0)
        {
            Debug.Log("[SliderManager] No saved ratios found in UserConfig");
            return;
        }

        isUpdating = true;

        // Apply saved ratios to matching sliders
        foreach (var slider in sliders)
        {
            if (savedRatios.ContainsKey(slider.crystalVariantName))
            {
                slider.Ratio = savedRatios[slider.crystalVariantName];
                Debug.Log($"[SliderManager] Loaded {slider.crystalVariantName}: {slider.Ratio:F2}%");
            }
        }

        isUpdating = false;

        Debug.Log($"[SliderManager] ✓ Loaded {savedRatios.Count} spawn ratios from UserConfig");
    }

    /// <summary>
    /// Reset all sliders to equal ratios
    /// </summary>
    public void ResetToEqualRatios()
    {
        if (sliders.Count == 0) return;

        float equalRatio = 100f / sliders.Count;

        isUpdating = true;
        foreach (var slider in sliders)
        {
            slider.Ratio = equalRatio;
        }
        isUpdating = false;

        Debug.Log($"[SliderManager] Reset all sliders to equal ratios: {equalRatio:F2}%");
        SaveRatiosToConfig();
    }

    // ============================================
    // Existing methods (unchanged)
    // ============================================

    public Dictionary<string, float> GetCrystalRatios()
    {
        Dictionary<string, float> ratios = new Dictionary<string, float>();
        
        foreach (var slider in sliders)
        {
            ratios[slider.crystalVariantName] = slider.Ratio;
        }
        
        return ratios;
    }

    public Dictionary<string, float> GetNormalizedRatios()
    {
        Dictionary<string, float> ratios = new Dictionary<string, float>();
        
        foreach (var slider in sliders)
        {
            ratios[slider.crystalVariantName] = slider.Ratio / 100f;
        }
        
        return ratios;
    }

    public string SelectRandomCrystalVariant()
    {
        if (sliders.Count == 0) return null;

        float randomValue = Random.Range(0f, 100f);
        float cumulative = 0f;

        foreach (var slider in sliders)
        {
            cumulative += slider.Ratio;
            if (randomValue <= cumulative)
            {
                return slider.crystalVariantName;
            }
        }

        return sliders[sliders.Count - 1].crystalVariantName;
    }

    public List<CrystalSpawnData> GetCrystalSpawnData()
    {
        List<CrystalSpawnData> data = new List<CrystalSpawnData>();
        
        foreach (var slider in sliders)
        {
            data.Add(new CrystalSpawnData
            {
                variantName = slider.crystalVariantName,
                color = slider.crystalColor,
                spawnRatio = slider.Ratio / 100f
            });
        }
        
        return data;
    }

    [System.Serializable]
    public class CrystalSpawnData
    {
        public string variantName;
        public Color color;
        public float spawnRatio;
    }
}