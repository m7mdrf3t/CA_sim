using System;
using UnityEngine;

/// <summary>
/// The default shape, containing all the logic from the original 'distributer'
/// for a rectangle with top and bottom curves.
/// </summary>
[System.Serializable]
public class CurvedRectangleShape : DistributionShape
{
    [Header("Rectangle Dimensions (meters)")]
    public float plateWidthX = 1f;
    public float plateHeightZ = 1f;

    [Header("Margins (meters) — Layout Bounds Only")]
    [Min(0f)] public float marginX = 0.0f;
    [Min(0f)] public float marginZ = 0.0f;
    public bool centerOnOrigin = true;

    [Header("Top Curve Profile")]
    public CurveMode topCurveMode = CurveMode.Parabola;
    [Range(0f, 0.5f)] public float topSag = 0.08f;
    public AnimationCurve topCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Bottom Curve (3 Linked Segments)")]
    public bool useTripleBottom = true;
    [Range(0f, 0.5f)] public float bottomMaxRise = 0.10f;
    [Range(0.05f, 0.9f)] public float splitLeft = 0.33f;
    [Range(0.10f, 0.95f)] public float splitRight = 0.66f;
    public AnimationCurve bottomLeft = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0.3f));
    public AnimationCurve bottomCenter = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0.6f));
    public AnimationCurve bottomRight = new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 0));

    
    // public override Bounds GetBounds(Transform relativeTo)
    // {
    //     float usableWidth = Mathf.Max(0f, plateWidthX - 2f * marginX);
    //     float usableHeight = Mathf.Max(0f, plateHeightZ - 2f * marginZ);
        
    //     Vector3 center = new Vector3(
    //         centerOnOrigin ? relativeTo.position.x : relativeTo.position.x + plateWidthX * 0.5f,
    //         relativeTo.position.y,
    //         centerOnOrigin ? relativeTo.position.z : relativeTo.position.z + plateHeightZ * 0.5f
    //     );

    //     Vector3 size = new Vector3(usableWidth, 0, usableHeight);
        
    //     return new Bounds(center, size);
    // }

    public override Bounds GetBounds(Transform relativeTo)
    {
        // Calculate bounds using THIS class's properties
        float usableWidth = Mathf.Max(0f, plateWidthX - 2f * marginX);
        float usableHeight = Mathf.Max(0f, plateHeightZ - 2f * marginZ);
        
        Vector3 center = new Vector3(
            centerOnOrigin ? relativeTo.position.x : relativeTo.position.x + plateWidthX * 0.5f,
            relativeTo.position.y,
            centerOnOrigin ? relativeTo.position.z : relativeTo.position.z + plateHeightZ * 0.5f
        );

        Vector3 size = new Vector3(usableWidth, 0, usableHeight);
        
        // Return a new Bounds, calculated here
        return new Bounds(center, size);
    }
    
    public override (float min, float max) GetVerticalBounds(float u, Transform relativeTo)
    {
        float topZ = CalculateTopZ(u, relativeTo);
        float bottomZ = CalculateBottomZ(u, relativeTo);
        return (Mathf.Min(topZ, bottomZ), Mathf.Max(topZ, bottomZ));
    }
    
    public override int CalculateTargetPoints(DensityProfile density)
    {
        if (density == null) return 0;
        float fullArea = plateWidthX * plateHeightZ;
        return Mathf.Max(1, Mathf.RoundToInt((fullArea * density.constantPoints) / Mathf.Max(0.000001f, density.baseUnits)));
    }
    
    public override Vector3 ApplySnapping(PointSnappingMode mode, Vector3 originalPos, float u, (float min, float max) bounds, int rowIndex, int totalRows, float verticalAlignment, bool topToBottom)
    {
        float x = originalPos.x;
        float y = originalPos.y;
        float z = originalPos.z;

        // Note: We MUST use the bounds provided, not recalculate, 
        // because the 'u' param might be flipped.
        float topZ = bounds.max;
        float bottomZ = bounds.min;
        float columnHeight = Mathf.Abs(topZ - bottomZ);

        switch (mode)
        {
            case PointSnappingMode.SnapToTopCurve:
                float normalizedRowPos = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedRowPos = 1f - normalizedRowPos;
                z = topZ - (normalizedRowPos * columnHeight);
                break;

            case PointSnappingMode.SnapToBottomCurve:
                float normalizedFromBottom = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) normalizedFromBottom = 1f - normalizedFromBottom;
                z = bottomZ + (normalizedFromBottom * columnHeight);
                break;

            case PointSnappingMode.VerticalAlignment:
                float blendedBase = Mathf.Lerp(bottomZ, topZ, verticalAlignment);
                float rowOffset = totalRows > 1 ? rowIndex / (float)(totalRows - 1) : 0f;
                if (!topToBottom) rowOffset = 1f - rowOffset;
                z = blendedBase + (0.5f - rowOffset) * columnHeight;
                break;

            case PointSnappingMode.NoSnapping:
            default:
                z = originalPos.z; // Keep original uniform grid position
                break;
        }

        return new Vector3(x, y, z);
    }
    
    public override void DrawGizmos(Transform relativeTo, bool showCurvePoints, int curveResolution)
    {
        DrawOuterBounds(relativeTo);
        DrawUsableBounds(relativeTo);

        if (showCurvePoints)
        {
            DrawCurve(relativeTo, (u, t) => CalculateTopZ(u, t), Color.cyan, curveResolution);
            DrawCurve(relativeTo, (u, t) => CalculateBottomZ(u, t), Color.magenta, curveResolution);
        }
    }
    
    public override void OnValidate()
    {
        splitLeft = Mathf.Clamp01(splitLeft);
        splitRight = Mathf.Clamp01(splitRight);
        if (splitRight < splitLeft + 0.01f)
            splitRight = splitLeft + 0.01f;

        if (topCurve == null) topCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);
        if (bottomLeft == null) bottomLeft = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0.3f));
        if (bottomCenter == null) bottomCenter = new AnimationCurve(new Keyframe(0, 0.3f), new Keyframe(1, 0.6f));
        if (bottomRight == null) bottomRight = new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(1, 0));
    }

    #region Private Curve Calculations
    
    private float CalculateTopZ(float u, Transform t)
    {
        float baseTop = GetTopEdgeFlat(t);
        return topCurveMode switch
        {
            CurveMode.Flat => baseTop,
            CurveMode.Parabola => baseTop - (4f * u * (1f - u) * topSag),
            CurveMode.Cosine => baseTop - ((1f - Mathf.Cos(Mathf.PI * u)) * 0.5f * topSag),
            CurveMode.Custom => baseTop - (Mathf.Clamp01(topCurve.Evaluate(u)) * topSag),
            _ => baseTop
        };
    }

    private float CalculateBottomZ(float u, Transform t)
    {
        if (!useTripleBottom)
            return GetBottomEdgeFlat(t);

        float sL = Mathf.Clamp01(splitLeft);
        float sR = Mathf.Clamp01(splitRight);
        if (sR <= sL) sR = sL + 0.01f;

        float normalizedRise = u <= sL
            ? EvaluateSegment(bottomLeft, u, 0f, sL)
            : u < sR
                ? EvaluateSegment(bottomCenter, u, sL, sR)
                : EvaluateSegment(bottomRight, u, sR, 1f);

        return GetBottomEdgeFlat(t) + normalizedRise * bottomMaxRise;
    }

    private float EvaluateSegment(AnimationCurve curve, float u, float segmentStart, float segmentEnd)
    {
        float span = Mathf.Max(1e-6f, segmentEnd - segmentStart);
        float t = Mathf.Clamp01((u - segmentStart) / span);
        return Mathf.Clamp01(curve.Evaluate(t));
    }
    
    #endregion
    
    #region Private Geometry Helpers
    
    private float GetLeftEdge(Transform t)
    {
        float baseX = centerOnOrigin ? t.position.x - plateWidthX * 0.5f : t.position.x;
        return baseX + marginX;
    }

    private float GetRightEdge(Transform t)
    {
        float baseX = centerOnOrigin ? t.position.x + plateWidthX * 0.5f : t.position.x + plateWidthX;
        return baseX - marginX;
    }

    private float GetTopEdgeFlat(Transform t)
    {
        float baseZ = centerOnOrigin ? t.position.z + plateHeightZ * 0.5f : t.position.z + plateHeightZ;
        return baseZ - marginZ;
    }

    private float GetBottomEdgeFlat(Transform t)
    {
        float baseZ = centerOnOrigin ? t.position.z - plateHeightZ * 0.5f : t.position.z;
        return baseZ + marginZ;
    }
    
    private Vector3 GetOuterCorner(Transform t, float offsetX, float offsetZ)
    {
        float x = centerOnOrigin
            ? t.position.x - plateWidthX * 0.5f + offsetX
            : t.position.x + offsetX;
        float z = centerOnOrigin
            ? t.position.z - plateHeightZ * 0.5f + offsetZ
            : t.position.z + offsetZ;
        return new Vector3(x, t.position.y, z);
    }
    
    #endregion

    #region Private Gizmo Helpers

    private void DrawOuterBounds(Transform t)
    {
        Vector3 bl = GetOuterCorner(t, 0, 0);
        Vector3 br = GetOuterCorner(t, plateWidthX, 0);
        Vector3 tl = GetOuterCorner(t, 0, plateHeightZ);
        Vector3 tr = GetOuterCorner(t, plateWidthX, plateHeightZ);

        Gizmos.color = Color.white; // outerBoundsColor
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }

    private void DrawUsableBounds(Transform t)
    {
        Vector3 bl = new Vector3(GetLeftEdge(t), t.position.y, GetBottomEdgeFlat(t));
        Vector3 br = new Vector3(GetRightEdge(t), t.position.y, GetBottomEdgeFlat(t));
        Vector3 tl = new Vector3(GetLeftEdge(t), t.position.y, GetTopEdgeFlat(t));
        Vector3 tr = new Vector3(GetRightEdge(t), t.position.y, GetTopEdgeFlat(t));

        Gizmos.color = Color.gray; // usableBoundsColor
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
    
    private void DrawCurve(Transform t, Func<float, Transform, float> curveFunction, Color color, int segments)
    {
        Gizmos.color = color;
        Vector3 previousPoint = Vector3.zero;
        float xLeft = GetLeftEdge(t);
        float xRight = GetRightEdge(t);

        for (int i = 0; i <= segments; i++)
        {
            float u = i / (float)segments;
            // Note: We draw based on L-R, but calculate U based on 'leftToRight'
            float x = Mathf.Lerp(xLeft, xRight, u); 
            // float uParam = leftToRight ? u : 1f - u; // This is handled by PointGenerator's UParam calc
            
            float z = curveFunction(u, t);
            Vector3 point = new Vector3(x, t.position.y, z);

            if (i > 0)
                Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
        }
    }
    
    #endregion
}