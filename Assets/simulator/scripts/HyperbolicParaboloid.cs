using UnityEngine;

public class HyperbolicParaboloid : ShapeGenerator
{
    private float surfaceA;
    private float surfaceB;
    private float heightAmplitude;

    public HyperbolicParaboloid(float ceilingHeight, Vector2 surfaceCenter, float randomVariationRatio,
        float surfaceA, float surfaceB, float heightAmplitude) : base(ceilingHeight, surfaceCenter, randomVariationRatio)
    {
        this.surfaceA = surfaceA;
        this.surfaceB = surfaceB;
        this.heightAmplitude = heightAmplitude;
    }

    public override float CalculateSurfaceLength(Vector3 anchorPos)
    {
        float x = anchorPos.x - surfaceCenter.x;
        float y = anchorPos.y - surfaceCenter.y;

        float baseSurfaceZ = heightAmplitude * ((x * x) / (surfaceA * surfaceA) - (y * y) / (surfaceB * surfaceB));

        float randomOffset = UnityEngine.Random.Range(-randomVariationRatio, randomVariationRatio);

        float zSurface = baseSurfaceZ + randomOffset;

        return Mathf.Max(0.1f, Mathf.Abs(ceilingHeight - zSurface));
    }

    public override void OnDrawGizmosSelected(Transform anchorRoot, Color gizmoColor, float wireRadius)
    {
        base.OnDrawGizmosSelected(anchorRoot, gizmoColor, wireRadius);
    }
}