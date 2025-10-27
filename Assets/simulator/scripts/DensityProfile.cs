using UnityEngine;

/// <summary>
/// A ScriptableObject to define density presets (e.g., "High", "Medium", "Low").
/// Create assets from this via the 'Create > Distribution > Density Profile' menu.
/// </summary>
[CreateAssetMenu(fileName = "Density_Medium", menuName = "Distribution/Density Profile")]
public class DensityProfile : ScriptableObject
{
    [Tooltip("Points per base area unit (e.g., 120)")]
    public float constantPoints = 120f;

    [Tooltip("Base units divisor (e.g., 1)")]
    [Min(0.001f)]
    public float baseUnits = 1f;
}