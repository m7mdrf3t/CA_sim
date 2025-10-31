using UnityEngine;
using UnityEngine.UI;
using UISwitcher;


public class UIToggleSwitcher : MonoBehaviour
{
    [Header("UI Switcher Reference")]
    [SerializeField] private UISwitcher.UISwitcher switcher;
    
    [SerializeField] private HangerBuilder hanger;

    [Header("Choices")]
    [SerializeField] private GameObject choice1Object;
    [SerializeField] private GameObject choice2Object;
    
    [Header("Choice Labels (Optional)")]
    [SerializeField] private string choice1Name = "Choice 1";
    [SerializeField] private string choice2Name = "Choice 2";

    private void Start()
    {
        if (switcher != null)
        {
            // Subscribe to the switcher's value change event
            switcher.onValueChanged.AddListener(OnSwitcherChanged);
            
            // Initialize with current value
            OnSwitcherChanged(switcher.isOn);
        }
        else
        {
            Debug.LogError("UISwitcher reference is missing!");
        }
    }

    private void OnSwitcherChanged(bool isOn)
    {
        // Toggle between the two choices
        if (isOn)
        {
          hanger.SetCompsType(CompsType.cocktail);
            
            OnChoice2Selected();
        }
        else
        {
            hanger.SetCompsType(CompsType.fade);
            
            OnChoice1Selected();
        }
    }

    // Override these methods for custom behavior
    protected virtual void OnChoice1Selected()
    {
        Debug.Log($"Selected: {choice1Name}");
    }

    protected virtual void OnChoice2Selected()
    {
        Debug.Log($"Selected: {choice2Name}");
    }

    // Public method to programmatically switch
    public void SetChoice(bool useChoice2)
    {
        if (switcher != null)
        {
            switcher.isOn = useChoice2;
        }
    }

    // Public method to get current choice
    public bool IsChoice2Active()
    {
        return switcher != null && switcher.isOn;
    }

    private void OnDestroy()
    {
        // Clean up listener
        if (switcher != null)
        {
            switcher.onValueChanged.RemoveListener(OnSwitcherChanged);
        }
    }
}