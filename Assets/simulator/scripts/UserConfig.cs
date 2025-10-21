using UnityEngine;

[CreateAssetMenu(fileName = "NewUserConfig", menuName = "User Generator/Box Configuration", order = 1)]
public class UserConfig : ScriptableObject
{
    [Header("Box Dimensions")]
    [Tooltip("Box width in meters")]
    [Min(0.01f)]
    public float xSize = 1f;

    [Tooltip("Box depth in meters")]
    [Min(0.01f)]
    public float ySize = 1f;
    
    [Tooltip("Box depth in meters")]
    [Min(0.01f)]
    public float zSize = 1f;

    [Header("Crystal Properties")]
    [Tooltip("The crystal prefab to spawn as spots")]
    public GameObject crystalPrefab;
    
    [Tooltip("Color to apply to crystals")]
    public Color crystalColor = Color.white;
    
    [Tooltip("Type/category of crystal")]
    public CrystalType crystalType = CrystalType.Standard;

    [Header("Additional Settings (Optional)")]
    [Tooltip("Custom name for this configuration")]
    public string configurationName = "Default Box";
    
    [Tooltip("Density of crystals per square meter")]
    [Range(1, 100)]
    public int crystalsPerSquareMeter = 16;

    public enum CrystalType
    {
        Standard,
        Rare,
        Epic,
        Legendary,
        Custom
    }

    /// <summary>
    /// Validates the configuration data
    /// </summary>
    public bool IsValid()
    {
        if (xSize <= 0 || ySize <= 0)
        {
            Debug.LogError($"BoxConfiguration '{name}': Invalid dimensions. X and Y must be greater than 0.");
            return false;
        }

        if (crystalPrefab == null)
        {
            Debug.LogWarning($"BoxConfiguration '{name}': No crystal prefab assigned.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns a summary of this configuration
    /// </summary>
    public string GetSummary()
    {
        return $"{configurationName}: {xSize}m × {ySize}m, {crystalType} crystals, " +
               $"~{Mathf.RoundToInt(xSize * ySize * crystalsPerSquareMeter)} spots";
    }

    private void OnValidate()
    {
        xSize = Mathf.Max(0.01f, xSize);
        ySize = Mathf.Max(0.01f, ySize);
        crystalsPerSquareMeter = Mathf.Max(1, crystalsPerSquareMeter);
    }
}