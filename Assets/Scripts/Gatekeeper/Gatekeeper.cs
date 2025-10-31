using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class Gatekeeper : MonoBehaviour
{
    public static Gatekeeper I { get; private set; }

    [Header("References")]
    [SerializeField] private GatekeeperConfig config;
    [SerializeField] private GatekeeperOverlay overlay;

    [Header("Behavior")]
    [Tooltip("Seconds between remote checks & heartbeat posts.")]
    [SerializeField] private float remotePollSeconds = 5f;

    [Header("Logging")]
    [SerializeField] private bool verboseLogs = true;

    private const string PP_IS_LOCKED = "cgplus_app_locked";
    private const string TAG = "[Gatekeeper]";

    private enum AppState { Boot, CheckingLocalLock, LocalLocked, CheckingGeo, GeoBlocked, CheckingRemote, RemoteBlocked, Allowed }
    private AppState _state = AppState.Boot;

    private bool _geoPass;
    private bool _remotePass = true;
    private bool _geoBypassSession = false; // session-only after UNLOCK or server force_allow

    [Serializable] private class GeoResponseIpApiCo { public string country; }

    // NOTE: new fields force_allow & force_until (epoch seconds) supported
    [Serializable] private class RemoteControlDTO { public bool shutdown; public string message; public bool force_allow; public long force_until; }

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (Application.internetReachability != NetworkReachability.NotReachable)
            Application.targetFrameRate = 60;

        SafeHideOverlay();
        Log($"Awake. GEO bypass (session)={_geoBypassSession}");
    }

    private IEnumerator Start()
    {
        // 1) local lock
        _state = AppState.CheckingLocalLock;
        if (IsLocallyLocked())
        {
            _state = AppState.LocalLocked;
            BlockApp(config.blockMessageShutdown);
            StartBackgroundLoops();
            yield break;
        }

        // 2) GEO
        _state = AppState.CheckingGeo;
        yield return CheckGeoAllowed();
        if (!_geoPass && _geoBypassSession) _geoPass = true;
        if (!_geoPass)
        {
            _state = AppState.GeoBlocked;
            BlockApp(config.blockMessageOutside);
            StartBackgroundLoops();
            yield break;
        }

        // 3) Remote (fresh)
        _state = AppState.CheckingRemote;
        yield return CheckRemoteControlOnce(forceNoCache:true); // may flip state via force_allow
        if (!_remotePass)
        {
            _state = AppState.RemoteBlocked;
            BlockApp(config.blockMessageShutdown);
            StartBackgroundLoops();
            yield break;
        }

        // 4) Allowed
        _state = AppState.Allowed;
        Time.timeScale = 1f; AudioListener.pause = false; SafeHideOverlay();
        StartBackgroundLoops();

        Application.focusChanged += OnFocusChanged;
    }

    private void OnDestroy() { Application.focusChanged -= OnFocusChanged; if (I == this) I = null; }
    private void OnFocusChanged(bool f) { if (f) ForceRemoteRefreshNow(); }

    // ================= Loops (remote + heartbeat) =================
    private Coroutine _loopCo;
    private void StartBackgroundLoops()
    {
        if (_loopCo != null) StopCoroutine(_loopCo);
        _loopCo = StartCoroutine(RemoteAndHeartbeatLoop());
    }

    private IEnumerator RemoteAndHeartbeatLoop()
    {
        var wait = new WaitForSeconds(Mathf.Max(1f, remotePollSeconds));
        while (true)
        {
            yield return CheckRemoteControlOnce(forceNoCache:true);

            if (!_remotePass)
            {
                if (_state != AppState.RemoteBlocked)
                {
                    _state = AppState.RemoteBlocked;
                    BlockApp(config.blockMessageShutdown);
                }
            }
            else
            {
                if (_state != AppState.Allowed) UnblockIfEligible();
            }

            yield return SendHeartbeatNow();
            yield return wait;
        }
    }

    // ================= GEO =================
    private IEnumerator CheckGeoAllowed()
    {
        _geoPass = false;
        if (!string.IsNullOrEmpty(config.geoIpUrl))
        {
            using (var req = UnityWebRequest.Get(AddCacheBuster(config.geoIpUrl)))
            {
                req.timeout = 10; yield return req.SendWebRequest();
#if UNITY_2022_3_OR_NEWER
                bool ok = req.result == UnityWebRequest.Result.Success;
#else
                bool ok = !(req.isNetworkError || req.isHttpError);
#endif
                if (ok)
                {
                    try {
                        string code = ExtractCountryCode(req.downloadHandler.text, config.countryCodeJsonKey);
                        if (!string.IsNullOrEmpty(code)) { code = code.Trim().ToUpperInvariant(); _geoPass = IsAllowed(code); }
                    } catch { }
                }
            }
        }
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_geoPass)
        {
            string c = TryGetAndroidCountryISO();
            if (!string.IsNullOrEmpty(c)) _geoPass = IsAllowed(c);
        }
