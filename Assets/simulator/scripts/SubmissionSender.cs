using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro; // OK if you use TextMeshPro; if not, leave fields empty

namespace Simulator.Scripts
{
    public class SubmissionSender : MonoBehaviour
    {
        [Header("Apps Script")]
        [SerializeField] private string appsScriptExecUrl =
            "https://script.google.com/macros/s/AKfycbzr49DER9O1SKCLsM-2rZgRHiHM6_KaVKEIp2CeC9FpT1NoY6dLm297EE5yBhP1bZDX/exec";
        [SerializeField] private string sharedSecret = "AsfourCG+";

        [Header("UI - Text Inputs (assign one type per field)")]
        [SerializeField] private TMP_InputField nameTMP;
        [SerializeField] private TMP_InputField emailTMP;
        [SerializeField] private TMP_InputField phoneTMP;

        [SerializeField] private InputField nameUGUI;
        [SerializeField] private InputField emailUGUI;
        [SerializeField] private InputField phoneUGUI;

        [Header("UI - Dropdowns (assign one type per field)")]
        [SerializeField] private TMP_Dropdown optionATMP;
        [SerializeField] private TMP_Dropdown optionBTMP;

        [SerializeField] private Dropdown optionAUGUI;
        [SerializeField] private Dropdown optionBUGUI;

        [Header("UI - Controls / Feedback")]
        [SerializeField] private Button continueButton;     // your “Join/Continue” button
        [SerializeField] private Text statusTextUGUI;       // optional legacy Text for inline status
        [SerializeField] private TMP_Text statusTextTMP;    // optional TMP_Text for inline status

        [Header("Behavior")]
        [Tooltip("Fetch the echo page after 302 just to log the body (not required).")]
        [SerializeField] private bool fetchEchoAfterRedirect = false;

        [Tooltip("HTTP timeout seconds for each request.")]
        [SerializeField] private int timeoutSeconds = 15;

        [Tooltip("Max retry attempts on 429/503.")]
        [SerializeField] private int maxAttempts = 5;

        private bool _isSending;
        private bool _lastSendWasSuccess;
        private long _lastCode;

        // -------- Public entrypoint for Button --------
        public void OnContinueClicked()
        {
            if (_isSending) return;

            // Read UI values
            string fullName = ReadInput(nameTMP, nameUGUI);
            string email    = ReadInput(emailTMP, emailUGUI);
            string phone    = ReadInput(phoneTMP, phoneUGUI);
            string optionA  = ReadDropdown(optionATMP, optionAUGUI);
            string optionB  = ReadDropdown(optionBTMP, optionBUGUI);

            // Minimal validation
            if (string.IsNullOrWhiteSpace(fullName))
            {
                FailUI("Full name is required.");
                return;
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                FailUI("Email is required.");
                return;
            }

            StartCoroutine(SendSubmissionWithRetry(fullName, email, phone, optionA, optionB));
        }

        // -------- Retry wrapper --------
        private IEnumerator SendSubmissionWithRetry(string fullName, string email, string phone, string optionA, string optionB)
        {
            _isSending = true;
            SetButtonInteractable(false);
            InfoUI("Submitting…");

            int attempt = 0;

            while (true)
            {
                attempt++;
                yield return SendOnce(fullName, email, phone, optionA, optionB);

                if (_lastSendWasSuccess) break;

                if ((_lastCode == 429 || _lastCode == 503) && attempt < maxAttempts)
                {
                    // Exponential backoff with jitter
                    float baseDelay = Mathf.Pow(2f, attempt - 1) * 0.5f; // 0.5,1,2,4,8…
                    float jitter = Random.Range(-0.1f, 0.1f) * baseDelay;
                    float delay = Mathf.Clamp(baseDelay + jitter, 0.25f, 10f);
                    InfoUI($"Rate-limited ({_lastCode}). Retrying in {delay:0.00}s…");
                    yield return new WaitForSeconds(delay);
                }
                else
                {
                    break; // non-retryable or out of attempts
                }
            }

            SetButtonInteractable(true);
            _isSending = false;
        }

