using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CrystalDataManager : MonoBehaviour
{
    [Header("Crystal Data Reference")]
    [SerializeField] private CardData crystalData;

    // Individual methods to assign each field

    // Basic Information
    public void SetName(string name)
    {
        if (crystalData != null)
        {
            crystalData.ItemName = name;
            Debug.Log($"Name set to: {name}");
        }
    }

    public void SetEmail(string email)
    {
        if (crystalData != null)
        {
            crystalData.EmailAddress = email;
            Debug.Log($"Email set to: {email}");
        }
    }

    public void SetSalesmanID(string id)
    {
        if (crystalData != null)
        {
            crystalData.SalesmanID = id;
            Debug.Log($"Salesman ID set to: {id}");
        }
    }

    // Crystal Configuration
    public void SetNumberOfCrystals(int count)
    {
        if (crystalData != null)
        {
            crystalData.NumberOfCrystals = count;
            crystalData.InitializeArrays(count);
            Debug.Log($"Number of crystals set to: {count}");
        }
    }

    public void SetSelectedCrystal(int index, string crystalType)
    {
        if (crystalData != null && crystalData.SelectedCrystals != null)
        {
            if (index >= 0 && index < crystalData.SelectedCrystals.Length)
            {
                crystalData.SelectedCrystals[index] = crystalType;
                Debug.Log($"Crystal at index {index} set to: {crystalType}");
            }
            else
            {
                Debug.LogWarning($"Index {index} is out of range!");
            }
        }
    }

    public void SetCrystalColor(int index, string color)
    {
        if (crystalData != null && crystalData.ColorsOfCrystals != null)
        {
            if (index >= 0 && index < crystalData.ColorsOfCrystals.Length)
            {
                crystalData.ColorsOfCrystals[index] = color;
                Debug.Log($"Crystal color at index {index} set to: {color}");
            }
            else
            {
                Debug.LogWarning($"Index {index} is out of range!");
            }
        }
    }

    // Design Details
    public void SetCompStyle(string style)
    {
        if (crystalData != null)
        {
            crystalData.CompStyle = style;
            Debug.Log($"Comp style set to: {style}");
        }
    }

    public void SetNumberOfWires(int count)
    {
        if (crystalData != null)
        {
            crystalData.NumberOfWires = count;
            Debug.Log($"Number of wires set to: {count}");
        }
    }

    public void SetBaseShape(string shape)
    {
        if (crystalData != null)
        {
            crystalData.BaseShape = shape;
            Debug.Log($"Base shape set to: {shape}");
        }
    }

    // Methods to work with UI InputFields
    public void SetNameFromInputField(TMP_InputField inputField)
    {
        if (inputField != null)
        {
            SetName(inputField.text);
        }
    }

    // Methods to work with UI InputFields
    public void SetEmailFromInputField(TMP_InputField inputField)
    {
        if (inputField != null)
        {
            SetName(inputField.text);
        }
    }

    public void SetSalesmanIDFromInputField(TMP_InputField inputField)
    {
        if (inputField != null)
        {
            SetSalesmanID(inputField.text);
        }
    }

    public void SetNumberOfCrystalsFromInputField(InputField inputField)
    {
        if (inputField != null && int.TryParse(inputField.text, out int count))
        {
            SetNumberOfCrystals(count);
        }
    }

    public void SetCompStyleFromInputField(InputField inputField)
    {
        if (inputField != null)
        {
            SetCompStyle(inputField.text);
        }
    }

    public void SetNumberOfWiresFromInputField(InputField inputField)
    {
        if (inputField != null && int.TryParse(inputField.text, out int count))
        {
            SetNumberOfWires(count);
        }
    }

    public void SetBaseShapeFromInputField(InputField inputField)
    {
        if (inputField != null)
        {
            SetBaseShape(inputField.text);
        }
    }

    // Getter methods
    public string GetName()
    {
        return crystalData != null ? crystalData.ItemName : "";
    }

    public string GetSalesmanID()
    {
        return crystalData != null ? crystalData.SalesmanID : "";
    }

    public int GetNumberOfCrystals()
    {
        return crystalData != null ? crystalData.NumberOfCrystals : 0;
    }

    public string GetSelectedCrystal(int index)
    {
        if (crystalData != null && crystalData.SelectedCrystals != null)
        {
            if (index >= 0 && index < crystalData.SelectedCrystals.Length)
                return crystalData.SelectedCrystals[index];
        }
        return "";
    }

    public string GetCrystalColor(int index)
    {
        if (crystalData != null && crystalData.ColorsOfCrystals != null)
        {
            if (index >= 0 && index < crystalData.ColorsOfCrystals.Length)
                return crystalData.ColorsOfCrystals[index];
        }
        return "";
    }

    public string GetCompStyle()
    {
        return crystalData != null ? crystalData.CompStyle : "";
    }

    public int GetNumberOfWires()
    {
        return crystalData != null ? crystalData.NumberOfWires : 0;
    }

    public string GetBaseShape()
    {
        return crystalData != null ? crystalData.BaseShape : "";
    }

    // Utility methods
    public void PrintSummary()
    {
        if (crystalData != null)
        {
            Debug.Log(crystalData.GetSummary());
        }
    }

    public void ClearAllData()
    {
        if (crystalData != null)
        {
            crystalData.ClearData();
            Debug.Log("All data cleared!");
        }
    }
}