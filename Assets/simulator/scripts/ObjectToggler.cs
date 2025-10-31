using UnityEngine;
using UISwitcher; // Import your namespace

public class ObjectToggler : MonoBehaviour
{
  [Header("UI Switcher Reference")]
    [SerializeField] private UISwitcher.UISwitcher uiSwitcher;

    [Header("Target Object Names")]
    [Tooltip("List of object names to toggle (can include runtime-generated ones).")]
    [SerializeField] private string[] targetObjectNames;

    private GameObject[] targetObjects;

    private void Start()
    {
        if (uiSwitcher == null)
        {
            Debug.LogError(" UISwitcher reference missing!");
            return;
        }

        uiSwitcher.onValueChanged.AddListener(OnSwitchChanged);

        // Try to find all targets at start
        FindAllTargetObjects();
    }

    private void FindAllTargetObjects()
    {
        targetObjects = new GameObject[targetObjectNames.Length];

        for (int i = 0; i < targetObjectNames.Length; i++)
        {
            var obj = GameObject.Find(targetObjectNames[i]);
            targetObjects[i] = obj;

            if (obj != null)
                Debug.Log($"Found object: {targetObjectNames[i]}");
            else
                Debug.LogWarning($" Object '{targetObjectNames[i]}' not found (yet).");
        }
    }

    private void OnSwitchChanged(bool isOn)
    {
        // Re-find missing ones in case they're created later
        for (int i = 0; i < targetObjectNames.Length; i++)
        {
            if (targetObjects[i] == null)
                targetObjects[i] = GameObject.Find(targetObjectNames[i]);

            if (targetObjects[i] != null)
                targetObjects[i].SetActive(isOn);
        }

        Debug.Log($"🎮 Toggled {targetObjectNames.Length} objects → {(isOn ? "ON" : "OFF")}");
    }
}