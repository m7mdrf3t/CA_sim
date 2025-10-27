using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Events;

/// <summary>
/// Color-selection panel that plays its own set of clips
/// into the SAME VideoPlayer managed by GalleryVideoController,
/// via PlayExternalClip(). (Shared player; last click wins.)
/// </summary>
public class ColorSelection : MonoBehaviour
{
    [Header("Shared Player Owner")]
    [Tooltip("Reference your GalleryVideoController that owns the VideoPlayer.")]
    [SerializeField] private GalleryVideoController galleryController;

    [Header("Color clips (same order as your color buttons)")]
    [SerializeField] private VideoClip[] colorClips;

    [Header("Color buttons (same order as clips)")]
    [SerializeField] private Button[] buttons;

    [Header("Optional: auto-find buttons under this parent if array is empty")]
    [SerializeField] private Transform buttonsParent;

    [Header("Events")]
    public UnityEvent<int> OnColorSelectedIndex;   // optional callback with the chosen index

    private bool wired;

    private void Awake()
    {
        // Auto-find buttons if not assigned
        if ((buttons == null || buttons.Length == 0) && buttonsParent != null)
        {
            buttons = buttonsParent.GetComponentsInChildren<Button>(includeInactive: true);
        }

        if (galleryController == null)
        {
            Debug.LogError("[ColorSelection] No GalleryVideoController assigned.");
            enabled = false;
            return;
        }
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

        int count = Mathf.Min(buttons.Length, colorClips != null ? colorClips.Length : 0);
        for (int i = 0; i < count; i++)
        {
            int idx = i; // capture
            buttons[i].onClick.AddListener(() => PlayColorIndex(idx));
        }
        wired = true;

        if (count == 0)
        {
            Debug.LogWarning("[ColorSelection] No buttons/clips set up.");
        }
    }

    private void UnwireButtons()
    {
        if (!wired || buttons == null) return;
        foreach (var b in buttons)
        {
            if (b != null) b.onClick.RemoveAllListeners(); // assumes these are dedicated color buttons
        }
        wired = false;
    }

    /// <summary>
    /// Plays the color clip at index using the shared VideoPlayer (debounced inside GalleryVideoController).
    /// </summary>
    public void PlayColorIndex(int index)
    {
        if (colorClips == null || index < 0 || index >= colorClips.Length)
        {
            Debug.LogWarning($"[ColorSelection] Invalid color index {index}.");
            return;
        }

        var clip = colorClips[index];
        if (clip == null)
        {
            Debug.LogWarning($"[ColorSelection] Color clip at {index} is null.");
            return;
        }

        // Use the shared player via the controller (handles prepare debounce + events)
        galleryController.PlayExternalClip(clip, markAsExternal: true);

        OnColorSelectedIndex?.Invoke(index);
    }

    // Optional helper: play by clip name
    public void PlayColorByName(string clipName)
    {
        if (colorClips == null) return;
        for (int i = 0; i < colorClips.Length; i++)
        {
            if (colorClips[i] != null && colorClips[i].name == clipName)
            {
                PlayColorIndex(i);
                return;
            }
        }
        Debug.LogWarning($"[ColorSelection] Color clip '{clipName}' not found.");
    }
}