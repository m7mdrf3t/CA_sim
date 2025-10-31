using UnityEngine;
using TMPro;
using System.Text;

public class CardDataLoader : MonoBehaviour
{
    [Header("Configuration Reference")]
    [SerializeField] private UserConfig userConfig;

    [SerializeField] private CardData cardData;
    

    [Header("TextMeshPro Fields - Basic Info")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI salesmanIDText;

    [Header("TextMeshPro Fields - Crystal Info")]
    [SerializeField] private TextMeshProUGUI numberOfCrystalsText;
    [SerializeField] private TextMeshProUGUI selectedCrystalsTexts;
    [SerializeField] private TextMeshProUGUI crystalColorsTexts;

    [Header("TextMeshPro Fields - Design Details")]
    [SerializeField] private TextMeshProUGUI compStyleText;
    [SerializeField] private TextMeshProUGUI numberOfWiresText;
    [SerializeField] private TextMeshProUGUI baseShapeText;


    [SerializeField] private TextMeshProUGUI totalprice;



    [Header("Optional - Summary Text")]
    [SerializeField] private TextMeshProUGUI summaryText;

    private void Start()
    {
        // Get userConfig from ConfigurationManager if not assigned
        if (userConfig == null && ConfigurationManager.Instance != null)
        {
            userConfig = ConfigurationManager.Instance.GetCurrentConfig();
        }

        LoadAllData();
    }

    // Load all data at once
    public void LoadAllData()
    {
        if (userConfig == null)
        {
            Debug.LogError("[CardDataLoader] UserConfig reference is missing!");
            return;
        }

        LoadBasicInfo();
        LoadCrystalInfo();
        LoadDesignDetails();
        LoadSummary();

        Debug.Log("[CardDataLoader] All data loaded from UserConfig!");
    }

    // Load basic information from userConfig
    public void LoadBasicInfo()
    {
        if (userConfig == null) return;

        if (nameText != null)
            nameText.text = userConfig.clientName;

        if (salesmanIDText != null)
            salesmanIDText.text = cardData.SalesmanID; // UserConfig doesn't have salesman ID
    }

    // Load crystal information from userConfig
    public void LoadCrystalInfo()
    {
        if (userConfig == null) return;

        // Load total number of crystals (points)
        if (numberOfCrystalsText != null)
        {
            if(userConfig.densityLevel == DensityLevel.Medium)
            {
                float _numberOfCrystals = userConfig.totalPoints * 0.75f;
                numberOfCrystalsText.text = _numberOfCrystals.ToString("F0");
            }else if (userConfig.densityLevel == DensityLevel.Low)
            {
                float _numberOfCrystals = userConfig.totalPoints * 0.50f;
                numberOfCrystalsText.text = _numberOfCrystals.ToString("F0");
            }
            else
            {
                numberOfCrystalsText.text = userConfig.totalPoints.ToString("F0");

            }
            
        }

        // Load selected crystals names
        if (selectedCrystalsTexts != null)
        {
            StringBuilder crystalsText = new StringBuilder();
            var selections = userConfig.GetEnabledSelections();

            for (int i = 0; i < selections.Count; i++)
            {
                var spawnData = userConfig.GetSpawnDataForSelection(selections[i]);
                crystalsText.AppendLine($"{i + 1}. {spawnData.crystalType}");
            }

            selectedCrystalsTexts.text = crystalsText.ToString().TrimEnd();
            
            if (selections.Count == 0)
            {
                selectedCrystalsTexts.text = "No crystals selected";
            }
        }

        // Load crystal colors
        if (crystalColorsTexts != null)
        {
            var selections = userConfig.GetEnabledSelections();

            var spawnData = userConfig.GetSpawnDataForSelection(selections[0]);            
                        // Display color name and apply color to text
            crystalColorsTexts.text += $"{spawnData.variantName}";
            crystalColorsTexts.color += spawnData.color;
        }
    }

    // Load design details from userConfig
    public void LoadDesignDetails()
    {
        if (userConfig == null) return;

        // Composition style (from colorStyle enum)
        if (compStyleText != null)
        {
            compStyleText.text = userConfig.colorStyle.ToString();
        }

        // Number of wires (total points)
        if (numberOfWiresText != null)
        {
             if(userConfig.densityLevel == DensityLevel.Medium)
            {
                float _numberOfCrystals = userConfig.totalPoints * 0.75f;
                numberOfWiresText.text = _numberOfCrystals.ToString("F0");
            }else if (userConfig.densityLevel == DensityLevel.Low)
            {
                float _numberOfCrystals = userConfig.totalPoints * 0.50f;
                numberOfWiresText.text = _numberOfCrystals.ToString("F0");
            }
            else
            {
                numberOfWiresText.text = userConfig.totalPoints.ToString("F0");

            }
        }

        // Base shape
        if (baseShapeText != null)
        {
            baseShapeText.text = userConfig.BaseType.ToString();
        }

        if (totalprice != null)
        {
            totalprice.text = userConfig.finalPrice.ToString("F2");
        }
    }

    // Load summary
    public void LoadSummary()
    {
        if (userConfig == null || summaryText == null) return;

        StringBuilder summary = new StringBuilder();
        
        // Configuration name
        summary.AppendLine($"<b>Configuration:</b> {userConfig.configurationName}");
        summary.AppendLine();
        
        // Dimensions
        summary.AppendLine($"<b>Dimensions:</b>");
        summary.AppendLine($"Width: {userConfig.xSize:F2}m");
        summary.AppendLine($"Height: {userConfig.ySize:F2}m");
        summary.AppendLine($"Depth: {userConfig.zSize:F2}m");
        summary.AppendLine($"Area: {userConfig.area:F2}m²");
        summary.AppendLine();
        
        // Crystal count
        summary.AppendLine($"<b>Crystals:</b> {userConfig.totalPoints:F0} points");
        summary.AppendLine($"Density: {userConfig.densityLevel}");
        summary.AppendLine();
        
        // Selected crystals
        var selections = userConfig.GetEnabledSelections();
        summary.AppendLine($"<b>Selected Types ({selections.Count}):</b>");
        
        foreach (var selection in selections)
        {
            var spawnData = userConfig.GetSpawnDataForSelection(selection);
            summary.AppendLine($"• {spawnData.variantName} ({selection.spawnRatio:F1}%)");
        }
        
        // Design details
        summary.AppendLine();
        summary.AppendLine($"<b>Design:</b>");
        summary.AppendLine($"Base: {userConfig.BaseType}");
        summary.AppendLine($"Style: {userConfig.colorStyle}");
        summary.AppendLine($"Surface: {userConfig.hangerSurface}");
        
        // Total price
        summary.AppendLine();
        summary.AppendLine($"<b>Total Price:</b> {userConfig.GetTotalPrice():F2} EGP");

        summaryText.text = summary.ToString();
    }

    // Individual loading methods
    public void LoadName()
    {
        if (userConfig != null && nameText != null)
            nameText.text = userConfig.configurationName;
    }

    public void LoadSalesmanID()
    {
        if (salesmanIDText != null)
            salesmanIDText.text = "N/A";
    }

    public void LoadNumberOfCrystals()
    {
        if (userConfig != null && numberOfCrystalsText != null)
            numberOfCrystalsText.text = userConfig.totalPoints.ToString("F0");
    }

    public void LoadSelectedCrystals()
    {
        if (userConfig == null || selectedCrystalsTexts == null) return;

        StringBuilder crystalsText = new StringBuilder();
        var selections = userConfig.GetEnabledSelections();

        foreach (var selection in selections)
        {
            var spawnData = userConfig.GetSpawnDataForSelection(selection);
            crystalsText.AppendLine($"• {spawnData.crystalType}");
        }

        selectedCrystalsTexts.text = crystalsText.ToString().TrimEnd();
    }

    public void LoadCrystalColors()
    {
        if (userConfig == null || crystalColorsTexts == null) return;

        var selections = userConfig.GetEnabledSelections();

        var spawnData = userConfig.GetSpawnDataForSelection(selections[0]);
        crystalColorsTexts.text += spawnData.variantName;
        crystalColorsTexts.color += spawnData.color;
        
    }

    public void LoadCompStyle()
    {
        if (userConfig != null && compStyleText != null)
            compStyleText.text = userConfig.colorStyle.ToString();
    }

    public void LoadNumberOfWires()
    {
        if (userConfig != null && numberOfWiresText != null)
            numberOfWiresText.text = userConfig.totalPoints.ToString("F0");
    }

    public void LoadBaseShape()
    {
        if (userConfig != null && baseShapeText != null)
            baseShapeText.text = userConfig.BaseType.ToString();
    }

    // Format data with labels
    public void LoadNameWithLabel()
    {
        if (userConfig != null && nameText != null)
            nameText.text = $"Name: {userConfig.configurationName}";
    }

    public void LoadSalesmanIDWithLabel()
    {
        if (salesmanIDText != null)
            salesmanIDText.text = "Salesman ID: N/A";
    }

    public void LoadCrystalCountWithLabel()
    {
        if (userConfig != null && numberOfCrystalsText != null)
            numberOfCrystalsText.text = $"Crystals: {userConfig.totalPoints:F0}";
    }

    public void LoadPriceWithLabel()
    {
        if (userConfig != null && summaryText != null)
        {
            float totalPrice = userConfig.GetTotalPrice();
            summaryText.text = $"Total Price: {totalPrice:F2} EGP";
        }
    }

    // Clear all text fields
    public void ClearAllTexts()
    {
        if (nameText != null) nameText.text = "";
        if (salesmanIDText != null) salesmanIDText.text = "";
        if (numberOfCrystalsText != null) numberOfCrystalsText.text = "";
        if (compStyleText != null) compStyleText.text = "";
        if (numberOfWiresText != null) numberOfWiresText.text = "";
        if (baseShapeText != null) baseShapeText.text = "";
        if (summaryText != null) summaryText.text = "";

        if (selectedCrystalsTexts != null)
        {
            selectedCrystalsTexts.text = "";
        }

        Debug.Log("[CardDataLoader] All text fields cleared!");
    }

    // Refresh all data (useful after configuration changes)
    public void RefreshAllData()
    {
        // Get latest config from ConfigurationManager
        if (ConfigurationManager.Instance != null)
        {
            userConfig = ConfigurationManager.Instance.GetCurrentConfig();
        }

        LoadAllData();
        Debug.Log("[CardDataLoader] Data refreshed from UserConfig");
    }
}