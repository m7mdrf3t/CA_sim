using System;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds data for a composition category in the carousel
/// Each category has a placeholder image and a gallery of images
/// </summary>
[CreateAssetMenu(fileName = "CompositionCategory", menuName = "Crystal Studio/Composition Category")]
public class CompositionCarouselData : ScriptableObject
{
    [Header("CATEGORY NAME FOR CAPTION")]
    [Tooltip("This is the name that appears in the carousel caption (e.g., 'Central', 'Vertical')")]
    public string displayTitle = "Central";
    
    [Header("PLACEHOLDER SPRITE")]
    [Tooltip("The main image shown in the carousel card and as hero image")]
    public Sprite placeholderImage;
    public string placeholderName = "Placeholder"; // Optional name for the placeholder
    
    [Header("BUTTON SPRITES FOR SHAPES")]
    [Tooltip("These are your gallery images - just drag them here (simple!)")]
    public Sprite[] gallerySprites; // Simple array: drag your sprites here!
    
    [Header("Gallery Items (Advanced - if you want custom names per image)")]
    [Tooltip("Use this if you want to name each gallery image individually")]
    public GalleryItem[] galleryItems;
    
    [Serializable]
    public class GalleryItem
    {
        public string imageName;
        public Sprite imageSprite;
    }
}

