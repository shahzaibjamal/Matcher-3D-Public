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
        GameEvents.OnRequestFlightEvent += HandleFlight;
        GameEvents.OnRequestMatchResolveEvent += HandleMatch;
        GameEvents.OnUndoAddItemEvent += HandleUndo;
    }

    private void OnDestroy()
    {
        GameEvents.OnRequestFlightEvent -= HandleFlight;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatch;
        GameEvents.OnUndoAddItemEvent -= HandleUndo;
    }


    private void UpdateUI()
    {
        AssetLoader.Instance.LoadIcon(ItemData.IconName, (sprite) =>
        {
            icon.sprite = sprite;
        });

        countText.text = CurrentCount.ToString();
    }
    private void HandleFlight(ItemData data, int target, Transform src, Action cb)
    {
        if (data.Id != ItemData.Id) return;

        CurrentCount--;
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
            GameEvents.OnRequestFlightEvent -= HandleFlight;
            GameEvents.OnRequestMatchResolveEvent -= HandleMatch;

            PlayCompletionAnimation(() =>
            {
                _onFinished?.Invoke(); // Tells State "I'm gone!"
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

        var color = GetMostAbundantColor(icon.sprite);

        // 2. Get the module FROM the instance (this is what the error asks for)
        var mainModule = _particleSystem.main;

        // 3. Create the new gradient and assign it back
        // Setting it this way replaces the entire startColor struct correctly
        mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
        _particleSystem.Play();

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public Color32 GetMostAbundantColor(Sprite sprite)
    {
        // 1. Get the pixel data from the sprite's texture area
        // Note: Texture must be set to 'Read/Write Enabled' in Import Settings
        Texture2D tex = sprite.texture;
        Rect rect = sprite.rect;
        Color32[] pixels = tex.GetPixels32();

        Dictionary<Color32, int> colorCounts = new Dictionary<Color32, int>();

        int startX = (int)rect.x;
        int startY = (int)rect.y;
        int width = (int)rect.width;
        int height = (int)rect.height;

        // 2. Iterate through the sprite's specific pixels within the larger texture
        for (int y = startY; y < startY + height; y++)
        {
            for (int x = startX; x < startX + width; x++)
            {
                // Map 2D coordinates to the 1D array
                Color32 pixel = pixels[y * tex.width + x];

                // Skip fully transparent pixels
                if (pixel.a == 0) continue;

                if (colorCounts.ContainsKey(pixel))
                    colorCounts[pixel]++;
                else
                    colorCounts[pixel] = 1;
            }
        }

        // 3. Find the entry with the highest count
        Color32 mostAbundant = new Color32(0, 0, 0, 255);
        int maxCount = -1;

        foreach (var kvp in colorCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostAbundant = kvp.Key;
            }
        }

        return mostAbundant;
    }
}
