using UnityEngine;
using UnityEngine.UI;

public class LightColorController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Light targetLight;    // Light to change color
    [SerializeField] private string objectName = "Generated_Cylinder"; // Name of the object to find

    private Renderer targetRenderer; // Renderer of the found object

    [Header("Color Buttons")]
    [SerializeField] private Button yellowButton;
    [SerializeField] private Button grayButton;
    [SerializeField] private Button whiteButton;
    [SerializeField] private Button blackButton;

    private void Start()
    {
        // Try to find the object in the scene by name
        FindTargetObject();

        // Assign button listeners
        if (yellowButton != null) yellowButton.onClick.AddListener(() => SetColor(Color.yellow));
        if (grayButton != null)   grayButton.onClick.AddListener(() => SetColor(Color.gray));
        if (whiteButton != null)  whiteButton.onClick.AddListener(() => SetColor(Color.white));
        if (blackButton != null)  blackButton.onClick.AddListener(() => SetColor(Color.black));
    }

    private void FindTargetObject()
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            targetRenderer = obj.GetComponent<Renderer>();
            if (targetRenderer == null)
                Debug.LogWarning($" Object '{objectName}' found, but it has no Renderer component.");
            else
                Debug.Log($" Found object '{objectName}' to color.");
        }
        else
        {
            Debug.LogWarning($" Could not find object named '{objectName}' in the scene!");
        }
    }

    private void SetColor(Color newColor)
    {
        // Change light color
        if (targetLight != null)
            targetLight.color = newColor;

        // Apply color to found 3D object
        if (targetRenderer == null)
            FindTargetObject(); // Try finding again in case it was generated later

        if (targetRenderer != null)
            targetRenderer.material.color = newColor;

        Debug.Log($"🎨 Applied color {newColor} to light and object '{objectName}'.");
    }
}