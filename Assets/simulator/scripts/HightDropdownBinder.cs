

using TMPro;
using UnityEngine;

public class HightDropdownBinder : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private HangerBuilder hanger;

    private void Awake()
    {
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void OnChanged(int index)
    {
        // Ensure enum order matches dropdown order: High=0, Medium=1, Low=2 (or map explicitly)
        HightLevel level = HightLevel.High;
        switch (index)
        {
            case 0: level = HightLevel.High;   break;
            case 1: level = HightLevel.Medium; break;
            case 2: level = HightLevel.Low;    break;
        }
        hanger.SetHightLevel(level);
    }
}