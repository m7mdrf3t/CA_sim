using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug script to help identify carousel selection issues
/// </summary>
public class CarouselDebugger : MonoBehaviour
{
    [Header("Debug Target")]
    [SerializeField] private CarouselCategoryRouter carouselRouter;
    [SerializeField] private CompositionLevel2Initializer level2Initializer;
    
    [Header("Debug Info")]
    [SerializeField] private Text debugText; // Optional UI text to show debug info
    
    private void Start()
    {
        if (carouselRouter == null)
            carouselRouter = FindObjectOfType<CarouselCategoryRouter>();
            
        if (level2Initializer == null)
            level2Initializer = FindObjectOfType<CompositionLevel2Initializer>();
    }
    
    private void Update()
    {
        UpdateDebugInfo();
    }
    
    private void UpdateDebugInfo()
    {
        string debugInfo = "=== CAROUSEL DEBUG INFO ===\n";
        
        // Check SelectionBus data
        debugInfo += $"Selected Category Index: {SelectionBus.SelectedCategoryIndex}\n";
        debugInfo += $"Selected Category Title: {SelectionBus.SelectedCategoryTitle}\n";
        debugInfo += $"Selected Category Sprite: {(SelectionBus.SelectedCategorySprite != null ? SelectionBus.SelectedCategorySprite.name : "NULL")}\n";
        
        // Check CarouselCategoryRouter configuration
        if (carouselRouter != null)
        {
            debugInfo += "\n=== CAROUSEL ROUTER ===\n";
            
            // Use reflection to check private fields
            var titleField = typeof(CarouselCategoryRouter).GetField("categoryTitles", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spriteField = typeof(CarouselCategoryRouter).GetField("categorySprites", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (titleField != null)
            {
                string[] titles = titleField.GetValue(carouselRouter) as string[];
                debugInfo += $"Category Titles: {(titles != null ? string.Join(", ", titles) : "NULL")}\n";
            }
            
            if (spriteField != null)
            {
                Sprite[] sprites = spriteField.GetValue(carouselRouter) as Sprite[];
                debugInfo += $"Category Sprites: {(sprites != null ? sprites.Length.ToString() : "NULL")} sprites\n";
                if (sprites != null)
                {
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        debugInfo += $"  [{i}]: {(sprites[i] != null ? sprites[i].name : "NULL")}\n";
                    }
                }
            }
        }
        
        // Check Level2Initializer configuration
        if (level2Initializer != null)
        {
            debugInfo += "\n=== LEVEL 2 INITIALIZER ===\n";
            debugInfo += $"Level2Initializer found: YES\n";
            
            // Check if hero preview is assigned
            var heroField = typeof(CompositionLevel2Initializer).GetField("heroPreview", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (heroField != null)
            {
                Image heroImage = heroField.GetValue(level2Initializer) as Image;
                debugInfo += $"Hero Preview Image: {(heroImage != null ? "ASSIGNED" : "NOT ASSIGNED")}\n";
                if (heroImage != null)
                {
                    debugInfo += $"Hero Image Sprite: {(heroImage.sprite != null ? heroImage.sprite.name : "NULL")}\n";
                }
            }
        }
        
        if (debugText != null)
        {
            debugText.text = debugInfo;
        }
        
        // Log to console every few seconds
        if (Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
        {
            Debug.Log(debugInfo);
        }
    }
    
    [ContextMenu("Test Selection Bus")]
    public void TestSelectionBus()
    {
        Debug.Log("=== SELECTION BUS TEST ===");
        Debug.Log($"Index: {SelectionBus.SelectedCategoryIndex}");
        Debug.Log($"Title: {SelectionBus.SelectedCategoryTitle}");
        Debug.Log($"Sprite: {(SelectionBus.SelectedCategorySprite != null ? SelectionBus.SelectedCategorySprite.name : "NULL")}");
    }
    
    [ContextMenu("Force Level 2 Refresh")]
    public void ForceLevel2Refresh()
    {
        if (level2Initializer != null)
        {
            level2Initializer.gameObject.SetActive(false);
            level2Initializer.gameObject.SetActive(true);
            Debug.Log("Level 2 initializer refreshed!");
        }
    }
}