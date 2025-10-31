using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewCrystalPopulator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform panelColorStyle; // Panel_colorStyle parent - where to instantiate prefabs
    [SerializeField] private GameObject crystalHolderPrefab; // crystal_holder prefab to instantiate
    
    private List<GameObject> instantiatedHolders = new List<GameObject>();
    
    private void OnEnable()
    {
        PopulateCrystals();
    }
    
    public void PopulateCrystals()
    {
        if (SelectionBus.SelectedCrystalSprites == null || SelectionBus.SelectedCrystalSprites.Count == 0)
        {
            Debug.LogWarning("[PreviewCrystalPopulator] No crystals selected.");
            ClearInstantiatedHolders();
            return;
        }
        
        int count = SelectionBus.SelectedCrystalSprites.Count;
        
        Debug.Log($"[PreviewCrystalPopulator] Populating {count} crystals.");
        
        // Clear any previously instantiated holders
        ClearInstantiatedHolders();
        
        // Check if we have the required references
        if (crystalHolderPrefab == null)
        {
            Debug.LogError("[PreviewCrystalPopulator] crystalHolderPrefab is not assigned!");
            return;
        }
        
        if (panelColorStyle == null)
        {
            Debug.LogError("[PreviewCrystalPopulator] panelColorStyle (parent transform) is not assigned!");
            return;
        }
        
        // Instantiate the required number of crystal holders
        for (int i = 0; i < count; i++)
        {
            if (i >= SelectionBus.SelectedCrystalSprites.Count) break;
            
            Sprite sprite = SelectionBus.SelectedCrystalSprites[i];
            if (sprite == null)
            {
                Debug.LogWarning($"[PreviewCrystalPopulator] Sprite at index {i} is null.");
                continue;
            }
            
            // Instantiate the prefab
            GameObject holder = Instantiate(crystalHolderPrefab, panelColorStyle);
            
            // Find the Image component and set the sprite
            Image img = holder.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.sprite = sprite;
                img.preserveAspect = true;
                
                // Try to get the ImageIcon specifically
                Transform imageIcon = holder.transform.Find("ImageIcon");
                if (imageIcon != null)
                {
                    Image iconImg = imageIcon.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        iconImg.sprite = sprite;
                        iconImg.preserveAspect = true;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[PreviewCrystalPopulator] No Image component found on instantiated holder {i}.");
            }
            
            // Keep track of instantiated objects
            instantiatedHolders.Add(holder);
            
            Debug.Log($"[PreviewCrystalPopulator] Instantiated crystal holder {i + 1}/{count} with sprite: {sprite.name}");
        }
    }
    
    private void ClearInstantiatedHolders()
    {
        foreach (GameObject holder in instantiatedHolders)
        {
            if (holder != null)
            {
                Destroy(holder);
            }
        }
        instantiatedHolders.Clear();
    }
    
    private void OnDestroy()
    {
        ClearInstantiatedHolders();
    }
}

