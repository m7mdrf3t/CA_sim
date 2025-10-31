using System;
using TMPro;
using UnityEngine;

public class SettingPooulator : MonoBehaviour
{


    [SerializeField]
    private UserConfig userConfig;

    [SerializeField]
    private TMP_Text showBeads;

    [SerializeField]
    private TMP_Text showBase;

    [SerializeField]
    private TMP_Text ColorStyle;

    [SerializeField]
    private TMP_Text Density;

    private void OnEnable() {
        
        PopulateSetting();

    }

    public void PopulateSetting()
    {

        showBeads.text = userConfig.showBeads? "Yes" : "No";
        showBase.text = userConfig.showBeads? "Yes" : "No";
        ColorStyle.text = userConfig.colorStyle.ToString();
        Density.text = userConfig.densityLevel.ToString();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PopulateSetting();
    }
}
