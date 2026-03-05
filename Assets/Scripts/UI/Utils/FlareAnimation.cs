using UnityEngine;
using UnityEngine.UI; // Required for Image component
using DG.Tweening;

public class UIFlareAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _minScale = 0.5f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _delay = 0.2f;
    [SerializeField] private Ease _easeType = Ease.InOutSine;

    [Header("Color Settings")]
    [SerializeField] private Color _colorStart = Color.white;
    [SerializeField] private Color _colorEnd = new Color(1f, 0.9f, 0.5f, 1f); // Soft Gold

    [Header("Control")]
    [SerializeField] private bool _playOnEnable = true;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Image _image;
    private Sequence _flareSequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (_playOnEnable) Play();
    }

    public void Play()
    {
        KillAnimation();

        // Initial State
        _rectTransform.localScale = Vector3.one * _minScale;
        if (_image != null) _image.color = _colorStart;
        _canvasGroup.alpha = 0;

        _flareSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetDelay(_delay)
            .SetLoops(-1, LoopType.Yoyo);

        // 1. Scale Up/Down
        _flareSequence.Append(_rectTransform.DOScale(_maxScale, _duration).SetEase(_easeType));

        // 2. Color Transition (Simultaneous)
        if (_image != null)
        {
            _flareSequence.Join(_image.DOColor(_colorEnd, _duration).SetEase(_easeType));
        }

        // 3. Fade In (Only on first play or loop)
        _flareSequence.Join(_canvasGroup.DOFade(1f, _duration).SetEase(_easeType));
    }

    private void OnDisable() => KillAnimation();
    private void OnDestroy() => KillAnimation();

    private void KillAnimation()
    {
        _flareSequence?.Kill();
        // Return to start color/scale if you want it to look "off" when disabled
        if (_image != null) _image.color = _colorStart;
    }
}