using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShapeUIControllerTMP : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ShapeManager manager;
    [SerializeField] private TMP_Dropdown shapeDropdown;
    [SerializeField] private Button calculateButton;

    [Header("Input Fields")]
    public TMP_InputField lengthField;
    public TMP_InputField widthField;
    public TMP_InputField diameterField;
    public TMP_InputField majorAxisField;
    public TMP_InputField minorAxisField;
    public TMP_InputField heightField;
    public TMP_InputField helixCountField;

    void Awake()
    {
        if (shapeDropdown != null && shapeDropdown.options.Count == 0)
        {
            shapeDropdown.ClearOptions();
            shapeDropdown.AddOptions(System.Enum.GetNames(typeof(ShapeType)).ToList());
        }

        if (shapeDropdown != null)
            shapeDropdown.onValueChanged.AddListener(OnShapeDropdownChanged);

        if (calculateButton != null)
            calculateButton.onClick.AddListener(OnCalculateClicked);
    }

    public void OnShapeDropdownChanged(int index)
    {
        if (manager == null) return;
        manager.SwitchTo((CalcShapeType)index, recalc: false);
    }

    public void OnCalculateClicked()
    {
        Debug.Log("Calculate button clicked");
        if (manager == null) return;

        // push UI values into the correct calculator
        var shape = manager.ActiveCalculator;
        if (shape == null) return;


        switch (manager.activeShape)
        {
            case CalcShapeType.Rectangle:
                var rect = manager.GetActive<RectangleLayoutCalculator>();
                rect.length = Parse(lengthField);
                rect.width  = Parse(widthField);
                rect.height = Parse(heightField);
                break;

            case CalcShapeType.Circle:
                var circle = manager.GetActive<CircleLayoutCalculator>();
                circle.diameter = Parse(diameterField);
                break;

            case CalcShapeType.Oval:
                var oval = manager.GetActive<OvalLayoutCalculator>();
                oval.majorAxis = Parse(majorAxisField);
                oval.minorAxis = Parse(minorAxisField);
                break;

            case CalcShapeType.Helix:
                var helix = manager.GetActive<HelixLayoutCalculator>();
                helix.diameter   = Parse(diameterField);
                helix.height     = Parse(heightField);
                helix.helixCount = Mathf.RoundToInt(Parse(helixCountField));
                break;
        }

        // var beads = manager.GetActive<BeadsShapeCalculator>();
        // beads.length = Parse(lengthField);



        manager.Recalculate();
    }

    private float Parse(TMP_InputField field)
    {
        if (field == null || string.IsNullOrWhiteSpace(field.text))
            return 0f;
        float.TryParse(field.text, out float val);
        return val;
    }
}