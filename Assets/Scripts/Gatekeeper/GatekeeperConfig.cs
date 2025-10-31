using UnityEngine;

[CreateAssetMenu(fileName = "GatekeeperConfig", menuName = "CGPlus/Gatekeeper Config")]
public class GatekeeperConfig : ScriptableObject
{
    [Header("Country Gating")]
    [Tooltip("Two-letter ISO codes allowed to run (e.g., EG for Egypt).")]
    public string[] allowedCountryCodes = new[] { "EG" };

    [Tooltip("HTTPS Geo-IP endpoint that returns a JSON with a country code. Default: ipapi.co")]
    public string geoIpUrl = "https://ipapi.co/json/"; // returns { "country": "EG", ... }

    [Tooltip("JSON key that holds the two-letter country code for your provider.")]
    public string countryCodeJsonKey = "country"; // ipapi: 'country'; ipinfo: 'country'; ipwho.is: 'country_code'

    [Header("Remote Control (Kill Switch)")]
    [Tooltip("HTTPS endpoint returning { \"shutdown\":bool, \"message\":string }. Leave empty to disable.")]
    public string remoteControlUrl = ""; // e.g., "https://your.domain.com/app-control"
    
    [Tooltip("Poll the remote control URL every N seconds. 0 disables polling.")]
    public float remotePollSeconds = 300f;

    [Tooltip("If true, failing to reach the remote endpoint will BLOCK the app (fail-closed). If false, it allows (fail-open).")]
    public bool remoteFailClosed = false;

    [Header("Admin Codes (SHA-256 hex)")]
    [Tooltip("SHA-256 hex of the LOCK code. Entering this disables the app locally until unlocked.")]
    public string lockCodeHashHex = "";   // e.g., SHA256("LOCK-1234")
    [Tooltip("SHA-256 hex of the UNLOCK code. Entering this re-enables the app locally.")]
    public string unlockCodeHashHex = ""; // e.g., SHA256("OPEN-5678")

    [Header("Messages")]
    public string blockMessageOutside = "Please contact CG+ Tech.Team";
    public string blockMessageShutdown = "Application is temporarily disabled. Please contact CG+ Tech.Team.";
}