using UnityEngine;
using UnityEngine.SceneManagement;

public class sceneloader_i : MonoBehaviour
{
    // Make sure method is public and returns void
    public void ReloadScene()
    {
        Debug.Log("Reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    // Alternative method
    public void ReloadSceneByName()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}