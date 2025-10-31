using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GatekeeperOverlay : MonoBehaviour
{
    [Header("Required refs")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TMP_InputField adminCodeInput;
    [SerializeField] private Button submitButton;

    [Header("UX")]
    [SerializeField] private TextMeshProUGUI feedbackText; // optional: assign a small text under the button
    [SerializeField] private bool autoFocusInput = true;

    const string TAG = "[GatekeeperOverlay]";

    void Awake()
    {
        Debug.Log($"{TAG} Awake()");
        // Defensive: if button is assigned, wire it programmatically
        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(OnSubmit);
            submitButton.onClick.AddListener(OnSubmit);
            Debug.Log($"{TAG} submitButton listener attached.");
        }
        else
        {
            Debug.LogError($"{TAG} submitButton is NOT assigned!");
        }
    }

    void OnEnable()
    {
        Debug.Log($"{TAG} OnEnable()");
        if (autoFocusInput && adminCodeInput != null)
        {
            // autofocus after a frame to ensure layout is ready
            StartCoroutine(FocusNextFrame());
        }
    }

    System.Collections.IEnumerator FocusNextFrame()
    {
        yield return null;
        try
        {
            adminCodeInput.ActivateInputField();
            adminCodeInput.Select();
            Debug.Log($"{TAG} Input focused.");
        }
        catch { }
    }

    public void Show(string message, bool adminMode)
    {
        Debug.Log($"{TAG} Show(adminMode={adminMode})  msg='{message}'");

        if (rootCanvas != null) rootCanvas.enabled = true;
        if (panel != null) panel.SetActive(true);

        if (messageText != null) messageText.text = message;

        if (adminCodeInput != null) adminCodeInput.gameObject.SetActive(adminMode);
        if (submitButton != null) submitButton.gameObject.SetActive(adminMode);
        if (feedbackText != null) feedbackText.text = string.Empty;

        // sanity logs for references
        if (rootCanvas == null) Debug.LogError($"{TAG} rootCanvas not assigned");
        if (panel == null) Debug.LogError($"{TAG} panel not assigned");
        if (messageText == null) Debug.LogWarning($"{TAG} messageText not assigned");
        if (adminCodeInput == null) Debug.LogWarning($"{TAG} adminCodeInput not assigned");
        if (submitButton == null) Debug.LogWarning($"{TAG} submitButton not assigned");
    }

    public void Hide()
    {
        Debug.Log($"{TAG} Hide()");
        if (panel != null) panel.SetActive(false);
        if (rootCanvas != null) rootCanvas.enabled = false;
        if (feedbackText != null) feedbackText.text = string.Empty;
    }

    private void OnSubmit()
    {
        Debug.Log($"{TAG} OnSubmit() clicked.");

        if (Gatekeeper.I == null)
        {
            Debug.LogError($"{TAG} Gatekeeper.I is NULL. Is the Gatekeeper object in the scene?");
            SetFeedback("System error. Gatekeeper not found.");
            return;
        }

        if (adminCodeInput == null)
        {
            Debug.LogError($"{TAG} adminCodeInput is NULL.");
            SetFeedback("No input assigned.");
            return;
        }

        string code = adminCodeInput.text;
        Debug.Log($"{TAG} Submitting code (len={code?.Length ?? 0}).");

        if (string.IsNullOrEmpty(code))
        {
            SetFeedback("Please enter a code.");
            return;
        }

        bool accepted = Gatekeeper.I.TrySubmitAdminCode(code);

        if (accepted)
        {
            Debug.Log($"{TAG} Code accepted by Gatekeeper.");
            SetFeedback(""); // clear
            // Gatekeeper will hide overlay if appropriate; we also clear input
            adminCodeInput.text = "";
        }
        else
        {
            Debug.LogWarning($"{TAG} Code rejected by Gatekeeper.");
            SetFeedback("Invalid code. Try again.");
            // keep focus for quick retry
            adminCodeInput.ActivateInputField();
            adminCodeInput.Select();
        }
    }

    private void SetFeedback(string msg)
    {
        if (feedbackText != null) feedbackText.text = msg;
    }
}