using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// ==================== SAVE DATA STRUCTURE ====================
/// <summary>
/// Serializable data structure for saving/loading configs
/// </summary>
[System.Serializable]
public class UserConfigSaveData
{
    public string configurationName;
    public float xSize;
    public float ySize;
    public float zSize;
    public int crystalsPerSquareMeter;
    public string crystalSurfaceName;
    public List<CrystalSelectionData> selections = new List<CrystalSelectionData>();
    
    [System.Serializable]
    public class CrystalSelectionData
    {
        public bool enabled;
        public string crystalTypeName;
        public int variantIndex;
        public float spawnWeight;
    }
}

// ==================== CONFIGURATION MANAGER ====================
/// <summary>
/// Manages crystal selection and saving/loading configurations
/// </summary>
public class ConfigurationManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CrystalDatabase crystalDatabase;
    [SerializeField] private UserConfig currentConfig;
    
    private static ConfigurationManager instance;
    public static ConfigurationManager Instance => instance;

    public 
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        ClearAllSelections();
    }
    
    void Start()
    {
        LoadConfigFromFile("LastConfig");
    }

    
    // ==================== ADDING SELECTIONS ====================
    
    /// <summary>
    /// Add a crystal selection to the current config
    /// </summary>
    public void AddCrystalSelection(CrystalType crystalType, int variantIndex, float weight = 1f)
    {
        if (currentConfig == null)
        {
            Debug.LogError("No UserConfig assigned!");
            return;
        }
        
        if (currentConfig.crystalSelections.Count >= 4)
        {
            Debug.LogWarning("Maximum 4 crystal selections allowed!");
            return;
        }
        
        var newSelection = new UserConfig.CrystalSelection
        {
            enabled = true,
            crystalType = crystalType,
            variantIndex = variantIndex,
            spawnWeight = weight
        };
        
        currentConfig.crystalSelections.Add(newSelection);
        
        Debug.Log($"Added crystal selection: {crystalType}, Variant: {variantIndex}");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(currentConfig);
        #endif
        
        SaveCurrentConfig("LastConfig");
    }
    
    public void RemoveCrystalSelection(int index)
    {
        if (currentConfig == null) return;
        
        if (index >= 0 && index < currentConfig.crystalSelections.Count)
        {
            currentConfig.crystalSelections.RemoveAt(index);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentConfig);
            #endif
            
            SaveCurrentConfig("LastConfig");
        }
    }
    
    public void ClearAllSelections()
    {
        if (currentConfig == null) return;
        
        currentConfig.crystalSelections.Clear();
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(currentConfig);
        #endif
        
        SaveCurrentConfig("LastConfig");
    }
    
    // ==================== SAVING & LOADING ====================
    
    public void SaveCurrentConfig(string fileName)
    {
        if (currentConfig == null)
        {
            Debug.LogError("No config to save!");
            return;
        }
        
        var saveData = new UserConfigSaveData
        {
            configurationName = currentConfig.configurationName,
            xSize = currentConfig.xSize,
            ySize = currentConfig.ySize,
            zSize = currentConfig.zSize,
            crystalSurfaceName = currentConfig.hangerSurface.ToString(),
            crystalsPerSquareMeter = currentConfig.crystalsPerSquareMeter
        };
        
        foreach (var selection in currentConfig.crystalSelections)
        {
            saveData.selections.Add(new UserConfigSaveData.CrystalSelectionData
            {
                enabled = selection.enabled,
                crystalTypeName = selection.crystalType.ToString(),
                variantIndex = selection.variantIndex,
                spawnWeight = selection.spawnWeight
            });
        }
        
        string json = JsonUtility.ToJson(saveData, true);
        string path = GetSavePath(fileName);
        File.WriteAllText(path, json);
        
        Debug.Log($"Config saved to: {path}");
    }
    
    public bool LoadConfigFromFile(string fileName)
    {
        string path = GetSavePath(fileName);
        
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Config file not found: {path}");
            return false;
        }
        
        try
        {
            string json = File.ReadAllText(path);
            var saveData = JsonUtility.FromJson<UserConfigSaveData>(json);
            ApplySaveDataToConfig(saveData);
            
            Debug.Log($"Config loaded from: {path}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load config: {e.Message}");
            return false;
        }
    }
    
    private void ApplySaveDataToConfig(UserConfigSaveData saveData)
    {
        if (currentConfig == null) return;
        
        currentConfig.configurationName = saveData.configurationName;
        currentConfig.xSize = saveData.xSize;
        currentConfig.ySize = saveData.ySize;
        currentConfig.zSize = saveData.zSize;
        currentConfig.crystalsPerSquareMeter = saveData.crystalsPerSquareMeter;
        
        currentConfig.crystalSelections.Clear();
        
        if (System.Enum.TryParse<HangerBuilder.SurfaceType>(saveData.crystalSurfaceName, out HangerBuilder.SurfaceType surface))
        {
            currentConfig.hangerSurface = surface;
        }

        foreach (var selData in saveData.selections)
        {
            if (System.Enum.TryParse<CrystalType>(selData.crystalTypeName, out CrystalType type))
            {
                currentConfig.crystalSelections.Add(new UserConfig.CrystalSelection
                {
                    enabled = selData.enabled,
                    crystalType = type,
                    variantIndex = selData.variantIndex,
                    spawnWeight = selData.spawnWeight
                });
            }
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(currentConfig);
        #endif
    }
    
    private string GetSavePath(string fileName)
    {
        string directory = Application.persistentDataPath + "/Configs/";
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        return directory + fileName + ".json";
    }
    
    public List<string> GetAllSavedConfigs()
    {
        string directory = Application.persistentDataPath + "/Configs/";
        
        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }
        
        var files = Directory.GetFiles(directory, "*.json");
        return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
    }
    
    // ==================== PUBLIC GETTERS ====================
    
    public UserConfig GetCurrentConfig() => currentConfig;
    public CrystalDatabase GetDatabase() => crystalDatabase;
    public List<UserConfig.CrystalSelection> GetCurrentSelections() => currentConfig?.crystalSelections ?? new List<UserConfig.CrystalSelection>();
    
    public string GetSelectionInfo(int index)
    {
        if (currentConfig == null || index < 0 || index >= currentConfig.crystalSelections.Count)
            return "";
        
        var selection = currentConfig.crystalSelections[index];
        var variant = currentConfig.GetVariantForSelection(selection);
        
        return $"{selection.crystalType} - {variant?.variantName ?? "Unknown"} (Weight: {selection.spawnWeight:F2})";
    }
}
