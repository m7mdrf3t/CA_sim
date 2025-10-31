using UnityEngine;

public class HelixLayoutCalculator : ShapeLayoutCalculator
{
    [Header("Inputs")]
    [Min(0f)] public float diameter = 1f;
    [Min(0f)] public float height = 1f;
    [Min(0)] public int helixCount = 3;

    [Header("Reference Constants")]
    [Min(0f)] public float refDiameter = 1f;
    [Min(0f)] public float refHeight = 1f;
    [Min(0)] public int refHelixCount = 3;
    [Min(0f)] public float refHelixLength = 5.4f;
    [Min(0f)] public float refPointsPerHelix = 50f;

    public float helixLength;
    public float pointsPerHelix;
    public float totalHelixPoints;

    

    protected override float CalculateArea()
    {
        return CalculateHelixLength();
    }

    private float CalculateHelixLength()
    {
        helixLength = refHelixLength * (height / refHeight) * (diameter / refDiameter);
        return helixLength;
    }

    public override void CalculateLayout()
    {
        helixLength = CalculateHelixLength();

        pointsPerHelix = refPointsPerHelix * (diameter / refDiameter) * (height / refHeight);
        totalHelixPoints = pointsPerHelix * helixCount;

        area = helixLength;
        totalSpots = helixCount;
        totalPoints = totalHelixPoints;
        density = 100f;

        highDensity = totalHelixPoints;
        mediumDensity = totalHelixPoints * 0.75f;
        lowDensity = totalHelixPoints * 0.375f;
    }
}