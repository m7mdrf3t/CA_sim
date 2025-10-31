using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class LerpMover : MonoBehaviour
{
    [Header("Points (assign in Inspector)")]
    [Tooltip("Start / End point A")]
    public Transform pointA;

    [Tooltip("Start / End point B")]
    public Transform pointB;

    [Tooltip("Start / End point B")]
    public Transform pointC;

    [Header("Movement")]
    [Tooltip("Time in seconds for a one-way trip")]
    public float moveDuration = 1.5f;

    // internal coroutine reference – lets us stop a running move
    private Coroutine _currentMove;

    // --------------------------------------------------------------------
    // Public API
    // --------------------------------------------------------------------
    /// <summary>
    /// Move from current position (or A) to point B.
    /// </summary>
    public void MoveToB() => StartMove(pointB.position);

    /// <summary>
    /// Move from current position (or B) to point A.
    /// </summary>
    public void MoveToA() => StartMove(pointA.position);

        /// <summary>
    /// Move from current position (or B) to point A.
    /// </summary>
    public void MoveToC() => StartMove(pointC.position);

    // --------------------------------------------------------------------
    // Core logic
    // --------------------------------------------------------------------
    private void StartMove(Vector3 target)
    {
        // Stop any previous movement
        if (_currentMove != null)
            StopCoroutine(_currentMove);

        _currentMove = StartCoroutine(LerpTo(target, moveDuration));
    }

    private IEnumerator LerpTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // optional easing: t = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        // guarantee final position
        transform.position = target;
        _currentMove = null;
    }

    // --------------------------------------------------------------------
    // Editor helpers
    // --------------------------------------------------------------------
#if UNITY_EDITOR
    private void Reset()
    {
        // Auto-create two empty children as A & B when the script is added
        if (pointA == null) pointA = CreatePoint("PointA");
        if (pointB == null) pointB = CreatePoint("PointB");
    }

    private Transform CreatePoint(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    public void OnClick_GoToB()
    {
        MoveToB();
    }

    public void OnClick_GoToA()
    {
        MoveToA();
    }

    private void OnDrawGizmosSelected()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawSphere(pointA.position, 0.08f);
            Gizmos.DrawSphere(pointB.position, 0.08f);
        }
    }
#endif
}