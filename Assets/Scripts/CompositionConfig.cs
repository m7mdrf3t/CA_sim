using UnityEngine;
using System;

/// <summary>
/// Config file for composition categories - like GatekeeperConfig
/// Create one asset and assign all category data here
/// </summary>
[CreateAssetMenu(fileName = "CompositionConfig", menuName = "Crystal Studio/Composition Config")]
public class CompositionConfig : ScriptableObject
{
    [Header("CATEGORY 1 - Central")]
    public string category1Title = "Central";
    public ShapeType shapeType1 = ShapeType.Cylinder;
    public Sprite category1Placeholder;
    public string category1PlaceholderName = "Central";
    public Sprite[] category1GallerySprites = new Sprite[4];
    public SurfaceType[] surfaceTypes1 = new SurfaceType[4];
    
    [Header("CATEGORY 2 - Vertical")]
    public string category2Title = "Vertical";
    public ShapeType shapeType2 = ShapeType.Cylinder;
    public Sprite category2Placeholder;
    public string category2PlaceholderName = "Vertical";
    public Sprite[] category2GallerySprites = new Sprite[4];
    public SurfaceType[] surfaceTypes2 = new SurfaceType[4];

    
    [Header("CATEGORY 3 - Horizontal")]
    public string category3Title = "Horizontal";
    public Sprite category3Placeholder;
    public string category3PlaceholderName = "Horizontal";
    public Sprite[] category3GallerySprites = new Sprite[4];
    
    [Header("CATEGORY 4 - Diagonal")]
    public string category4Title = "Diagonal";
    public Sprite category4Placeholder;
    public string category4PlaceholderName = "Diagonal";
    public Sprite[] category4GallerySprites = new Sprite[4];
    
    /// <summary>
    /// Get the category data at index (0-3)
    /// </summary>
    public CompositionConfig.CategoryData GetCategoryData(int index)
    {
        switch (index)
        {
            case 0: return new CategoryData 
            { 
                title = category1Title, 
                placeholder = category1Placeholder, 
                shapeType = shapeType1,
                surfaceType = surfaceTypes1,
                placeholderName = category1PlaceholderName,
                gallerySprites = category1GallerySprites 
            };
            case 1: return new CategoryData 
            { 
                title = category2Title, 
                placeholder = category2Placeholder, 
                shapeType = shapeType2,
                surfaceType = surfaceTypes2,
                placeholderName = category2PlaceholderName,
                gallerySprites = category2GallerySprites 
            };
            case 2: return new CategoryData 
            { 
                title = category3Title, 
                placeholder = category3Placeholder, 
                placeholderName = category3PlaceholderName,
                gallerySprites = category3GallerySprites 
            };
            case 3: return new CategoryData 
            { 
                title = category4Title, 
                placeholder = category4Placeholder, 
                placeholderName = category4PlaceholderName,
                gallerySprites = category4GallerySprites 
            };
            default: return new CategoryData();
        }
    }
    
    [Serializable]
    public class CategoryData
    {
        public string title;
        public ShapeType shapeType;
        public SurfaceType[] surfaceType;
        public Sprite placeholder;
        public string placeholderName;
        public Sprite[] gallerySprites;
    }
}




