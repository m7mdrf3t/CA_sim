using UnityEngine;

public abstract class ShapeGenerator
{
    protected float ceilingHeight;
    protected Vector2 surfaceCenter;
    protected float randomVariationRatio;

    public ShapeGenerator(float ceilingHeight, Vector2 surfaceCenter, float randomVariationRatio)
    {
        this.ceilingHeight = ceilingHeight;
        this.surfaceCenter = surfaceCenter;
        this.randomVariationRatio = randomVariationRatio;
    }

    public abstract float CalculateSurfaceLength(Vector3 anchorPos);

    public virtual void OnDrawGizmosSelected(Transform anchorRoot, Color gizmoColor, float wireRadius)
    {
        // Default implementation for gizmos (can be overridden)
        Gizmos.color = gizmoColor;
        foreach (Transform pt in anchorRoot)
        {
            float baseLength = CalculateSurfaceLength(pt.position);
            float randomOffset = UnityEngine.Random.Range(-randomVariationRatio, randomVariationRatio);
            float totalLength = baseLength + randomOffset;
            Vector3 surfacePos = new Vector3(pt.position.x, pt.position.y, ceilingHeight - totalLength);
            Gizmos.DrawWireSphere(surfacePos, 0.01f);
            Gizmos.DrawLine(pt.position, surfacePos);
        }
    }
}