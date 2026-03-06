using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private ParticleSystem _particleSystem;

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
        GameEvents.OnItemAddedToSlotEvent += OnItemAddedToSlot;
        GameEvents.OnRequestMatchResolveEvent += HandleMatch;
    }

    private void OnDestroy()
    {
        GameEvents.OnItemAddedToSlotEvent -= OnItemAddedToSlot;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatch;
    }


    private void UpdateUI()
    {
        AssetLoader.Instance.LoadIcon(ItemData.IconName, (sprite) =>
        {
            icon.sprite = sprite;
        });

        countText.text = CurrentCount.ToString();
    }
    private void OnItemAddedToSlot(ItemData data, int target, Transform src, bool isAdded, Action cb)
    {
        if (data.Id != ItemData.Id) return;
        CurrentCount += isAdded ? -1 : 1; // -1 added to the slots so minus 1 from the collectable Item view
        UpdateUI();
        PlayMatchShake(); // The "pulse" on click
    }

    private void HandleMatch(int idx, ItemData[] datas, Action cb)
    {
        if (datas.Length == 0 || datas[0].Id != ItemData.Id) return;

        // Visual feedback only on match
        PlayMatchShake();

        if (CurrentCount <= 0)
        {
            // Unsubscribe early so it doesn't catch more events during animation
            GameEvents.OnItemAddedToSlotEvent -= OnItemAddedToSlot;
            GameEvents.OnRequestMatchResolveEvent -= HandleMatch;

            PlayCompletionAnimation(() =>
            {
                _onFinished?.Invoke(); // Tells State "I'm gone!"
                SoundController.instance.PlaySoundEffect("item_complete");
                Destroy(gameObject);
            });
        }
    }
    private void HandleUndo(string id)
    {
        if (ItemData == null || id != ItemData.Id) return;

        CurrentCount++;
        UpdateUI();
        PlayMatchShake();
    }
    public void PlayMatchShake()
    {
        transform.DOKill(true);
        // Refresh effect: Slight scale jump + a gentle shake
        transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.3f, 10, 1f).SetId("ItemView: MatchShake Scale"); ;
        transform.DOShakeRotation(0.3f, new Vector3(0, 0, 10f)).SetId("ItemView: MatchShake Rotation"); ;
    }

    public void PlayCompletionAnimation(Action onComplete)
    {
        transform.DOKill(true);
        // Moves up, scales down with InOutElastic
        Sequence seq = DOTween.Sequence();
        seq.SetId("ItemView: Completion");
        seq.SetDelay(_particleSystem.main.duration);
        seq.Append(transform.DOMoveY(transform.position.y + 100f, 0.6f).SetEase(Ease.InOutElastic));
        seq.Join(transform.DOScale(Vector3.zero, 0.6f).SetEase(Ease.InOutElastic));

        _particleSystem.Play();

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

}
