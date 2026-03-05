using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIFlareAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float _minScale = 0.5f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _rotationAngle = 90f; // The angle it will rotate to
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _delay = 0.2f;
    [SerializeField] private Ease _easeType = Ease.InOutSine;

    [Header("Color Settings")]
    [SerializeField] private Color _colorStart = Color.white;
    [SerializeField] private Color _colorEnd = new Color(1f, 0.9f, 0.5f, 1f);

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

        // 1. Calculate the "Chunk" timeline
        // Total cycle = Delay + (Up Duration) + (Down Duration)
        float upTime = _duration;
        float downTime = _duration;
        float totalCycleTime = _delay + upTime + downTime;

        _rectTransform.localScale = Vector3.one * _minScale;
        _canvasGroup.alpha = 0;

        _flareSequence = DOTween.Sequence()
            .SetUpdate(true)
            .SetLoops(-1, LoopType.Restart);

        // 2. Schedule events at precise timestamps
        // We "Insert" them so they are anchored to the start of the clock (0)

        // START POP AT: _delay
        _flareSequence.Insert(_delay, _rectTransform.DOScale(_maxScale, upTime).SetEase(_easeType));
        _flareSequence.Insert(_delay, _canvasGroup.DOFade(1f, upTime).SetEase(_easeType));

        // START SHRINK AT: _delay + upTime
        float shrinkStart = _delay + upTime;
        _flareSequence.Insert(shrinkStart, _rectTransform.DOScale(_minScale, downTime).SetEase(_easeType));
        _flareSequence.Insert(shrinkStart, _canvasGroup.DOFade(0f, downTime).SetEase(_easeType));
        _flareSequence.Insert(shrinkStart, _rectTransform.DOLocalRotate(new Vector3(0, 0, _rotationAngle), downTime, RotateMode.LocalAxisAdd).SetEase(_easeType));

        // 3. Add a "dummy" callback at the very end to force the sequence to last the full duration
        _flareSequence.InsertCallback(totalCycleTime, () => { });
    }
    private void OnDisable() => KillAnimation();
    private void OnDestroy() => KillAnimation();

    private void KillAnimation()
    {
        _flareSequence?.Kill();
    }
}