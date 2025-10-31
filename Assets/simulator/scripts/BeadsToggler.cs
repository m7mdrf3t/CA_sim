using UnityEngine;
using UISwitcher; // Import your namespace

public class BeadsToggler : MonoBehaviour
{
  [Header("UI Switcher Reference")]
    [SerializeField] private UISwitcher.UISwitcher uiSwitcher;

    [SerializeField] private bool showBeads;
    [SerializeField] private bool ShowBase;

    [SerializeField] GameObject beads;

    

    [Header("Target Object Names")]
    [Tooltip("List of object names to toggle (can include runtime-generated ones).")]
    [SerializeField] private UserConfig userConfig;

    private void Start()
    {
        if (uiSwitcher == null)
        {
            Debug.LogError(" UISwitcher reference missing!");
            return;
        }

        uiSwitcher.onValueChanged.AddListener(OnSwitchChanged);
    }

    private void OnSwitchChanged(bool isOn)
    {
        if(showBeads)
            userConfig.showBeads = isOn;
        if(ShowBase)
            userConfig.WithBase = isOn;

        beads.SetActive(isOn);
    }
}