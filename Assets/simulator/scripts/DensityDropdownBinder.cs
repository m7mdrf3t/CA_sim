using TMPro;
using UnityEngine;

public class DensityDropdownBinder : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private PointGenerator generator;

    private void Awake()
    {
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void OnChanged(int index)
    {
        // Ensure enum order matches dropdown order: High=0, Medium=1, Low=2 (or map explicitly)
        DensityLevel level = DensityLevel.High;
        switch (index)
        {
            case 0: level = DensityLevel.High;   break;
            case 1: level = DensityLevel.Medium; break;
            case 2: level = DensityLevel.Low;    break;
        }
        generator.SetDensityLevel(level);
    }
}