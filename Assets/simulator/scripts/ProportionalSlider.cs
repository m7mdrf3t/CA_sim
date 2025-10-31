using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class ProportionalSlider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider slider;
    
#if TMP_PRESENT
    [SerializeField] private TMP_Text percentageText;
#else
    [SerializeField] private Text percentageText;
#endif

    [Header("Crystal Data")]
    public string crystalVariantName;
    public Color crystalColor;

    private ProportionalSliderManager manager;
    private float currentRatio;
    private bool isInitialized = false;

    public float Ratio 
    { 
        get => currentRatio;
        set 
        {
            currentRatio = value;
            if (slider != null)
            {
                slider.value = value;
            }
            UpdatePercentageDisplay();
        }
    }

    public Slider GetSlider() => slider;

    private void Awake()
    {
        if (slider == null)
            slider = GetComponentInChildren<Slider>();
    }

    public void Initialize(ProportionalSliderManager mgr, float initialRatio, string variantName, Color color)
    {
        manager = mgr;
        currentRatio = initialRatio;
        crystalVariantName = variantName;
        crystalColor = color;
        
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = initialRatio;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        UpdatePercentageDisplay();
        isInitialized = true;
    }

    private void OnSliderValueChanged(float newValue)
    {
        if (!isInitialized || manager == null) return;
        manager.OnSliderChanged(this, newValue);
    }

    private void UpdatePercentageDisplay()
    {
        if (percentageText != null)
        {
            percentageText.text = $"{currentRatio:F1}%";
        }
    }

    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveAllListeners();
        }

        if (manager != null)
        {
            manager.UnregisterSlider(this);
        }
    }
}
