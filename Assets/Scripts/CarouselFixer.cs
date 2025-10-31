using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick fix for carousel centering issues
/// </summary>
public class CarouselFixer : MonoBehaviour
{
    [Header("Carousel Setup")]
    [SerializeField] private UICarouselSnap carousel;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform[] items;
    
    [Header("Settings")]
    [SerializeField] private float itemWidth = 523f;
    [SerializeField] private float spacing = 40f;
    [SerializeField] private int centerIndex = 2;
    
    [ContextMenu("Fix Carousel Layout")]
    public void FixCarouselLayout()
    {
        if (carousel == null)
        {
            carousel = GetComponent<UICarouselSnap>();
        }
        
        if (content == null)
        {
            content = transform.Find("Viewport/Content")?.GetComponent<RectTransform>();
        }
        
        if (items == null || items.Length == 0)
        {
            // Auto-find items
            if (content != null)
            {
                items = content.GetComponentsInChildren<RectTransform>();
                // Filter out the content itself
                var itemList = new System.Collections.Generic.List<RectTransform>();
                foreach (var item in items)
                {
                    if (item != content && item.name.StartsWith("Iteam_"))
                    {
                        itemList.Add(item);
                    }
                }
                items = itemList.ToArray();
            }
        }
        
        if (items == null || items.Length == 0)
        {
            Debug.LogError("No carousel items found!");
            return;
        }
        
        // Set up content
        content.anchorMin = new Vector2(0f, 0.5f);
        content.anchorMax = new Vector2(1f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        
        // Calculate total width and set content size
        float totalWidth = (itemWidth + spacing) * (items.Length - 1) + itemWidth;
        content.sizeDelta = new Vector2(totalWidth, content.sizeDelta.y);
        
        // Position items
        float startX = -totalWidth * 0.5f + itemWidth * 0.5f;
        
        for (int i = 0; i < items.Length; i++)
        {
            var item = items[i];
            item.anchorMin = item.anchorMax = new Vector2(0.5f, 0.5f);
            item.pivot = new Vector2(0.5f, 0.5f);
            item.anchoredPosition = new Vector2(startX + i * (itemWidth + spacing), 0f);
            item.localScale = Vector3.one;
        }
        
        // Center on the specified index
        float centerX = content.rect.width * 0.5f;
        float itemX = items[centerIndex].anchoredPosition.x;
        float delta = itemX - centerX;
        content.anchoredPosition = new Vector2(-delta, 0f);
        
        Debug.Log($"Fixed carousel layout! Items: {items.Length}, Center Index: {centerIndex}");
    }
    
    private void Start()
    {
        // Auto-fix on start
        FixCarouselLayout();
    }
}
