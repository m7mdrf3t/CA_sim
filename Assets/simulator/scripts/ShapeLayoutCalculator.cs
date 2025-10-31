// using UnityEngine;

// [ExecuteAlways]
// public abstract class ShapeLayoutCalculator : MonoBehaviour
// {

//     [SerializeField]
//     private UserConfig userConfig;


//     [Header("Constants (Red Section)")]
//     [Min(0.0001f)] public float baseArea = 1f;
//     [Min(0f)] public float baseSpots = 14f;
//     [Min(0f)] public float basePoints = 120f;

//     [Header("Results (Calculated)")]
//     public float area;
//     public float totalSpots;
//     public float totalPoints;
//     public float density;
//     public float lowDensity;
//     public float mediumDensity;
//     public float highDensity;

//     private float _prevBaseArea, _prevBaseSpots, _prevBasePoints;

//     protected virtual void OnEnable()
//     {
//         CalculateLayout();
//         CacheInputs();
//     }

//     protected virtual void OnValidate()
//     {
//         baseArea = Mathf.Max(0.0001f, baseArea);
//         baseSpots = Mathf.Max(0f, baseSpots);
//         basePoints = Mathf.Max(0f, basePoints);

//         CalculateLayout();

// #if UNITY_EDITOR
//         if (!Application.isPlaying && InputsChanged())
//         {
//             CacheInputs();
//             Debug.Log($"[{GetType().Name}] Recalculated — Area: {area:F3}, Points: {totalPoints:F0}");
//         }
// #endif
//     }

//     protected abstract float CalculateArea();

//     [ContextMenu("Recalculate Layout")]
//     public virtual void CalculateLayout()
//     {
//         area = CalculateArea();
//         totalSpots = (baseSpots * area) / baseArea;
//         totalPoints = (basePoints * area) / baseArea;
//         density = 100f;

//         highDensity = totalPoints;
//         mediumDensity = totalPoints * 0.75f;
//         lowDensity = totalPoints * 0.375f;

// //        userConfig.area = area;
//         userConfig.totalSpots = totalSpots;
//         userConfig.totalPoints = totalPoints;
//         userConfig.density = density;

//         // Assign densities
//         userConfig.highDensity = highDensity;
//         userConfig.mediumDensity = mediumDensity;
//         userConfig.lowDensity = lowDensity;


//         Debug.Log($"Area: {area:F2} m²");
//         Debug.Log($"Total Spots: {totalSpots:F0}");
//         Debug.Log($"Total Points: {totalPoints:F0}");
//         Debug.Log($"Density: {density:F2}%");
//     }

//     private void CacheInputs()
//     {
//         _prevBaseArea = baseArea;
//         _prevBaseSpots = baseSpots;
//         _prevBasePoints = basePoints;
//     }

//     private bool InputsChanged()
//     {
//         return !Mathf.Approximately(_prevBaseArea, baseArea)
//             || !Mathf.Approximately(_prevBaseSpots, baseSpots)
//             || !Mathf.Approximately(_prevBasePoints, basePoints);
//     }
// }


using UnityEngine;

[ExecuteAlways]
public abstract class ShapeLayoutCalculator : MonoBehaviour
{
    [SerializeField]
    private UserConfig userConfig;

    // -------------------- INPUTS --------------------
    [Header("Constants (Red Section)")]
    [Min(0.0001f)] public float baseArea = 1f;     // reference area (m²)
    [Min(0f)] public float baseSpots = 14f;        // mirrors (cylinders) per m²
    [Min(0f)] public float basePoints = 120f;      // bead points per m²

    [Header("Mirror Pricing")]
    [Min(0f)] public float mirrorPricePerSqM = 20000f; // cost per m² of mirrors

    [Header("Bead Parameters")]
    [Min(0f)] public float beadHeightPerPointMeters = 1.5f; // meters per point
    [Min(0f)] public float beadPricePerMeter = 3f;          // cost per meter
    [Min(1)]  public int   beadStrands = 1;                 // optional multiplier

