using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReloaderManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool clearSelectionsBeforeReload = true;
    [SerializeField] private float delayBeforeReload = 0f;

    /// <summary>
    /// Reload the current active scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        if (clearSelectionsBeforeReload && ConfigurationManager.Instance != null)
        {
            ConfigurationManager.Instance.ClearAllSelections();
            Debug.Log("[SceneReloader] Cleared selections before reload");
        }

        if (delayBeforeReload > 0)
        {
            Invoke(nameof(ReloadSceneNow), delayBeforeReload);
        }
        else
        {
            ReloadSceneNow();
        }
    }

    private void ReloadSceneNow()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneReloader] Reloading scene: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Reload scene by name
    /// </summary>
    public void ReloadScene(string sceneName)
    {
        if (clearSelectionsBeforeReload && ConfigurationManager.Instance != null)
        {
            ConfigurationManager.Instance.ClearAllSelections();
        }

        Debug.Log($"[SceneReloader] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reload scene by build index
    /// </summary>
    public void ReloadScene(int buildIndex)
    {
        if (clearSelectionsBeforeReload && ConfigurationManager.Instance != null)
        {
            ConfigurationManager.Instance.ClearAllSelections();
        }

        Debug.Log($"[SceneReloader] Loading scene index: {buildIndex}");
        SceneManager.LoadScene(buildIndex);
    }

    /// <summary>
    /// Restart application (more complete reset)
    /// </summary>
    public void RestartApplication()
    {
        Debug.Log("[SceneReloader] Restarting application...");
        
        // Clear PlayerPrefs if needed
        // PlayerPrefs.DeleteAll();
        
        // Reload first scene
        SceneManager.LoadScene(0);
    }
}