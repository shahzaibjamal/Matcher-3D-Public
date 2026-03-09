using UnityEngine;
using DG.Tweening;

public class BroomSweeper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _broom;

    [Header("Movement Path")]
    [SerializeField] private Vector2 _startPosition = new Vector2(1000f, 0f);
    [SerializeField] private Vector2 _endPosition = new Vector2(-1000f, 0f);

    [Header("Animation Settings")]
    [SerializeField] private float _sweepDuration = 1.5f;
    [SerializeField] private float _dustingSpeed = 0.15f;
    [SerializeField] private float _tiltMin = -15f;
    [SerializeField] private float _tiltMax = -60f;

    private Sequence _activeSequence;

    [ContextMenu("Test Sweep Animation")] // Right-click component in Inspector to trigger
    public void PlayBroomSweep()
    {
        var source = SoundController.Instance.PlaySoundEffect("broom", true);
        // 1. Reset and Kill previous tweens
        _activeSequence?.Kill();
        _broom.DOKill();

        // 2. Initial State
        _broom.anchoredPosition = _startPosition;
        _broom.localRotation = Quaternion.Euler(0, 0, _tiltMin);

        // 3. Create Sequence
        _activeSequence = DOTween.Sequence();
        _activeSequence.SetId("BroomSweeper");
        // Start the "To and Fro" dusting (Independent loop)
        _broom.DOLocalRotate(new Vector3(0, 0, _tiltMax), _dustingSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Move from start to end
        _activeSequence.Append(_broom.DOAnchorPos(_endPosition, _sweepDuration).SetEase(Ease.Linear));

        // Cleanup
        _activeSequence.OnComplete(() =>
        {
            _broom.DOKill(); // Stops the rotation loop
            source.Stop();
        });
    }
}