#endif
        if (!_geoPass)
        {
            try { var tz = TimeZoneInfo.Local.Id; if (!string.IsNullOrEmpty(tz) && tz.ToLowerInvariant().Contains("cairo")) _geoPass = true; } catch {}
        }
        yield break;
    }

    private bool IsAllowed(string code)
    {
        if (config.allowedCountryCodes == null) return false;
        foreach (var a in config.allowedCountryCodes) if (string.Equals(a, code, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private string ExtractCountryCode(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return null;
        if (string.Equals(key, "country", StringComparison.OrdinalIgnoreCase))
        {
            try { var dto = JsonUtility.FromJson<GeoResponseIpApiCo>(json); if (!string.IsNullOrEmpty(dto.country)) return dto.country; } catch {}
        }
        string needle=$"\"{key}\""; int i=json.IndexOf(needle,StringComparison.OrdinalIgnoreCase); if(i<0)return null;
        int colon=json.IndexOf(':',i+needle.Length); if(colon<0)return null;
        int start=colon+1; while(start<json.Length && char.IsWhiteSpace(json[start])) start++;
        if (start>=json.Length) return null;
        if(json[start]=='\"'){int end=json.IndexOf('\"',start+1); if(end>start)return json.Substring(start+1,end-(start+1));}
        else {int end=json.IndexOfAny(new[]{',','}',' ','\n','\r'},start); if(end<0)end=json.Length; return json.Substring(start,end-start);}
        return null;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private string TryGetAndroidCountryISO()
    {
        try {
            using (var unityPlayer=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity=unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources=activity.Call<AndroidJavaObject>("getResources"))
            using (var configObj=resources.Call<AndroidJavaObject>("getConfiguration"))
            using (var locales=configObj.Call<AndroidJavaObject>("getLocales"))
            {
                string c=locales.Call<AndroidJavaObject>("get",0).Call<string>("getCountry");
                if(!string.IsNullOrEmpty(c)) return c.ToUpperInvariant();
            }
        } catch {}
        try {
            using (var localeClass=new AndroidJavaClass("java.util.Locale"))
            using (var def=localeClass.CallStatic<AndroidJavaObject>("getDefault"))
            {
                string c=def.Call<string>("getCountry");
                if(!string.IsNullOrEmpty(c)) return c.ToUpperInvariant();
            }
        } catch {}
        return null;
    }
#endif

    // ================= Remote + Heartbeat =================
    private string AddCacheBuster(string baseUrl) => baseUrl + (baseUrl.Contains("?")?"&":"?") + "_cb=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    private IEnumerator CheckRemoteControlOnce(bool forceNoCache=false)
    {
        if (string.IsNullOrEmpty(config.remoteControlUrl)) { _remotePass = true; yield break; }

        string url = forceNoCache ? AddCacheBuster(config.remoteControlUrl) : config.remoteControlUrl;
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10; yield return req.SendWebRequest();
#if UNITY_2022_3_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !(req.isNetworkError || req.isHttpError);
#endif
            if (!ok)
            {
                _remotePass = !config.remoteFailClosed;
                yield break;
            }

            try
            {
                var dto = JsonUtility.FromJson<RemoteControlDTO>(req.downloadHandler.text);
                if (dto == null) { _remotePass = true; yield break; }

                // 1) remote pass/fail
                _remotePass = !dto.shutdown;
                if (dto.shutdown && !string.IsNullOrWhiteSpace(dto.message))
                    config.blockMessageShutdown = dto.message;

                // 2) force_allow from server → clear local lock + enable GEO bypass (session)
                bool allowWindow = dto.force_allow;
                if (allowWindow && dto.force_until > 0)
                {
                    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    allowWindow = now <= dto.force_until;
                }
                if (allowWindow)
                {
                    if (IsLocallyLocked()) { SetLocallyLocked(false); Log("Server force_allow → cleared local lock."); }
                    if (!_geoBypassSession) { _geoBypassSession = true; Log("Server force_allow → GEO bypass (session) enabled."); }
                }
            }
            catch { _remotePass = !config.remoteFailClosed; }
        }
    }

    private string CurrentStatus()
    {
        bool geoAllowed = _geoPass || _geoBypassSession;
        bool logicallyAllowed = geoAllowed && _remotePass && !IsLocallyLocked();
        bool overlayActive = overlay != null && overlay.gameObject.activeInHierarchy;
        return (logicallyAllowed && !overlayActive && _state == AppState.Allowed) ? "live" : "locked";
    }

    private IEnumerator SendHeartbeatNow()
    {
        if (string.IsNullOrEmpty(config.remoteControlUrl)) yield break;
        string baseUrl = config.remoteControlUrl;
        string url = baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "report=1";

        var payload = new {
            device = SystemInfo.deviceUniqueIdentifier,
            platform = Application.platform.ToString(),
            version = Application.version,
            status = CurrentStatus(),
            ts_utc = DateTime.UtcNow.ToString("o")
        };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 10;

        yield return req.SendWebRequest();
        req.Dispose();
    }

    public void ForceRemoteRefreshNow() { StartCoroutine(_ForceRemoteRefreshNow()); }
    private IEnumerator _ForceRemoteRefreshNow()
    {
        yield return CheckRemoteControlOnce(forceNoCache:true);
        if (!_remotePass) BlockApp(config.blockMessageShutdown);
        else if (_state != AppState.Allowed) UnblockIfEligible();
        yield return SendHeartbeatNow();
    }

    // ================= Admin Codes =================
    public bool TrySubmitAdminCode(string input)
    {
        if (string.Equals(input, "REFRESH", StringComparison.OrdinalIgnoreCase))
        {
            ForceRemoteRefreshNow();
            return true;
        }

        string candidateHex = IsHex64(input) ? input.ToLowerInvariant() : Sha256Hex(input);

        if (!string.IsNullOrEmpty(config.lockCodeHashHex) &&
            ConstantTimeEquals(candidateHex, config.lockCodeHashHex.ToLowerInvariant()))
        {
            SetLocallyLocked(true);
            BlockApp(config.blockMessageShutdown);
            StartCoroutine(SendHeartbeatNow());
            return true;
        }

        if (!string.IsNullOrEmpty(config.unlockCodeHashHex) &&
            ConstantTimeEquals(candidateHex, config.unlockCodeHashHex.ToLowerInvariant()))
        {
            SetLocallyLocked(false);
            _geoBypassSession = true;
            // (FIX) Use coroutine, not yield in a non-IEnumerator method
            StartCoroutine(_UnlockWithRemoteRefresh());
            return true;
        }

        return false;
    }

    private IEnumerator _UnlockWithRemoteRefresh()
    {
        yield return CheckRemoteControlOnce(forceNoCache:true);
        if (_remotePass) UnblockConsideringGeoBypass(); else BlockApp(config.blockMessageShutdown);
        yield return SendHeartbeatNow();
    }

    // ================= UI Flow =================
    private void BlockApp(string message)
    {
        Time.timeScale = 0f; AudioListener.pause = true;
        SafeShowOverlay(message, true);
        LogState("BLOCKED");
    }

    private void UnblockConsideringGeoBypass()
    {
        if (!_remotePass) { BlockApp(config.blockMessageShutdown); return; }
        Time.timeScale = 1f; AudioListener.pause = false; SafeHideOverlay(); _state = AppState.Allowed;
        LogState("ALLOWED (via session GEO bypass).");
    }

    private void UnblockIfEligible()
    {
        bool geoAllowed = _geoPass || _geoBypassSession;
        if (!geoAllowed) { BlockApp(config.blockMessageOutside); return; }
        if (!_remotePass) { BlockApp(config.blockMessageShutdown); return; }
        Time.timeScale = 1f; AudioListener.pause = false; SafeHideOverlay(); _state = AppState.Allowed;
        LogState("ALLOWED");
    }

    private void SafeShowOverlay(string message, bool adminMode) { if (overlay != null) overlay.Show(message, adminMode); }
    private void SafeHideOverlay() { if (overlay != null) overlay.Hide(); }

    // ================= Utils =================
    private static string Sha256Hex(string s)
    { using(var sha=SHA256.Create()){ var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(s??"")); var sb=new StringBuilder(bytes.Length*2); foreach(var b in bytes) sb.Append(b.ToString("x2")); return sb.ToString(); } }

    private static bool IsHex64(string s)
    { if(string.IsNullOrEmpty(s)||s.Length!=64)return false; for(int i=0;i<s.Length;i++){char c=s[i]; bool hex=(c>='0'&&c<='9')||(c>='a'&&c<='f')||(c>='A'&&c<='F'); if(!hex)return false;} return true; }

    private static bool ConstantTimeEquals(string a,string b)
    { if(a==null||b==null||a.Length!=b.Length)return false; int diff=0; for(int i=0;i<a.Length;i++) diff|=a[i]^b[i]; return diff==0; }

    private static bool IsLocallyLocked()=> PlayerPrefs.GetInt(PP_IS_LOCKED,0)==1;
    private static void SetLocallyLocked(bool yes){ PlayerPrefs.SetInt(PP_IS_LOCKED, yes?1:0); PlayerPrefs.Save(); }

    private void Log(string m){ if(verboseLogs) Debug.Log($"{TAG} {m}"); }
    private void LogWarn(string m){ if(verboseLogs) Debug.LogWarning($"{TAG} {m}"); }
    private void LogError(string m){ Debug.LogError($"{TAG} {m}"); }
    private void LogState(string m){ if(verboseLogs) Debug.Log($"{TAG} [STATE:{_state}] {m}"); }
}