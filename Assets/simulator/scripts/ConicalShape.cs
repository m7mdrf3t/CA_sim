using UnityEngine;

public class ConicalShape : ShapeGenerator
{
    private float baseRadius; // Radius at the base of the cone
    private float height;    // Total height of the cone from apex to base
    private Vector3 apex;    // Apex position of the cone

    public ConicalShape(float ceilingHeight, Vector2 surfaceCenter, float randomVariationRatio,
        float baseRadius, float height, Vector3 apex) : base(ceilingHeight, surfaceCenter, randomVariationRatio)
    {
        this.baseRadius = baseRadius;
        this.height = height;
        this.apex = apex;
    }

    public override float CalculateSurfaceLength(Vector3 anchorPos)
    {
        // Calculate distance from anchor to apex in XY plane
        float dx = anchorPos.x - apex.x;
        float dy = anchorPos.y - apex.y;
        float radialDistance = Mathf.Sqrt(dx * dx + dy * dy);

        // Simple conical slope: length = height * (1 - radialDistance / baseRadius) + random offset
        float baseLength = height * (1 - Mathf.Clamp01(radialDistance / baseRadius));
        float randomOffset = UnityEngine.Random.Range(-randomVariationRatio, randomVariationRatio);

        return Mathf.Max(0.1f, Mathf.Abs(ceilingHeight - (baseLength + randomOffset)));
    }

    public override void OnDrawGizmosSelected(Transform anchorRoot, Color gizmoColor, float wireRadius)
    {
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
        // Draw apex point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(apex, 0.02f);
    }
}