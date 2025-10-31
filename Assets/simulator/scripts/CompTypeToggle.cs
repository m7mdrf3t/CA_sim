using UnityEngine;
using UnityEngine.UI;

public class CompTypeToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle; // Drag your Toggle here
    [SerializeField] private string typeOn  = "TypeA";
    [SerializeField] private string typeOff = "TypeB";

    private void Start()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {

        if(isOn)
        {
            Debug.Log($"🔄 Toggle switched → Component Type = 3333 ");
        }
        else
        {
           Debug.Log($"🔄 Toggle switched → Component Type = 6666 ");
        }
        
    }
}