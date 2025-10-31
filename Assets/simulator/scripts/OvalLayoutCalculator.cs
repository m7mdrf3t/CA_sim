using UnityEngine;

public class OvalLayoutCalculator : ShapeLayoutCalculator
{
    [Header("Inputs")]
    [Min(0f)] public float majorAxis = 1f;
    [Min(0f)] public float minorAxis = 0.5f;

    public float radiusA;
    public float radiusB;

    protected override float CalculateArea()
    {
        radiusA = majorAxis / 2f;
        radiusB = minorAxis / 2f;
        return Mathf.PI * radiusA * radiusB;
    }
}