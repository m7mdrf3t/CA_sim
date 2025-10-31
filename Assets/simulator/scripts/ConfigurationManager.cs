// ConfigurationManager.cs
using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.IntegerTime;

// ==================== SAVE DATA STRUCTURE ====================
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
        public float spawnRatio;
        public float price;
    }
}

// ==================== CONFIGURATION MANAGER ====================
[DefaultExecutionOrder(-1000)]
public class ConfigurationManager : MonoBehaviour
{
    [Header("References (assign in Inspector if possible)")]
    [SerializeField] private CrystalDatabase crystalDatabase;
    [SerializeField] private UserConfig currentConfig;

    [Header("Debug Display (REQUIRED for build debugging)")]
    [Tooltip("Assign a TMP_Text UI element to see debug output")]
    [SerializeField] private TMP_Text debugger;

    [Header("Debug Settings")]
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private int maxDebugLines = 50;

    private static ConfigurationManager instance;
    public static ConfigurationManager Instance => instance;

    private List<string> debugLog = new List<string>();

    // -------------------- LIFECYCLE --------------------
    private void Awake()
    {
        
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }


        // Try Resources fallback with detailed debugging
        if (crystalDatabase == null)
        {
            
            // Try to load all resources to see what's available
            var allDatabases = Resources.LoadAll<CrystalDatabase>("Databases");
            
            crystalDatabase = Resources.Load<CrystalDatabase>("Databases/CrystalDatabase");
            
            if (crystalDatabase != null)
            {
                DebugLog($"SUCCESS: Loaded {crystalDatabase.name}");
            }
            else
            {
                
                // Try alternate paths
                var altLoad = Resources.Load<CrystalDatabase>("CrystalDatabase");
                if (altLoad != null)
                {
                    DebugLog("Found at root: Resources/CrystalDatabase");
                    crystalDatabase = altLoad;
                }
            }
        }

        if (currentConfig == null)
        {
            DebugLog("Attempting to load UserConfig from Resources...");
            DebugLog("Looking for: Resources/Databases/UserConfig");
            
            var allConfigs = Resources.LoadAll<UserConfig>("Databases");
            DebugLog($"Found {allConfigs.Length} UserConfig(s) in Resources/Databases/");
            
            currentConfig = Resources.Load<UserConfig>("Databases/UserConfig");
            
            if (currentConfig != null)
            {
                DebugLog($"SUCCESS: Loaded {currentConfig.name}");
            }
            else
            {
                DebugLog("FAILED: UserConfig not found!");
                
                var altLoad = Resources.Load<UserConfig>("UserConfig");
                if (altLoad != null)
                {
                    DebugLog("Found at root: Resources/UserConfig");
                    currentConfig = altLoad;
                }
            }
        }

        currentConfig.crystalSelections.Clear();


        // Create runtime copy
        // if (currentConfig != null)
        // {
        //     DebugLog("Creating runtime copy of UserConfig");
        //     currentConfig = Instantiate(currentConfig);
        //     DebugLog("Runtime copy created successfully");
        // }
        // else
        // {
        //     DebugLog("ERROR: No UserConfig available!");
        // }

