using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StarView : MonoBehaviour
{
    [SerializeField] private Image _bgIcon; // The "Empty" background star
    [SerializeField] private Image _icon;   // The "Filled" star that animates

    private Vector3 _originalScale;

    void Awake()
    {
        _icon.enabled = false;
        _originalScale = Vector3.one;
    }

    public void Init()
    {
        ResetView();
    }

    public void ResetView()
    {
        _icon.enabled = false;
        _icon.transform.DOKill();
        _icon.transform.localScale = _originalScale;
        // Keep the background icon visible so player sees the "empty" slot
        _bgIcon.enabled = true;
    }
    public void Show(float delay)
    {
        // 1. Initial State: Small, rotated, and invisible
        _icon.transform.localScale = Vector3.zero;
        _icon.transform.localRotation = Quaternion.Euler(0, 0, -180f); // Start with a half-turn offset
        _icon.enabled = true;

        Color c = _icon.color;
        c.a = 0;
        _icon.color = c;

        // 2. Build the Sequence
        Sequence starSeq = DOTween.Sequence();
        starSeq.SetDelay(delay);

        // FIRST: Fade in and Scale up while spinning
        // We scale slightly larger than 1.0 (e.g., 1.4) to create the "wind up" for the stomp
        starSeq.Append(_icon.DOFade(1f, 0.2f));
        starSeq.Join(_icon.transform.DOScale(2.0f, 0.5f).SetEase(Ease.OutBack));

        // 2-3 rotations (720 to 1080 degrees)
        starSeq.Join(_icon.transform.DORotate(new Vector3(0, 0, 1080f), 0.5f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutCubic));

        // SECOND: The "Stomp" (Fast scale down to 1.0)
        starSeq.Append(_icon.transform.DOScale(1f, 0.1f).SetEase(Ease.InExpo));

        // FINALLY: Impact Juice (Shake and Punch)
        starSeq.AppendCallback(() =>
        {
            // Shake the position for the "thud" effect
            _icon.transform.DOShakePosition(0.2f, 15f, 30);
            // Slight overshoot punch to make it feel "settled"
            _icon.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.2f, 10, 1f);

            // SoundController.instance.PlaySoundEffect("star");
            // Optional: Trigger haptic feedback or sound effect here
        });
    }
}