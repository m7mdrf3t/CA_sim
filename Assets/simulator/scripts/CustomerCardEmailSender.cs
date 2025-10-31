using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Handles collecting customer card data and sending it via email
/// Attach this to the "15-Customer Card" screen or Next button GameObject
/// </summary>
public class CustomerCardEmailSender : MonoBehaviour
{
    public CardData cardData;

    public UserConfig userConfig;

    [Header("UI References - Assign from Inspector")]
    
    [Header("Email Settings")]
    [Tooltip("Method to use for sending email")]
    [SerializeField] private EmailMethod emailMethod = EmailMethod.BackendService;
    
    [Tooltip("Fallback recipient if client email is not provided")]
    [SerializeField] private string fallbackRecipientEmail = "sales@example.com";
    
    [Tooltip("CC email addresses (optional)")]
    [SerializeField] private string ccEmail = "";
    
    [Tooltip("Subject line for the email")]
    [SerializeField] private string emailSubject = "Customer Card - Crystal Order";
    
    [Header("Backend Email Service (if using BackendService method)")]
    [Tooltip("URL to your email sending service (Google Apps Script or PHP)")]
    [SerializeField] private string backendEmailURL = "https://script.google.com/macros/s/AKfycbwm3EmTRsnZyQId81H4Jd3NlEtwCSpnIHam9NHaUkVMofKkRmrmvZHq0rOvfc59C6wyEQ/exec";
    
    [Tooltip("Secret key for backend authentication")]
    [SerializeField] private string backendSecret = "AsfourCG+";
    