        // Ensure DB reference
        if (currentConfig != null)
        {

            if (currentConfig.crystalDatabase == null)
            {
                DebugLog("Assigning crystalDatabase to config");
                currentConfig.crystalDatabase = crystalDatabase;
            }
            else
            {
                DebugLog("Config already has database reference");
            }
        }

        
        UpdateDebugDisplay();
    }

    private void Start()
    {
        
        try
        {
            if (crystalDatabase == null)
            {
                DebugLog("CRITICAL: CrystalDatabase is NULL in Start!");
            }
            else
            {
                DebugLog($"Database OK: {crystalDatabase.name}");
            }

            if (currentConfig == null)
            {
                DebugLog("CRITICAL: UserConfig is NULL in Start!");
            }
            else
            {
                DebugLog($"Config OK: {currentConfig.name}");
                DebugLog($"Config Valid: {currentConfig.IsValid()}");
            }

            // Try to load saved config
            DebugLog("Attempting to load LastConfig...");
            bool loaded = LoadConfigFromFile("LastConfig");
            DebugLog($"Load result: {loaded}");

            if (currentConfig != null && currentConfig.crystalSelections != null)
            {
                DebugLog($"Current selections count: {currentConfig.crystalSelections.Count}");
            }

            DebugLog("=== START COMPLETE ===");
            UpdateDebugDisplay();
        }
        catch (Exception ex)
        {
            DebugLog($"EXCEPTION in Start: {ex.GetType().Name}");
            DebugLog($"Message: {ex.Message}");
            DebugLog($"Stack: {ex.StackTrace}");
        }
    }

    // -------------------- DEBUG LOGGING --------------------
    private void DebugLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] {message}";
        
        // Always log to Unity console
        Debug.Log(logEntry);

        // Store in memory for display
        debugLog.Add(logEntry);
        if (debugLog.Count > maxDebugLines)
        {
            debugLog.RemoveAt(0);
        }

        // Update display immediately if available
        if (debugger != null && verboseLogging)
        {
            debugger.text = string.Join("\n", debugLog);
        }
    }

    private void UpdateDebugDisplay()
    {
        if (debugger == null)
        {
            Debug.LogWarning("Debugger TMP_Text is not assigned!");
            return;
        }

        string summary = "=== CONFIG MANAGER STATUS ===\n";
        summary += $"Database: {(crystalDatabase ? crystalDatabase.name : "NULL")}\n";
        summary += $"Config: {(currentConfig ? currentConfig.name : "NULL")}\n";
        
        if (currentConfig != null)
        {
            summary += $"Valid: {currentConfig.IsValid()}\n";
            summary += $"Selections: {currentConfig.crystalSelections?.Count ?? 0}\n";
            summary += $"Size: {currentConfig.xSize}x{currentConfig.ySize}x{currentConfig.zSize}\n";
        }
        
        summary += "\n=== RECENT LOG ===\n";
        
        // Show last 15 lines
        int startIdx = Mathf.Max(0, debugLog.Count - 15);
        for (int i = startIdx; i < debugLog.Count; i++)
        {
            summary += debugLog[i] + "\n";
        }

        debugger.text = summary;
    }

    // ==================== ADDING SELECTIONS ====================
    public void AddCrystalSelection(CrystalType crystalType, int variantIndex, float weight = 1f , float price = 0f)
    {
        DebugLog($"AddCrystalSelection called: {crystalType}, variant {variantIndex}");
        
        if (currentConfig == null)
        {
            DebugLog("ERROR: No UserConfig assigned!");
            UpdateDebugDisplay();
            return;
        }

        if (currentConfig.crystalSelections.Count >= 4)
        {
            DebugLog("ERROR: Maximum 4 crystal selections allowed!");
            UpdateDebugDisplay();
            return;
        }

        var newSelection = new UserConfig.CrystalSelection
        {
            enabled = true,
            crystalType = crystalType,
            variantIndex = variantIndex,
            spawnWeight = weight,
            mainTypes = CrystalMainTypes.Refraction,
            price = price
        };

        currentConfig.crystalSelections.Add(newSelection);
        DebugLog($"Selection added. Total count: {currentConfig.crystalSelections.Count}");
        
        SaveCurrentConfig("LastConfig");
        UpdateDebugDisplay();
    }

    public void RemoveCrystalSelection(int index)
    {
        DebugLog($"RemoveCrystalSelection called: index {index}");
        
        if (currentConfig == null)
        {
            DebugLog("ERROR: No config to remove from");
            return;
        }

        if (index >= 0 && index < currentConfig.crystalSelections.Count)
        {
            currentConfig.crystalSelections.RemoveAt(index);
            SaveCurrentConfig("LastConfig");
            UpdateDebugDisplay();
        }
        else
        {
            DebugLog($"ERROR: Invalid index {index}");
        }
    }

    public void ClearAllSelections()
    {
        DebugLog("ClearAllSelections called");
        
        if (currentConfig == null)
        {
            DebugLog("ERROR: No config to clear");
            return;
        }

        currentConfig.crystalSelections.Clear();
        DebugLog("All selections cleared");
        SaveCurrentConfig("LastConfig");
        UpdateDebugDisplay();
    }

    // ==================== SAVING & LOADING ====================
    public void SaveCurrentConfig(string fileName)
    {
        
        if (currentConfig == null)
        {
            UpdateDebugDisplay();
            return;
        }

        try
        {
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
                    spawnWeight = selection.spawnWeight,
                    price = selection.price,
                    spawnRatio = selection.spawnRatio

                });
            }

            string json = JsonUtility.ToJson(saveData, true);
            DebugLog($"JSON length: {json.Length} chars");
            
            string path = GetSavePath(fileName);
            File.WriteAllText(path, json);
            
            UpdateDebugDisplay();
        }
        catch (Exception e)
        {
            DebugLog($"Stack: {e.StackTrace}");
            UpdateDebugDisplay();
        }
    }

    public bool LoadConfigFromFile(string fileName)
    {
        string path = GetSavePath(fileName);

        if (!File.Exists(path))
        {
            DebugLog($"File does not exist: {path}");
            UpdateDebugDisplay();
            return false;
        }

        try
        {
            string json = File.ReadAllText(path);
            
            var saveData = JsonUtility.FromJson<UserConfigSaveData>(json);
            
            ApplySaveDataToConfig(saveData);
            
            UpdateDebugDisplay();
            return true;
        }
        catch (Exception e)
        {
            DebugLog($"LOAD ERROR: {e.GetType().Name}");
            DebugLog($"Message: {e.Message}");
            DebugLog($"Stack: {e.StackTrace}");
            UpdateDebugDisplay();
            return false;
        }
    }

    private void ApplySaveDataToConfig(UserConfigSaveData saveData)
    {
        if (currentConfig == null)
        {
            return;
        }

        currentConfig.configurationName = saveData.configurationName;
        currentConfig.xSize = saveData.xSize;
        currentConfig.ySize = saveData.ySize;
        currentConfig.zSize = saveData.zSize;
        currentConfig.crystalsPerSquareMeter = saveData.crystalsPerSquareMeter;

        currentConfig.crystalSelections.Clear();

        if (Enum.TryParse<SurfaceType>(saveData.crystalSurfaceName, out SurfaceType surface))
        {
            currentConfig.hangerSurface = surface;
            DebugLog($"Surface set to: {surface}");
        }
        else
        {
            DebugLog($"WARNING: Could not parse surface: {saveData.crystalSurfaceName}");
        }

        
        foreach (var selData in saveData.selections)
        {
            if (Enum.TryParse<CrystalType>(selData.crystalTypeName, out CrystalType type))
            {
                currentConfig.crystalSelections.Add(new UserConfig.CrystalSelection
                {
                    enabled = selData.enabled,
                    crystalType = type,
                    variantIndex = selData.variantIndex,
                    spawnWeight = selData.spawnWeight
                });
            }
            else
            {
                DebugLog($"  WARNING: Could not parse type: {selData.crystalTypeName}");
            }
        }

        // Ensure DB reference
        if (currentConfig.crystalDatabase == null)
        {
            currentConfig.crystalDatabase = crystalDatabase;
        }

    }

    private string GetSavePath(string fileName)
    {
        string directory = Application.persistentDataPath + "/Configs/";
        
        if (verboseLogging)
        {
            DebugLog($"Save directory: {directory}");
        }

        try
        {
            if (!Directory.Exists(directory))
            {
                DebugLog("Creating directory...");
                Directory.CreateDirectory(directory);
                DebugLog("Directory created");
            }
        }
        catch (Exception e)
        {
            DebugLog($"Directory creation error: {e.Message}");
        }

        string fullPath = Path.Combine(directory, fileName + ".json");
        
        if (verboseLogging)
        {
            DebugLog($"Full path: {fullPath}");
        }
        
        return fullPath;
    }

    public List<string> GetAllSavedConfigs()
    {
        string directory = Application.persistentDataPath + "/Configs/";

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        try
        {
            var files = Directory.GetFiles(directory, "*.json");
            return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
        }
        catch (Exception e)
        {
            DebugLog($"Error listing configs: {e.Message}");
            return new List<string>();
        }
    }

    // ==================== PUBLIC GETTERS ====================
    public UserConfig GetCurrentConfig() => currentConfig;
    public CrystalDatabase GetDatabase() => crystalDatabase;
    
    public List<UserConfig.CrystalSelection> GetCurrentSelections()
    {
        if (currentConfig == null)
        {
            DebugLog("GetCurrentSelections: config is null");
            return new List<UserConfig.CrystalSelection>();
        }
        return currentConfig.crystalSelections ?? new List<UserConfig.CrystalSelection>();
    }

    public string GetSelectionInfo(int index)
    {
        if (currentConfig == null || index < 0 || index >= currentConfig.crystalSelections.Count)
        {
            return "";
        }

        var selection = currentConfig.crystalSelections[index];
        var variant = currentConfig.GetVariantForSelection(selection);
        return $"{selection.crystalType} - {variant?.variantName ?? "Unknown"} (Weight: {selection.spawnWeight:F2})";
    }

    // ==================== DEBUG HELPERS ====================
    [ContextMenu("Force Update Debug Display")]
    public void ForceUpdateDebugDisplay()
    {
        UpdateDebugDisplay();
    }

    [ContextMenu("Test Save")]
    public void TestSave()
    {
        SaveCurrentConfig("TestConfig");
    }

    [ContextMenu("Test Load")]
    public void TestLoad()
    {
        LoadConfigFromFile("TestConfig");
    }

    [ContextMenu("Clear Debug Log")]
    public void ClearDebugLog()
    {
        debugLog.Clear();
        UpdateDebugDisplay();
    }
}