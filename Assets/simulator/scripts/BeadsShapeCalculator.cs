using UnityEngine;

public class BeadsShapeCalculator : ShapeLayoutCalculator
{

    public float length;

    protected override float CalculateArea()
    {
        return length * 7f;
    }
}