        // -------- Single send --------
        private IEnumerator SendOnce(string fullName, string email, string phone, string optionA, string optionB)
        {
            _lastSendWasSuccess = false;
            _lastCode = 0;

            var payload = new Payload
            {
                secret  = sharedSecret,
                fullName = fullName,
                email    = email,
                phone    = phone,
                optionA  = optionA,
                optionB  = optionB
            };

            string json = JsonUtility.ToJson(payload);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            Debug.Log($"[SubmissionSender] Prepared JSON: {json}");

            using (var req = new UnityWebRequest(appsScriptExecUrl, UnityWebRequest.kHttpVerbPOST))
            {
                req.uploadHandler = new UploadHandlerRaw(jsonBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = timeoutSeconds;

                // Prevent POST-after-redirect to GET-only echo URL
                req.redirectLimit = 0;

                Debug.Log("[SubmissionSender] Sending POST…");
                yield return req.SendWebRequest();

                _lastCode = req.responseCode;
                Debug.Log($"[SubmissionSender] Request completed. result={req.result} code={_lastCode}");

                // Success criteria: 2xx or Apps Script's common 302 to echo
                if ((_lastCode >= 200 && _lastCode < 300) || _lastCode == 302)
                {
                    _lastSendWasSuccess = true;
                    SuccessUI("✅ Data submitted successfully!");

                    if (_lastCode == 302)
                    {
                        string location = req.GetResponseHeader("location");
                        Debug.Log($"[SubmissionSender] Redirected to echo URL: {location}");

                        if (fetchEchoAfterRedirect && !string.IsNullOrEmpty(location))
                        {
                            using (var echo = UnityWebRequest.Get(location))
                            {
                                echo.timeout = timeoutSeconds;
                                yield return echo.SendWebRequest();
                                if (echo.result == UnityWebRequest.Result.Success)
                                    Debug.Log($"[SubmissionSender] Echo body:\n{echo.downloadHandler.text}");
                                else
                                    Debug.LogWarning($"[SubmissionSender] Echo GET failed: {echo.responseCode} {echo.error}");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[SubmissionSender] Server body:\n{req.downloadHandler.text}");
                    }
                    yield break;
                }

                // Failure
                string body = req.downloadHandler != null ? req.downloadHandler.text : "";
                Debug.LogError($"❌ Submit failed: {_lastCode} {req.error}\n{body}");
                FailUI($"Submit failed ({_lastCode}).");
            }
        }

        // -------- Helpers: read UI values --------
        private static string ReadInput(TMP_InputField tmp, InputField ugui)
        {
            if (tmp)   return (tmp.text ?? string.Empty).Trim();
            if (ugui)  return (ugui.text ?? string.Empty).Trim();
            return string.Empty;
        }

        private static string ReadDropdown(TMP_Dropdown tmp, Dropdown ugui)
        {
            if (tmp)
            {
                int i = Mathf.Clamp(tmp.value, 0, tmp.options.Count > 0 ? tmp.options.Count - 1 : 0);
                return tmp.options.Count > 0 ? tmp.options[i].text.Trim() : string.Empty;
            }
            if (ugui)
            {
                int i = Mathf.Clamp(ugui.value, 0, ugui.options.Count > 0 ? ugui.options.Count - 1 : 0);
                return ugui.options.Count > 0 ? ugui.options[i].text.Trim() : string.Empty;
            }
            return string.Empty;
        }

        // -------- UI feedback --------
        private void SetButtonInteractable(bool on)
        {
            if (continueButton) continueButton.interactable = on;
        }

        private void InfoUI(string msg)
        {
            if (statusTextUGUI) statusTextUGUI.text = msg;
            if (statusTextTMP)  statusTextTMP.text  = msg;
            Debug.Log($"[SubmissionSender] {msg}");
        }

        private void SuccessUI(string msg)
        {
            if (statusTextUGUI) statusTextUGUI.text = msg;
            if (statusTextTMP)  statusTextTMP.text  = msg;
            Debug.Log(msg);
        }

        private void FailUI(string msg)
        {
            if (statusTextUGUI) statusTextUGUI.text = msg;
            if (statusTextTMP)  statusTextTMP.text  = msg;
            Debug.LogWarning($"[SubmissionSender] {msg}");
        }

        // -------- Payload --------
        [System.Serializable]
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
}