    // -------------------- OUTPUTS --------------------
    [Header("Results (Calculated)")]
    public float area;         
    public float totalSpots;   
    public float totalPoints;  
    public float density;      
    public float lowDensity;
    public float mediumDensity;
    public float highDensity;

    [Header("Mirror Results")]
    public int   mirrorCylinders; // rounded totalSpots
    public float mirrorCost;      // area * mirrorPricePerSqM

    [Header("Bead Results")]
    public float beadTotalMeters; // totalPoints * beadHeightPerPointMeters
    public float beadCost;        // beadTotalMeters * beadPricePerMeter * beadStrands

    private float _prevBaseArea, _prevBaseSpots, _prevBasePoints;

    protected virtual void OnEnable()
    {
        CalculateLayout();
        CacheInputs();
    }

    protected virtual void OnValidate()
    {
        baseArea   = Mathf.Max(0.0001f, baseArea);
        baseSpots  = Mathf.Max(0f, baseSpots);
        basePoints = Mathf.Max(0f, basePoints);

        mirrorPricePerSqM        = Mathf.Max(0f, mirrorPricePerSqM);
        beadHeightPerPointMeters = Mathf.Max(0f, beadHeightPerPointMeters);
        beadPricePerMeter        = Mathf.Max(0f, beadPricePerMeter);
        beadStrands              = Mathf.Max(1, beadStrands);

        CalculateLayout();

#if UNITY_EDITOR
        if (!Application.isPlaying && InputsChanged())
        {
            CacheInputs();
            Debug.Log($"[{GetType().Name}] Recalculated — Area: {area:F3}, Mirrors: {mirrorCylinders}, MirrorCost: {mirrorCost:F0}, BeadMeters: {beadTotalMeters:F2}, BeadCost: {beadCost:F0}");
        }
#endif
    }

    protected abstract float CalculateArea();

    [ContextMenu("Recalculate Layout")]
    public virtual void CalculateLayout()
    {
        // --- Base calculations ---
        area        = CalculateArea();
        totalSpots  = (baseSpots  * area) / baseArea;
        totalPoints = (basePoints * area) / baseArea;
        density     = 100f;

        highDensity   = totalPoints;
        mediumDensity = totalPoints * 0.75f;
        lowDensity    = totalPoints * 0.375f;

        // --- Mirror Calculations ---
        mirrorCylinders = Mathf.Max(0, Mathf.RoundToInt(totalSpots));
        mirrorCost      = area * mirrorPricePerSqM;

        // --- Bead Calculations ---
        beadTotalMeters = Mathf.Max(0f, totalPoints) * beadHeightPerPointMeters;
        beadCost        = beadTotalMeters * beadPricePerMeter * beadStrands;

        // --- Sync results to UserConfig ---
        if (userConfig != null)
        {
            userConfig.area          = area;
            userConfig.totalSpots    = totalSpots;
            userConfig.totalPoints   = totalPoints;
            userConfig.density       = density;
            userConfig.highDensity   = highDensity;
            userConfig.mediumDensity = mediumDensity;
            userConfig.lowDensity    = lowDensity;

            // Newly added mirror and bead data
            userConfig.mirrorCost      = mirrorCost;
            userConfig.beadCost        = beadCost;
        }

        Debug.Log($"Area: {area:F2} m² | Mirrors: {mirrorCylinders} | MirrorCost: {mirrorCost:F0} | BeadMeters: {beadTotalMeters:F2} | BeadCost: {beadCost:F0}");
    }

    private void CacheInputs()
    {
        _prevBaseArea   = baseArea;
        _prevBaseSpots  = baseSpots;
        _prevBasePoints = basePoints;
    }

    private bool InputsChanged()
    {
        return !Mathf.Approximately(_prevBaseArea, baseArea)
            || !Mathf.Approximately(_prevBaseSpots, baseSpots)
            || !Mathf.Approximately(_prevBasePoints, basePoints);
    }
}