using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UICarouselSnap : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Wiring")]
    [SerializeField] private RectTransform viewport;   // Image + RectMask2D
    [SerializeField] private RectTransform content;    // parent of items
    [SerializeField] private Button btnPrev;           // optional
    [SerializeField] private Button btnNext;           // optional

    [Header("Items & Layout")]
    [SerializeField] private RectTransform[] items;    // leave empty to auto-grab content children
    [SerializeField] private float itemWidth = 480f;
    [SerializeField] private float spacing = 40f;
    [SerializeField] private int startIndex = 2;

    [Header("Scaling & Fading")]
    [SerializeField] private float centerScale = 1.0f;
    [SerializeField] private float sideScale = 0.75f;
    [SerializeField] private float scaleFalloff = 0.0025f;
    [SerializeField] private bool dimSides = true;
    [SerializeField] private float minAlpha = 0.5f;

    [Header("Snap")]
    [SerializeField] private float snapSpeed = 12f;
    [SerializeField] private float dragDampen = 0.9f;

    public Action<int> OnCenteredIndexChanged;   // fires when center changes
    public Action<int> OnCenterItemClicked;      // fires when user clicks centered card
    public int CenterIndex => _centerIndex;

    // runtime
    private Vector2 _contentStartPos;
    private Vector2 _velocity;
    private bool _dragging;
    private int _centerIndex = -1;
    private float _targetX;

    private void Awake()
    {
        if (items == null || items.Length == 0)
            items = content.GetComponentsInChildren<RectTransform>(includeInactive: false);
        items = Array.FindAll(items, it => it != content);

        LayoutItems();

        if (btnPrev) btnPrev.onClick.AddListener(() => JumpTo(Mathf.Clamp(GetNearestIndex() - 1, 0, items.Length - 1)));
        if (btnNext) btnNext.onClick.AddListener(() => JumpTo(Mathf.Clamp(GetNearestIndex() + 1, 0, items.Length - 1)));

        // Use existing Buttons; if missing, add one + raycastable Graphic
        for (int i = 0; i < items.Length; i++)
        {
            var rt = items[i];

            var btn = rt.GetComponent<Button>();
            if (btn == null)
            {
                var g = rt.GetComponent<Graphic>();
                if (g == null)
                {
                    var img = rt.gameObject.AddComponent<Image>();
                    img.color = new Color(1, 1, 1, 0); // invisible but raycastable
                }
                btn = rt.gameObject.AddComponent<Button>();
            }

            btn.onClick.RemoveAllListeners();
            int idx = i;
            btn.onClick.AddListener(() =>
            {
                if (idx != _centerIndex) JumpTo(idx);    // side tap → center it
                else OnCenterItemClicked?.Invoke(idx);    // center tap → select
            });
        }

        // Start centered
        JumpTo(Mathf.Clamp(startIndex, 0, items.Length - 1), immediate: true);
        UpdateItemVisuals();
        UpdateRaycastTargets(GetNearestIndex());
    }

    private void Update()
    {
        if (!_dragging)
        {
            Vector2 pos = content.anchoredPosition;
            pos.x = Mathf.Lerp(pos.x, _targetX, 1f - Mathf.Exp(-snapSpeed * Time.unscaledDeltaTime));
            content.anchoredPosition = pos;
            _velocity *= dragDampen;
        }
        else
        {
            content.anchoredPosition += _velocity * Time.unscaledDeltaTime;
        }

        UpdateItemVisuals();

        int nearest = GetNearestIndex();
        if (nearest != _centerIndex)
        {
            _centerIndex = nearest;
            OnCenteredIndexChanged?.Invoke(_centerIndex);
            UpdateRaycastTargets(_centerIndex);
        }
    }

    private void LayoutItems()
    {
        float step = itemWidth + spacing;
        float total = step * (items.Length - 1);
        float start = -total * 0.5f;

        // Set up content RectTransform properly
        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(1f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        
        // Set content size to accommodate all items
        content.sizeDelta = new Vector2(total + itemWidth, content.sizeDelta.y);

        for (int i = 0; i < items.Length; i++)
        {
            var rt = items[i];
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(start + i * step, 0f);
        }
    }

    private float ViewportCenterXLocal()
    {
        // Fixed center calculation - viewport center should be at 0 in content's local space
        // when content is properly positioned
        return 0f;
    }

    private void UpdateItemVisuals()
    {
        float centerX = 0f; // Center is always at 0 in content's local space

        for (int i = 0; i < items.Length; i++)
        {
            var rt = items[i];
            // Calculate the item's position relative to content's current position
            float itemWorldX = rt.anchoredPosition.x + content.anchoredPosition.x;
            float dx = Mathf.Abs(itemWorldX - centerX);

            float t = Mathf.Exp(-dx * scaleFalloff);
            float scale = Mathf.Lerp(sideScale, centerScale, t);
            rt.localScale = new Vector3(scale, scale, 1f);

            if (dimSides)
            {
                foreach (var g in rt.GetComponentsInChildren<Graphic>(true))
                {
                    var c = g.color;
                    c.a = Mathf.Lerp(minAlpha, 1f, t);
                    g.color = c;
                }
            }
        }
    }

    private int GetNearestIndex()
    {
        float centerX = 0f; // Center is always at 0 in content's local space
        int nearest = 0;
        float best = float.MaxValue;
        
        for (int i = 0; i < items.Length; i++)
        {
            // Calculate the item's position relative to content's current position
            float itemWorldX = items[i].anchoredPosition.x + content.anchoredPosition.x;
            float d = Mathf.Abs(itemWorldX - centerX);
            if (d < best) { best = d; nearest = i; }
        }
        return nearest;
    }

    private void SnapToIndex(int index)
    {
        if (index < 0 || index >= items.Length) return;
        
        // Calculate the target position to center the selected item
        float itemX = items[index].anchoredPosition.x;
        _targetX = -itemX; // Negative because we want to move content to center the item
        
        // Debug logging to help troubleshoot
        Debug.Log($"[Carousel] Snapping to index {index}: itemX={itemX:F2}, targetX={_targetX:F2}");
    }

    public void JumpTo(int index, bool immediate = false)
    {
        index = Mathf.Clamp(index, 0, items.Length - 1);
        SnapToIndex(index);
        if (immediate)
        {
            var p = content.anchoredPosition;
            p.x = _targetX;
            content.anchoredPosition = p;
            _velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Force refresh the carousel layout - useful for debugging
    /// </summary>
    [ContextMenu("Refresh Layout")]
    public void RefreshLayout()
    {
        LayoutItems();
        JumpTo(_centerIndex >= 0 ? _centerIndex : startIndex, immediate: true);
        UpdateItemVisuals();
    }

    private void UpdateRaycastTargets(int centerIndex)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var rt = items[i];
            var btn = rt.GetComponent<Button>();
            if (btn) btn.interactable = (i == centerIndex);

            foreach (var g in rt.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = (i == centerIndex);
        }
    }

    // Drag handling
    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = true;
        _velocity = Vector2.zero;
        _contentStartPos = content.anchoredPosition;
    }
    public void OnDrag(PointerEventData eventData)
    {
        content.anchoredPosition += new Vector2(eventData.delta.x, 0f);
        _velocity = new Vector2(eventData.delta.x / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0f);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        float predictedX = content.anchoredPosition.x + _velocity.x * 0.1f;
        content.anchoredPosition = new Vector2(predictedX, content.anchoredPosition.y);
        int nearest = GetNearestIndex();
        SnapToIndex(nearest);
    }
}