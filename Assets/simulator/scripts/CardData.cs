using UnityEngine;

[CreateAssetMenu(fileName = "New Crystal Data", menuName = "Crystal/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string itemName;
    [SerializeField] private string Email;
    [SerializeField] private string salesmanID;
    
    [Header("Crystal Configuration")]
    [SerializeField] private string[] selectedCrystals;
    [SerializeField] private int numberOfCrystals;
    [SerializeField] private string[] colorsOfCrystals;
    
    [Header("Design Details")]
    [SerializeField] private string compStyle;
    [SerializeField] private int numberOfWires;
    [SerializeField] private string baseShape;

    // Public Properties
    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    public string SalesmanID
    {
        get => salesmanID;
        set => salesmanID = value;
    }

    public string EmailAddress
    {
        get => Email;
        set => Email = value;
    }

    public string[] SelectedCrystals
    {
        get => selectedCrystals;
        set => selectedCrystals = value;
    }

    public int NumberOfCrystals
    {
        get => numberOfCrystals;
        set => numberOfCrystals = value;
    }

    public string[] ColorsOfCrystals
    {
        get => colorsOfCrystals;
        set => colorsOfCrystals = value;
    }

    public string CompStyle
    {
        get => compStyle;
        set => compStyle = value;
    }

    public int NumberOfWires
    {
        get => numberOfWires;
        set => numberOfWires = value;
    }

    public string BaseShape
    {
        get => baseShape;
        set => baseShape = value;
    }

    // Helper method to initialize arrays
    public void InitializeArrays(int crystalCount)
    {
        numberOfCrystals = crystalCount;
        selectedCrystals = new string[crystalCount];
        colorsOfCrystals = new string[crystalCount];
    }

    // Helper method to add a crystal
    public void SetCrystal(int index, string crystalType, string crystalColor)
    {
        if (index >= 0 && index < numberOfCrystals)
        {
            selectedCrystals[index] = crystalType;
            colorsOfCrystals[index] = crystalColor;
        }
    }

    // Helper method to get summary
    public string GetSummary()
    {
        return $"Name: {itemName}\n" +
               $"Crystals: {numberOfCrystals}\n" +
               $"Style: {compStyle}\n" +
               $"Base: {baseShape}\n" +
               $"Wires: {numberOfWires}\n" +
               $"Salesman: {salesmanID}";
    }

    // Helper method to clear data
    public void ClearData()
    {
        itemName = "";
        salesmanID = "";
        selectedCrystals = new string[0];
        numberOfCrystals = 0;
        colorsOfCrystals = new string[0];
        compStyle = "";
        numberOfWires = 0;
        baseShape = "";
    }
}