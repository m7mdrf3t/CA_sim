using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PriceCalculator : MonoBehaviour
{
    [SerializeField] private TMP_Text priceText;
    
    [Header("Optional: Assign if not using ConfigurationManager")]
    [SerializeField] private UserConfig userConfig;
    
    [Header("Update Settings")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 0.5f; // Update every 0.5 seconds
    
    private float lastUpdateTime;

    void Start()
    {
        // Try to get config from ConfigurationManager first
        if (userConfig == null && ConfigurationManager.Instance != null)
        {
            userConfig = ConfigurationManager.Instance.GetCurrentConfig();
            Debug.Log("[PriceCalculator] Using config from ConfigurationManager");
        }
        
        if (userConfig == null)
        {
            Debug.LogError("[PriceCalculator] No UserConfig assigned and ConfigurationManager not found!");
            return;
        }
        
        CalculatePrice();
    }

    void Update()
    {
        // Auto-update price periodically
        if (autoUpdate && Time.time - lastUpdateTime > updateInterval)
        {
            CalculatePrice();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Calculates and displays the total price
    /// Call this whenever selections change
    /// </summary>
    public void CalculatePrice()
    {
        // Always get fresh config from ConfigurationManager
        if (ConfigurationManager.Instance != null)
        {
            userConfig = ConfigurationManager.Instance.GetCurrentConfig();
        }
        
        if (userConfig == null)
        {
            Debug.LogWarning("[PriceCalculator] UserConfig is null!");
            if (priceText != null)
            {
                priceText.text = "0.00 EGP";
            }
            return;
        }
        
        float totalPrice = userConfig.GetTotalPrice();
        userConfig.finalPrice = totalPrice;
        
        if (priceText != null)
        {
            priceText.text = totalPrice.ToString("F2") + " EGP";
        }
        
        Debug.Log($"[PriceCalculator] Total Price: {totalPrice:F2} EGP");
    }
    
    /// <summary>
    /// Force immediate price recalculation
    /// Call this after adding/removing crystals or changing ratios
    /// </summary>
    public void ForceUpdate()
    {
        CalculatePrice();
    }
}