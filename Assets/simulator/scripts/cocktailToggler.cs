using UnityEngine;
using UISwitcher; // Import your namespace

public class cocktailToggler : MonoBehaviour
{
  [Header("UI Switcher Reference")]
    [SerializeField] private UISwitcher.UISwitcher uiSwitcher;

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
        if(isOn)
        {
            userConfig.colorStyle = colorStyle.Coctail;
        }
        else
        {
            userConfig.colorStyle = colorStyle.fade;
        }
    

        beads.SetActive(isOn);
    }
}