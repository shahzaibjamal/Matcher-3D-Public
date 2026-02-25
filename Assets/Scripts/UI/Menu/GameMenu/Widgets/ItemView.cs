using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public int CurrentCount { get; private set; }
    public ItemData ItemData { get; private set; }
    private Action _onFinished;

    public void SetItem(ItemData data, int count, Action onFinished)
    {
        ItemData = data;
        CurrentCount = count;
        _onFinished = onFinished;
        UpdateUI();

        // Subscribe to global events directly
        GameEvents.OnRequestFlightEvent += HandleFlight;
        GameEvents.OnRequestMatchResolveEvent += HandleMatch;
    }

    private void OnDestroy()
    {
        GameEvents.OnRequestFlightEvent -= HandleFlight;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatch;
    }


    // public void UpdateCount(int delta)
    // {
    //     CurrentCount += delta;
    //     UpdateUI();
    // }

    private void UpdateUI()
    {
        icon.sprite = ItemData.UISprite;
        countText.text = CurrentCount.ToString();
    }
    private void HandleFlight(ItemData data, int target, Transform src, Action cb)
    {
        if (data.UID != ItemData.UID) return;

        CurrentCount--;
        UpdateUI();
        PlayMatchShake(); // The "pulse" on click
    }

    private void HandleMatch(int idx, ItemData[] datas, Action cb)
    {
        if (datas.Length == 0 || datas[0].UID != ItemData.UID) return;

        // Visual feedback only on match
        PlayMatchShake();

        if (CurrentCount <= 0)
        {
            // Unsubscribe early so it doesn't catch more events during animation
            GameEvents.OnRequestFlightEvent -= HandleFlight;
            GameEvents.OnRequestMatchResolveEvent -= HandleMatch;

            PlayCompletionAnimation(() =>
            {
                _onFinished?.Invoke(); // Tells State "I'm gone!"
                Destroy(gameObject);
            });
        }
    }
    public void PlayMatchShake()
    {
        transform.DOKill(true);
        // Refresh effect: Slight scale jump + a gentle shake
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f, 10, 1f);
        transform.DOShakeRotation(0.3f, new Vector3(0, 0, 10f));
    }

    public void PlayCompletionAnimation(Action onComplete)
    {
        transform.DOKill(true);
        // Moves up, scales down with InOutElastic
        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOMoveY(transform.position.y + 100f, 0.6f).SetEase(Ease.InOutElastic));
        seq.Join(transform.DOScale(Vector3.zero, 0.6f).SetEase(Ease.InOutElastic));

        seq.OnComplete(() => onComplete?.Invoke());
    }
}
