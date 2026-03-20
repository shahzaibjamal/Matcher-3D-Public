using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class MagnifyingGlassSearch : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform glass;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Saved Path (Absolute)")]
    // This list stores the exact anchoredPositions you save
    [SerializeField] private List<Vector2> absolutePath = new List<Vector2>();

    [Header("Timing")]
    [SerializeField] private float duration = 3.5f;

    // --- EDITOR TOOLS ---

    [ContextMenu("1. Record Current Position")]
    public void RecordPosition()
    {
        if (glass == null) return;
        absolutePath.Add(glass.anchoredPosition);
        Debug.Log($"<color=green>Recorded:</color> {glass.anchoredPosition} (Point {absolutePath.Count})");
    }

    [ContextMenu("2. Clear All Points")]
    public void ClearPoints()
    {
        absolutePath.Clear();
        Debug.Log("<color=red>Path Cleared.</color>");
    }

    [ContextMenu("3. Play Search Wave")]
    public void PlaySearch()
    {
        if (absolutePath.Count < 2) return;

        Vector3[] path = new Vector3[absolutePath.Count];
        for (int i = 0; i < absolutePath.Count; i++)
        {
            // Vector2/3 conversion happens automatically here
            path[i] = absolutePath[i];
        }

        glass.DOKill();
        glass.anchoredPosition = path[0];
        canvasGroup.alpha = 0;

        Sequence s = DOTween.Sequence();
        // s.Append(canvasGroup.DOFade(1, fadeTime));

        // SWITCH TO DOLocalPath
        // This ignores World Space and uses the numbers exactly as you recorded them
        s.Join(glass.DOLocalPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InOutQuad));

        // s.Append(canvasGroup.DOFade(0, fadeTime));

        float halfDuration = duration / 2f;
        s.Insert(0, canvasGroup.DOFade(1f, halfDuration).SetEase(Ease.InQuad));

        // 3. Fade Out: Starts at the halfway point, lasts for the second half
        s.Insert(halfDuration, canvasGroup.DOFade(0f, halfDuration).SetEase(Ease.OutQuad));
    }

    // --- VISUAL FEEDBACK ---

    private void OnDrawGizmosSelected()
    {
        if (glass == null || absolutePath.Count < 2) return;

        Gizmos.color = Color.cyan;
        // Get the parent transform to draw correctly in world space
        Transform parent = glass.parent;

        for (int i = 0; i < absolutePath.Count - 1; i++)
        {
            Vector3 start = parent.TransformPoint(absolutePath[i]);
            Vector3 end = parent.TransformPoint(absolutePath[i + 1]);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(start, 10f); // Draw a small dot at each point
        }
    }
}