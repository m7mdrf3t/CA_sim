using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveUserDataDim : MonoBehaviour
{
    [Header("UI Input Fields")]
    [SerializeField] private TMP_InputField inputX;
    [SerializeField] private TMP_InputField inputY;

    [SerializeField] private TMP_InputField inputZ;

    [Header("Target Scriptable Object")]
    [SerializeField] private UserConfig xyData;

    // Called by a Button OnClick or manually
    public void SaveInputsToSO()
    {
        if (xyData == null)
        {
            Debug.LogWarning("XYData ScriptableObject not assigned!");
            return;
        }

        Debug.Log("Saving input values to UserConfig...");

        if (float.TryParse(inputX.text, out float parsedX) &&
            float.TryParse(inputY.text, out float parsedY) && 
            float.TryParse(inputZ.text, out float parsedZ))
        {
            xyData.xSize = parsedX;
            xyData.ySize = parsedY;
            xyData.zSize = parsedZ;


#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(xyData); // Mark as dirty for saving in Editor
#endif
            Debug.Log($"Saved values → X: {xyData.xSize}, Y: {xyData.ySize}");
        }
        else
        {
            Debug.LogWarning("Invalid input: Please enter numeric values for X and Y.");
        }
    }
}
