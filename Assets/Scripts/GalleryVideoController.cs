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
    [SerializeField] private Transform buttonsParent; // e.g., your "Gallery" panel

    public int CurrentIndex { get; private set; } = -1;

    /// <summary> Fired whenever a clip is finally started (after prepare completes). </summary>
    public event Action<int> OnClipChanged;

    // Debounce state
    private bool isPreparing;

    // Listener bookkeeping to avoid duplicates
    private bool wired;
    private readonly List<UnityAction> cachedButtonDelegates = new List<UnityAction>();

    // ===== Read-only accessors to avoid reflection =====
    public IReadOnlyList<VideoClip> Clips => clips;
    public string GetClipName(int index) =>
        (index >= 0 && index < clips.Length && clips[index] != null) ? clips[index].name : null;

    // Robust thumbnail pull from a Button
    public Sprite GetButtonSprite(int index)
    {
        if (buttons == null || index < 0 || index >= buttons.Length) return null;

        // 1) Try the Button's targetGraphic
        var img = buttons[index].targetGraphic as Image;
        if (img != null && img.sprite != null) return img.sprite;

        // 2) Try an Image on the same GameObject
        img = buttons[index].GetComponent<Image>();
        if (img != null && img.sprite != null) return img.sprite;

        // 3) Try any child Image (common in styled prefabs)
        img = buttons[index].GetComponentInChildren<Image>(true);
        if (img != null && img.sprite != null) return img.sprite;

        return null;
    }

    private void Awake()
    {
        // Auto-find buttons
        if ((buttons == null || buttons.Length == 0) && buttonsParent != null)
        {
            buttons = buttonsParent.GetComponentsInChildren<Button>(includeInactive: true);
        }

        // Safety
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

    private void OnEnable()
    {
        WireButtons();
    }

    private void OnDisable()
    {
        UnwireButtons();
    }

    private void WireButtons()
    {
        if (wired || buttons == null) return;

        int count = Mathf.Min(buttons.Length, clips != null ? clips.Length : 0);
        cachedButtonDelegates.Clear();
        for (int i = 0; i < count; i++)
        {
            int idx = i; // capture
            UnityAction a = () => PlayIndex(idx);
            cachedButtonDelegates.Add(a);
            buttons[i].onClick.AddListener(a);
        }
        wired = true;

        if (buttons != null && (clips == null || clips.Length == 0))
            Debug.LogWarning("[GalleryVideoController] Buttons set but no clips assigned.");

        if (clips != null && clips.Length > 0 && (buttons == null || buttons.Length == 0))
            Debug.LogWarning("[GalleryVideoController] Clips set but no buttons assigned/found.");
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

    /// <summary>
    /// Play a clip from our gallery list by index (called by gallery buttons).
    /// Debounced to avoid flicker when spamming clicks.
    /// </summary>
    public void PlayIndex(int index)
    {
        if (clips == null || index < 0 || index >= clips.Length)
        {
            Debug.LogWarning($"[GalleryVideoController] Invalid clip index {index}.");
            return;
        }

        VideoClip next = clips[index];

        // If same clip and already preparing/playing, ignore
        if ((isPreparing && videoPlayer.clip == next) ||
            (videoPlayer.clip == next && videoPlayer.isPlaying))
            return;

        CurrentIndex = index;
        StartPrepare(next);
    }

    /// <summary>
    /// Allow any external system (e.g., ColorSelection) to play a different clip on the same VideoPlayer.
    /// Sets CurrentIndex = -1 to indicate "not a gallery clip".
    /// </summary>
    public void PlayExternalClip(VideoClip clip, bool markAsExternal = true)
    {
        if (clip == null)
        {
            Debug.LogWarning("[GalleryVideoController] External clip is null.");
            return;
        }

        if (markAsExternal) CurrentIndex = -1;

        if ((isPreparing && videoPlayer.clip == clip) ||
            (videoPlayer.clip == clip && videoPlayer.isPlaying))
            return;

        StartPrepare(clip);
    }

    private void StartPrepare(VideoClip clip)
    {
        isPreparing = true;
        videoPlayer.Stop();
        videoPlayer.clip = clip;

        videoPlayer.prepareCompleted -= OnPrepared;
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

    // Optional: play by clip name in the gallery
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