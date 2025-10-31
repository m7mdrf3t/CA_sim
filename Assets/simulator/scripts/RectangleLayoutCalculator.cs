using UnityEngine;

public class RectangleLayoutCalculator : ShapeLayoutCalculator
{
    [Header("Inputs")]
    [Min(0f)] public float length = 2.45f;
    [Min(0f)] public float width = 0.9f;

    [Min(0f)] public float height = 0f;

    protected override float CalculateArea()
    {
        return length * width;
    }
}