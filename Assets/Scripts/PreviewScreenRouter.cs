using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// When the user presses Next, shows Preview 1..N based on how many items were selected.
/// If 1 selected → only "Preview 1" active; if 3 selected → "Preview 1,2,3" active, etc.
/// </summary>
public class PreviewScreenRouter : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private SelectedItemsUI selectedItemsUI;
    [SerializeField] private Button nextButton;

    [Header("Targets (exactly 4)")]
    [SerializeField] private GameObject preview1;
    [SerializeField] private GameObject preview2;
    [SerializeField] private GameObject preview3;
    [SerializeField] private GameObject preview4;

    // Optional: auto-find by name if not assigned
    [Header("Optional")]
    [SerializeField] private bool autoFindByName = true;

    private GameObject[] previews;

    private void Awake()
    {
        previews = new[]
        {
            preview1, preview2, preview3, preview4
        };

        if (autoFindByName)
        {
            AutoFindPreview(ref previews[0], "Preview 1");
            AutoFindPreview(ref previews[1], "Preview 2");
            AutoFindPreview(ref previews[2], "Preview 3");
            AutoFindPreview(ref previews[3], "Preview 4");
        }

        // Safety: disable all previews initially
        SetActiveCount(0);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNext);
        else
            Debug.LogWarning("[PreviewScreenRouter] NextButton not assigned.");

        if (selectedItemsUI == null)
            Debug.LogWarning("[PreviewScreenRouter] SelectedItemsUI not assigned.");
    }

    private void OnDestroy()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(OnNext);
    }

    private void OnNext()
    {
        if (selectedItemsUI == null)
        {
            Debug.LogWarning("[PreviewScreenRouter] No SelectedItemsUI; cannot route previews.");
            return;
        }

        int count = Mathf.Clamp(selectedItemsUI.SelectionCount, 0, 4);

        if (count == 0)
        {
            Debug.Log("[PreviewScreenRouter] No items selected yet.");
            // You can show a toast/prompt here if you wish.
            return;
        }

        // Show only the first N preview panels
        SetActiveCount(count);

        // Populate crystals in each active preview
        for (int i = 0; i < count; i++)
        {
            if (previews[i] != null)
            {
                var populator = previews[i].GetComponentInChildren<PreviewCrystalPopulator>();
                if (populator != null)
                {
                    populator.PopulateCrystals();
                }
            }
        }

        // (Optional) If your Preview panels live on another screen,
        // this is where you would switch screens / play a transition.
        // e.g., Screens.Show("PreviewComponentsScreen");
    }

    private void SetActiveCount(int count)
    {
        for (int i = 0; i < 4; i++)
        {
            if (previews[i] != null)
                previews[i].SetActive(i < count);
        }
    }

    private void AutoFindPreview(ref GameObject slot, string name)
    {
        if (slot != null) return;
        var t = transform.root.Find($"PreviewComponentsScreen/{name}");
        if (t == null) t = GameObject.Find(name)?.transform;
        if (t != null) slot = t.gameObject;
    }
}