    [Header("UI Feedback")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button sendButton;
    
    public enum EmailMethod
    {
        MailTo,         // Opens user's default email client
        BackendService  // Sends via Google Apps Script or PHP backend
    }
    
    private void Start() {
        if (cardData == null)
        {
            Debug.LogWarning("[CustomerCardEmailSender] CardData reference is missing!");
        }


    }
    /// <summary>
    /// Call this method when Next button is clicked
    /// </summary>
    public void SendCustomerCardEmail()
    {
        // Collect all data
        CustomerCardData cardData = CollectCardData();
        
        // Validate required fields
        if (string.IsNullOrEmpty(cardData.clientName))
        {
            ShowStatus("⚠️ Client name is required!", true);
            return;
        }
        
        // Validate email
        if (string.IsNullOrEmpty(cardData.clientEmail) || !IsValidEmail(cardData.clientEmail))
        {
            ShowStatus("⚠️ Valid client email is required!", true);
            Debug.LogWarning($"[CustomerCardEmailSender] Invalid email: '{cardData.clientEmail}'");
            return;
        }
        
        Debug.Log($"[CustomerCardEmailSender] Sending email to: {cardData.clientEmail}");
        
        // Send based on selected method
        switch (emailMethod)
        {
            case EmailMethod.MailTo:
                SendViaMailTo(cardData);
                break;
                
            case EmailMethod.BackendService:
                StartCoroutine(SendViaBackend(cardData));
                break;
        }
    }
    
    /// <summary>
    /// Collect all data from UI fields
    /// </summary>
    private CustomerCardData CollectCardData()
    {
        CustomerCardData data = new CustomerCardData();
        
        // Get data from UI fields
        data.clientName = cardData.ItemName;
        data.clientEmail = userConfig.clientEmail;
        
        data.selectedCrystals = cardData.SelectedCrystals != null ? string.Join(", ", cardData.SelectedCrystals) : "";
        data.numberOfEachCrystal = cardData.NumberOfCrystals.ToString();
        data.colorOfEachCrystal = cardData.ColorsOfCrystals != null ? string.Join(", ", cardData.ColorsOfCrystals) : "";
        data.compositionStyle = cardData.CompStyle;
        data.numberOfWires = cardData.NumberOfWires.ToString();
        data.baseShapeColor = cardData.BaseShape;
        data.finalPrice = 17000.ToString("C"); // Example fixed price
        data.salesmanID = cardData.SalesmanID;
        
        // Add timestamp
        data.timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        Debug.Log($"[CustomerCardEmailSender] Collected data: {data.clientName} ({data.clientEmail})");
        return data;
    }
    
    /// <summary>
    /// Send email using mailto: (opens user's email client)
    /// </summary>
    private void SendViaMailTo(CustomerCardData data)
    {
        // Use client's email or fallback
        string recipient = !string.IsNullOrEmpty(data.clientEmail) ? data.clientEmail : fallbackRecipientEmail;
        
        string emailBody = FormatEmailBody(data);
        
        // URL encode the body
        string encodedBody = UnityWebRequest.EscapeURL(emailBody);
        string encodedSubject = UnityWebRequest.EscapeURL(emailSubject);
        
        // Build mailto URL
        StringBuilder mailtoURL = new StringBuilder();
        mailtoURL.Append("mailto:");
        mailtoURL.Append(recipient);
        mailtoURL.Append("?subject=");
        mailtoURL.Append(encodedSubject);
        mailtoURL.Append("&body=");
        mailtoURL.Append(encodedBody);
        
        // Add CC if specified
        if (!string.IsNullOrEmpty(ccEmail))
        {
            mailtoURL.Append("&cc=");
            mailtoURL.Append(ccEmail);
        }
        
        Debug.Log($"[CustomerCardEmailSender] Opening email client for: {recipient}");
        Application.OpenURL(mailtoURL.ToString());
        
        ShowStatus("✅ Email client opened! Please send the email.");
    }
    
    /// <summary>
    /// Send email via backend service (Google Apps Script or PHP)
    /// </summary>
    private IEnumerator SendViaBackend(CustomerCardData data)
    {
        if (string.IsNullOrEmpty(backendEmailURL))
        {
            ShowStatus("❌ Backend email URL not configured!", true);
            yield break;
        }
        
        // Use client's email or fallback
        string recipient = !string.IsNullOrEmpty(data.clientEmail) ? data.clientEmail : fallbackRecipientEmail;
        
        if (string.IsNullOrEmpty(recipient))
        {
            ShowStatus("❌ No recipient email available!", true);
            yield break;
        }
        
        SetButtonInteractable(false);
        ShowStatus($"📧 Sending email to {recipient}...");
        
        // Prepare JSON payload
        EmailPayload payload = new EmailPayload
        {
            secret = backendSecret,
            recipient = recipient, // ✅ Use client's email
            cc = ccEmail,
            subject = emailSubject,
            body = FormatEmailBody(data),
            clientName = data.clientName,
            clientEmail = data.clientEmail, // Include in payload for logging
            salesmanID = data.salesmanID,
            timestamp = data.timestamp
        };
        
        string json = JsonUtility.ToJson(payload);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
        
        Debug.Log($"[CustomerCardEmailSender] Sending to backend for recipient: {recipient}");
        Debug.Log($"[CustomerCardEmailSender] Payload: {json}");
        
        using (var req = new UnityWebRequest(backendEmailURL, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 15;
            req.redirectLimit = 0;
            
            yield return req.SendWebRequest();
            
            if (req.result == UnityWebRequest.Result.Success || req.responseCode == 302)
            {
                ShowStatus($"✅ Email sent to {recipient}!");
                Debug.Log($"[CustomerCardEmailSender] Email sent successfully to {recipient}");
            }
            else
            {
                ShowStatus($"❌ Failed to send email: {req.error}", true);
                Debug.LogError($"[CustomerCardEmailSender] Failed: {req.responseCode} {req.error}");
            }
        }
        
        SetButtonInteractable(true);
    }
    
    /// <summary>
    /// Format the email body with all customer card data
    /// </summary>
    private string FormatEmailBody(CustomerCardData data)
    {
        StringBuilder body = new StringBuilder();
        
        body.AppendLine("CUSTOMER CARD - CRYSTAL ORDER");
        body.AppendLine("=====================================");
        body.AppendLine();
        body.AppendLine($"Date/Time: {data.timestamp}");
        body.AppendLine();
        body.AppendLine($"Client Name: {data.clientName}");
        body.AppendLine($"Client Email: {data.clientEmail}"); // ✅ Include email in body
        body.AppendLine($"Salesman ID: {data.salesmanID}");
        body.AppendLine();
        body.AppendLine("--- ORDER DETAILS ---");
        body.AppendLine($"Selected Crystals: {data.selectedCrystals}");
        body.AppendLine($"Number of Each Crystal: {data.numberOfEachCrystal}");
        body.AppendLine($"Color of Each Crystal: {data.colorOfEachCrystal}");
        body.AppendLine($"Composition Style: {data.compositionStyle}");
        body.AppendLine($"Number of Wires: {data.numberOfWires}");
        body.AppendLine($"Base Shape/Color: {data.baseShapeColor}");
        body.AppendLine();
        body.AppendLine($"FINAL PRICE: {data.finalPrice}");
        body.AppendLine();
        body.AppendLine("=====================================");
        body.AppendLine("Sent from Crystal Studio App");
        
        return body.ToString();
    }
    
    // Helper methods
    private string GetText(TMP_Text textComponent)
    {
        return textComponent != null ? textComponent.text : "";
    }
    
    private string GetInputText(TMP_InputField inputField)
    {
        return inputField != null ? inputField.text : "";
    }
    
    private void ShowStatus(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            // Optional: Change color based on error
            if (isError)
            {
                statusText.color = new Color(1f, 0.3f, 0.3f); // Red for errors
            }
            else
            {
                statusText.color = Color.white;
            }
        }
        
        if (isError)
        {
            Debug.LogError($"[CustomerCardEmailSender] {message}");
        }
        else
        {
            Debug.Log($"[CustomerCardEmailSender] {message}");
        }
    }
    
    /// <summary>
    /// Simple email validation
    /// </summary>
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        
        // Basic validation: must contain @ and .
        return email.Contains("@") && email.Contains(".") && email.IndexOf("@") < email.LastIndexOf(".");
    }
    
    private void SetButtonInteractable(bool interactable)
    {
        if (sendButton != null)
        {
            sendButton.interactable = interactable;
        }
    }
    
    /// <summary>
    /// Data structure for customer card
    /// </summary>
    [System.Serializable]
    private class CustomerCardData
    {
        public string clientName;
        public string clientEmail; // ✅ NEW: Client's email address
        public string selectedCrystals;
        public string numberOfEachCrystal;
        public string colorOfEachCrystal;
        public string compositionStyle;
        public string numberOfWires;
        public string baseShapeColor;
        public string finalPrice;
        public string salesmanID;
        public string timestamp;
    }
    
    /// <summary>
    /// Payload for backend email service
    /// </summary>
    [System.Serializable]
    private class EmailPayload
    {
        public string secret;
        public string recipient;
        public string cc;
        public string subject;
        public string body;
        public string clientName;
        public string clientEmail; // ✅ NEW: For backend logging
        public string salesmanID;
        public string timestamp;
    }
}

