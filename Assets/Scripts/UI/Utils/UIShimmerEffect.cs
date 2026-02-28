using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
public class UIShimmerEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private RectTransform shimmerImage;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float delayBetweenFlashes = 3.0f;
    [SerializeField] private Ease moveEase = Ease.Linear;
    [SerializeField] private bool playOnAwake = true;

    private Sequence _shimmerSequence;
    [SerializeField] private RectTransform _parentRect;


    private void OnEnable()
    {
        if (playOnAwake) Play();
    }

    private void OnDisable()
    {
        _shimmerSequence?.Kill();
    }

    public void Play()
    {
        if (shimmerImage == null) return;

        _shimmerSequence?.Kill();
        _shimmerSequence = DOTween.Sequence();

        // Calculate travel distance based on parent width
        // We add a buffer so it starts and ends completely out of view
        float width = _parentRect.rect.width;
        float offset = width * 1.5f;

        float startX = -offset;
        float endX = offset;

        // Reset position initially
        shimmerImage.anchoredPosition = new Vector2(startX, 0);

        // Build the loop
        _shimmerSequence.Append(shimmerImage.DOAnchorPosX(endX, duration).SetEase(moveEase))
                        .AppendInterval(delayBetweenFlashes)
                        .SetLoops(-1);
    }

    public void Stop()
    {
        _shimmerSequence?.Pause();
    }
}