using DG.Tweening;
using UnityEngine;

public static class UIAnimations
{
    private const float Duration = 0.3f;
    private const float Elasticity = 0.5f;

    public static void ToonIn(CanvasGroup canvas, RectTransform container, System.Action onComplete = null)
    {
        // Reset states
        canvas.alpha = 0;
        container.localScale = Vector3.zero;

        // Sequence for the "Pop"
        Sequence seq = DOTween.Sequence().SetUpdate(true); // SetUpdate(true) works even if game is paused
        seq.Append(canvas.DOFade(1f, 0.2f));
        seq.Join(container.DOScale(1f, Duration).SetEase(Ease.OutBack, 1.5f)); // OutBack gives that toon overshoot
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public static void ToonOut(CanvasGroup canvas, RectTransform container, System.Action onComplete = null)
    {
        // If the container is already null, don't even start
        if (container == null) return;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        // LINK the sequence to the GameObject. 
        // If 'container' is destroyed, the tween stops instantly.
        seq.SetLink(container.gameObject);

        seq.Append(container.DOScale(1.1f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(container.DOScale(0, Duration).SetEase(Ease.InBack));
        seq.Join(canvas.DOFade(0, 0.2f));
        seq.OnComplete(() => onComplete?.Invoke());
    }
}