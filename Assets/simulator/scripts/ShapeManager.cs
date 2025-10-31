using UnityEngine;

public enum CalcShapeType
{
    Rectangle,
    Circle,
    Oval,
    Helix
}

[ExecuteAlways]
[DisallowMultipleComponent]
public class ShapeManager : MonoBehaviour
{
    [Header("Active Shape")]
    public CalcShapeType activeShape = CalcShapeType.Rectangle;

    [Header("Shared Constants")]
    [Min(0.0001f)] public float baseArea   = 1f;
    [Min(0f)]       public float baseSpots  = 14f;
    [Min(0f)]       public float basePoints = 120f;

    // Cached components (created lazily)
    public RectangleLayoutCalculator rectangle;
    public CircleLayoutCalculator    circle;
    public OvalLayoutCalculator      oval;
    public HelixLayoutCalculator     helix;
    

    // Current active calculator
    public ShapeLayoutCalculator ActiveCalculator { get; private set; }

    void OnEnable()
    {
        EnsureCalculators();
        ApplySharedConstantsToAll();
        SwitchTo(activeShape, recalc: true);
    }

    void OnValidate()
    {
        EnsureCalculators();
        ApplySharedConstantsToAll();
        SwitchTo(activeShape, recalc: true);
    }

    public void SwitchTo(CalcShapeType newType, bool recalc = true)
    {
        activeShape = newType;

        // Disable all, then enable the chosen one
        EnableCalculator(rectangle, false);
        EnableCalculator(circle,    false);
        EnableCalculator(oval,      false);
        EnableCalculator(helix,     false);

        switch (activeShape)
        {
            case CalcShapeType.Rectangle:
                ActiveCalculator = rectangle; break;
            case CalcShapeType.Circle:
                ActiveCalculator = circle;    break;
            case CalcShapeType.Oval:
                ActiveCalculator = oval;      break;
            case CalcShapeType.Helix:
                ActiveCalculator = helix;     break;
        }

        EnableCalculator(ActiveCalculator, true);

        //if (recalc && ActiveCalculator != null)
           // ActiveCalculator.CalculateLayout();
    }

    public void Recalculate()
    {
        if (ActiveCalculator != null)
            ActiveCalculator.CalculateLayout();
    }

    public T GetActive<T>() where T : ShapeLayoutCalculator
    {
        return ActiveCalculator as T;
    }

    private void EnsureCalculators()
    {
        if (rectangle == null) rectangle = GetOrAdd<RectangleLayoutCalculator>();
        if (circle    == null) circle    = GetOrAdd<CircleLayoutCalculator>();
        if (oval      == null) oval      = GetOrAdd<OvalLayoutCalculator>();
        if (helix     == null) helix     = GetOrAdd<HelixLayoutCalculator>();
    }

    private T GetOrAdd<T>() where T : ShapeLayoutCalculator
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    private void EnableCalculator(Behaviour calc, bool enabled)
    {
        if (calc == null) return;
        calc.enabled = enabled;

#if UNITY_EDITOR
        // Keep the Inspector clean: hide inactive components
        UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(calc, enabled);
#endif
    }

    private void ApplySharedConstantsToAll()
    {
        void Apply(ShapeLayoutCalculator c)
        {
            if (c == null) return;
            c.baseArea   = baseArea;
            c.baseSpots  = baseSpots;
            c.basePoints = basePoints;
        }

        Apply(rectangle);
        Apply(circle);
        Apply(oval);
        Apply(helix);
    }
}