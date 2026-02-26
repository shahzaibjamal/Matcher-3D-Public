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
        // 1. Initial State: Keep it hidden and pre-scaled
        ResetView();
        _icon.transform.localScale = Vector3.one * 3f;

        // Set alpha to 0 immediately
        Color c = _icon.color;
        c.a = 0;
        _icon.color = c;

        // 2. Build the Sequence
        Sequence slamSeq = DOTween.Sequence();

        // The sequence will wait for its specific staggered delay
        slamSeq.SetDelay(delay);

        // FIRST: Enable the icon only when the delay is over
        slamSeq.AppendCallback(() =>
        {
            _icon.enabled = true;
        });

        // NEXT: Start the slam and fade simultaneously
        slamSeq.Append(_icon.transform.DOScale(1f, 0.25f).SetEase(Ease.InExpo));
        slamSeq.Join(_icon.DOFade(1f, 0.15f));

        // FINALLY: The Impact Juice
        slamSeq.AppendCallback(() =>
        {
            _icon.transform.DOShakePosition(0.2f, 10f, 20);
            _icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
        });
    }
}