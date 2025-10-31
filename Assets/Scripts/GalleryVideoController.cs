using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;

public class GalleryVideoController : MonoBehaviour
{
    [Header("Target Player (feeds the circle/RenderTexture)")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Clips in the same order as your gallery")]
    [SerializeField] private VideoClip[] clips;

    [Header("Buttons that trigger each clip (same order as clips)")]
    [SerializeField] private Button[] buttons;

    [Header("Optional: auto-find buttons under this parent if array is empty")]
    [SerializeField] private Transform buttonsParent; // e.g., "Gallery" panel

    public int CurrentIndex { get; private set; } = -1;
    public event Action<int> OnClipChanged;

    private bool isPreparing;
    private bool wired;
    private readonly List<UnityAction> cachedButtonDelegates = new List<UnityAction>();
    private int lastGalleryIndex = -1;

    public IReadOnlyList<VideoClip> Clips => clips;
    public string GetClipName(int index) =>
        (index >= 0 && index < clips.Length && clips[index] != null) ? clips[index].name : null;

    public Sprite GetButtonSprite(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return null;

        // Try targetGraphic first
        var img = buttons[index].targetGraphic as Image;
        if (img != null && img.sprite != null) return img.sprite;

        // Then same GameObject
        img = buttons[index].GetComponent<Image>();
        if (img != null && img.sprite != null) return img.sprite;

        // Then any child Image
        img = buttons[index].GetComponentInChildren<Image>(true);
        if (img != null && img.sprite != null) return img.sprite;

        return null;
    }

    private void Awake()
    {
        // Auto-find buttons if needed
        if ((buttons == null || buttons.Length == 0) && buttonsParent != null)
            buttons = buttonsParent.GetComponentsInChildren<Button>(includeInactive: true);

        if (videoPlayer == null)
        {
            Debug.LogError("[GalleryVideoController] No VideoPlayer assigned.");
            enabled = false;
            return;
        }

        // Sane defaults
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = true;
        videoPlayer.skipOnDrop = true;
    }

    private void Start()
    {
        if (clips != null && clips.Length > 0)
        {
            if (videoPlayer.clip == null)
            {
                PlayIndex(0);
                lastGalleryIndex = 0; // ensure fallback index is valid
            }
            else
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] == videoPlayer.clip)
                    {
                        CurrentIndex = i;
                        lastGalleryIndex = i;
                        break;
                    }
                }
            }
        }

        // Ensure buttons reflect current clip count on start
        RefreshButtonStates();
    }

    private void OnEnable() => WireButtons();

    private void OnDisable() => UnwireButtons();

    private void OnDestroy()
    {
        // Safety: make sure we’re not still subscribed
        if (videoPlayer != null)
            videoPlayer.prepareCompleted -= OnPrepared;
    }

    private void WireButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            if (buttonsParent != null)
                buttons = buttonsParent.GetComponentsInChildren<Button>(includeInactive: true);
        }
        if (buttons == null) return;

        // Clear previous listeners
        foreach (var btn in buttons) if (btn) btn.onClick.RemoveAllListeners();

        int count = Mathf.Min(buttons.Length, clips != null ? clips.Length : 0);
        cachedButtonDelegates.Clear();

        for (int i = 0; i < count; i++)
        {
            int idx = i;
            UnityAction a = () => PlayIndex(idx);
            cachedButtonDelegates.Add(a);
            buttons[i].onClick.AddListener(a);
        }

        // Disable any extra buttons beyond clip count
        for (int i = count; i < (buttons?.Length ?? 0); i++)
        {
            if (buttons[i] != null) buttons[i].interactable = false;
        }

        wired = true;
    }

    private void UnwireButtons()
    {
        if (!wired || buttons == null) return;

        int count = Mathf.Min(buttons.Length, cachedButtonDelegates.Count);
        for (int i = 0; i < count; i++)
        {
            if (buttons[i] != null && cachedButtonDelegates[i] != null)
                buttons[i].onClick.RemoveListener(cachedButtonDelegates[i]);
        }
        cachedButtonDelegates.Clear();
        wired = false;
    }

    public void PlayIndex(int index)
    {
        if (clips == null || index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[GalleryVideoController] Invalid clip index {index}.");
            return;
        }

        var next = clips[index];
        if ((isPreparing && videoPlayer.clip == next) || (videoPlayer.clip == next && videoPlayer.isPlaying))
            return;

        CurrentIndex = index;
        lastGalleryIndex = index;
        StartPrepare(next);
    }

    public void PlayExternalClip(VideoClip clip, bool markAsExternal = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("[GalleryVideoController] External clip is null.");
            return;
        }

        if (markAsExternal) CurrentIndex = -1;

        if ((isPreparing && videoPlayer.clip == clip) || (videoPlayer.clip == clip && videoPlayer.isPlaying))
            return;

        StartPrepare(clip);
    }

    private void StartPrepare(VideoClip clip)
    {
        isPreparing = true;
        videoPlayer.Stop();
        videoPlayer.clip = clip;

        videoPlayer.prepareCompleted -= OnPrepared; // avoid dupes
        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.Prepare();
    }

    private void OnPrepared(VideoPlayer vp)
    {
        vp.prepareCompleted -= OnPrepared;
        isPreparing = false;
        vp.Play();
        OnClipChanged?.Invoke(CurrentIndex);
    }

    public int ResolveCurrentIndex()
    {
        if (CurrentIndex >= 0) return CurrentIndex;
        if (lastGalleryIndex >= 0) return lastGalleryIndex;

        if (videoPlayer != null && videoPlayer.clip != null && clips != null)
        {
            for (int i = 0; i < clips.Length; i++)
                if (clips[i] == videoPlayer.clip) return i;
        }
        return -1;
    }

    /// <summary>
    /// Swap the gallery clips at runtime and optionally refresh button sprites.
    /// Also refreshes button interactivity based on new clip count.
    /// </summary>
    public void SetClips(VideoClip[] newClips, Sprite[] newButtonSprites = null)
    {
        clips = newClips ?? Array.Empty<VideoClip>();

        // Update thumbnails if provided
        if (newButtonSprites != null && buttons != null)
        {
            int count = Mathf.Min(buttons.Length, newButtonSprites.Length);
            for (int i = 0; i < count; i++)
            {
                var img = buttons[i].GetComponentInChildren<Image>(true);
                if (img) img.sprite = newButtonSprites[i];
            }
        }

        // Refresh availability of buttons vs clips
        RefreshButtonStates();

        // Reset to first clip if available
        if (clips.Length > 0) PlayIndex(0);
        else
        {
            CurrentIndex = -1;
            lastGalleryIndex = -1;
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }
    }

    /// <summary>
    /// Enable/disable buttons based on clip count and keep existing listeners.
    /// Call this after changing clips or wiring buttons.
    /// </summary>
    private void RefreshButtonStates()
    {
        if (buttons == null) return;

        int activeCount = Mathf.Min(buttons.Length, clips != null ? clips.Length : 0);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].interactable = i < activeCount;
        }
    }

    public void PlayByName(string clipName)
    {
        if (clips == null) return;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == clipName)
            {
                PlayIndex(i);
                return;
            }
        }
        Debug.LogWarning($"[GalleryVideoController] Clip '{clipName}' not found.");
    }
}