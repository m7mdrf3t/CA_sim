using UnityEngine;
using UnityEngine.UI;

public class ButtonCircleHighlighter : MonoBehaviour
{
    [System.Serializable]
    public class HighlightButton
    {
        public Button button;          // The clickable button
        public GameObject highlight;   // The circle under it
    }

    [Header("Buttons and Circles")]
    public HighlightButton[] buttonGroups;

    private int currentIndex = -1;

    void Start()
    {
        // Attach click listeners
        for (int i = 0; i < buttonGroups.Length; i++)
        {
            int index = i; // Capture loop variable
            if (buttonGroups[i].button != null)
                buttonGroups[i].button.onClick.AddListener(() => OnButtonClicked(index));

            // Ensure only one active highlight at start
            if (buttonGroups[i].highlight != null)
                buttonGroups[i].highlight.SetActive(false);
        }
    }

    private void OnButtonClicked(int index)
    {
        // Disable all highlights
        for (int i = 0; i < buttonGroups.Length; i++)
        {
            if (buttonGroups[i].highlight != null)
                buttonGroups[i].highlight.SetActive(i == index);
        }

        currentIndex = index;
        Debug.Log($"Selected Button: {buttonGroups[index].button.name}");
    }
}