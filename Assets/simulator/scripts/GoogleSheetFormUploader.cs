using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class GoogleSheetFormUploader : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField fullNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField phoneInput;
    public TMP_Dropdown optionADropdown;
    public TMP_Dropdown optionBDropdown;
    public Button continueButton;
    public TextMeshProUGUI statusLabel; // optional

    [Header("Apps Script Webhook")]
    [Tooltip("Paste your Apps Script Web app URL here")]
    public string webAppUrl = "https://script.google.com/macros/s/XXXXX/exec";
    [Tooltip("Must match SHARED_SECRET in Apps Script")]
    public string sharedSecret = "PUT_A_LONG_RANDOM_SECRET_HERE";

    void Awake()
    {
        if (continueButton) continueButton.onClick.AddListener(() => _ = OnContinuePressed());
    }

    public async Task OnContinuePressed()
    {
        try
        {
            var fullName = fullNameInput?.text?.Trim() ?? "";
            var email    = emailInput?.text?.Trim() ?? "";
            var phone    = phoneInput?.text?.Trim() ?? "";
            var optionA  = GetDropdownText(optionADropdown);
            var optionB  = GetDropdownText(optionBDropdown);

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email))
            {
                SetStatus("Please fill in Full Name and Email.");
                return;
            }

            SetStatus("Sending…");

            var payload = new Payload {
                secret  = sharedSecret,
                fullName = fullName,
                email    = email,
                phone    = phone,
                optionA  = optionA,
                optionB  = optionB
            };

            string json = JsonUtility.ToJson(payload);

            using (var req = new UnityWebRequest(webAppUrl, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
                bool isError = req.result != UnityWebRequest.Result.Success;
#else
                bool isError = req.isNetworkError || req.isHttpError;
#endif
                if (isError)
                {
                    Debug.LogError($"POST failed: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                    SetStatus("Failed to save.");
                }
                else
                {
                    // Expecting {"ok":true}
                    Debug.Log($"Response: {req.downloadHandler.text}");
                    SetStatus("Saved! ✅");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            SetStatus("Unexpected error.");
        }
    }

    private string GetDropdownText(TMP_Dropdown dd)
    {
        if (dd == null || dd.options == null || dd.options.Count == 0) return "";
        return dd.options[dd.value].text?.Trim() ?? "";
    }

    private void SetStatus(string msg)
    {
        if (statusLabel) statusLabel.text = msg;
        Debug.Log(msg);
    }

    [Serializable]
    private class Payload
    {
        public string secret;
        public string fullName;
        public string email;
        public string phone;
        public string optionA;
        public string optionB;
    }
}