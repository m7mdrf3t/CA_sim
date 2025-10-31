using UnityEngine;

public class CircleLayoutCalculator : ShapeLayoutCalculator
{
    [Header("Inputs")]
    [Min(0f)] public float diameter = 1f;
    public float radius;

    protected override float CalculateArea()
    {
        radius = diameter / 2f;
        return Mathf.PI * radius * radius;
    }
}