using UnityEngine;

/// <summary>
/// Test script to verify carousel centering
/// </summary>
public class CarouselTester : MonoBehaviour
{
    [Header("Carousel Reference")]
    [SerializeField] private UICarouselSnap carousel;
    
    [Header("Test Controls")]
    [SerializeField] private KeyCode testKey0 = KeyCode.Alpha0;
    [SerializeField] private KeyCode testKey1 = KeyCode.Alpha1;
    [SerializeField] private KeyCode testKey2 = KeyCode.Alpha2;
    [SerializeField] private KeyCode testKey3 = KeyCode.Alpha3;
    [SerializeField] private KeyCode testKey4 = KeyCode.Alpha4;
    
    private void Start()
    {
        if (carousel == null)
            carousel = FindObjectOfType<UICarouselSnap>();
    }
    
    private void Update()
    {
        if (carousel == null) return;
        
        // Test keys for centering different items
        if (Input.GetKeyDown(testKey0)) TestCenterItem(0);
        if (Input.GetKeyDown(testKey1)) TestCenterItem(1);
        if (Input.GetKeyDown(testKey2)) TestCenterItem(2);
        if (Input.GetKeyDown(testKey3)) TestCenterItem(3);
        if (Input.GetKeyDown(testKey4)) TestCenterItem(4);
    }
    
    private void TestCenterItem(int index)
    {
        carousel.JumpTo(index, true);
        Debug.Log($"Testing center for item {index}");
    }
    
    private void OnGUI()
    {
        if (carousel == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 150));
        GUILayout.Label("Carousel Test Controls:");
        GUILayout.Label("Press 0-4 to center items");
        GUILayout.Label($"Current Center: {carousel.CenterIndex}");
        GUILayout.EndArea();
    }
}
