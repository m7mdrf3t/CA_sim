using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GatekeeperCanvasPriority : MonoBehaviour
{
    [SerializeField] Canvas gatekeeperCanvas;   // assign Canvas gatekeeper
    [SerializeField] GatekeeperOverlay overlay; // assign GatekeeperOverlay

    GraphicRaycaster[] others;

    void Awake()
    {
        if (gatekeeperCanvas != null) gatekeeperCanvas.sortingOrder = 1000;
        others = FindObjectsOfType<GraphicRaycaster>(true)
                 .Where(gr => gr.GetComponentInParent<Canvas>() != gatekeeperCanvas)
                 .ToArray();

        // Hook the overlay show/hide by polling its active state (simple & safe).
        InvokeRepeating(nameof(RefreshStates), 0.15f, 0.15f);
    }

    void OnDestroy() => CancelInvoke();

    void RefreshStates()
    {
        bool overlayVisible = overlay != null && overlay.isActiveAndEnabled &&
                              overlay.gameObject.activeInHierarchy;

        foreach (var gr in others)
        {
            if (gr != null) gr.enabled = !overlayVisible; // off when gatekeeper is up
        }
    }
}