using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class TrayView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private SlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Image ghostIconPrefab;

    [Header("Animation Settings")]
    [SerializeField] private float flightDuration = 0.6f;
    [SerializeField] private float cameraZOffset = 12.0f;
    [SerializeField] private float mergeUpHeight = 80f;
    [SerializeField] private ParticleSystem matchParticlePrefab;

    private SlotView[] _slots;
    private SlotManager _slotManager;
    private int _itemsInFlight = 0;

    public void Initialize(SlotManager slotManager, int slotCount)
    {
        _slotManager = slotManager;

        foreach (Transform child in slotParent) Destroy(child.gameObject);

        _slots = new SlotView[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            _slots[i] = Instantiate(slotPrefab, slotParent);
            _slots[i].SetIndex(i);
        }

        // --- SUBSCRIPTIONS ---
        _slotManager.OnItemLeaped += HandleLeap;

        // When a new item is reserved (via AddItem), update data but keep icon hidden
        _slotManager.OnNewItemReserved += (index, item) => _slots[index].SetItemDataOnly(item);

        _slotManager.OnMatchFound += HandleMatch;
        GameEvents.OnItemCollected += AnimateItemToTray;
    }

    private void HandleLeap(int from, int to, ItemData item)
    {
        if (ghostIconPrefab == null || item == null) return;

        // 1. Prepare the destination slot's DATA
        _slots[to].SetItemDataOnly(item);

        // 2. Create the Ghost
        Image ghost = Instantiate(ghostIconPrefab, transform.parent);
        ghost.sprite = item.UISprite;
        ghost.transform.position = _slots[from].IconTransform.position;

        // 3. VISUAL HANDOFF
        // Hide the source icon immediately (the ghost is now representing it)
        _slots[from].HideIcon();

        // Ensure the target icon is hidden while the ghost is traveling
        _slots[to].HideIcon();

        ghost.transform.DOJump(_slots[to].transform.position, 60f, 1, 0.3f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 4. LANDING
                // Only reveal if the logic still says this specific item is in this slot
                if (_slots[to].CurrentItem == item)
                {
                    _slots[to].RevealIcon();
                }
                Destroy(ghost.gameObject);
            });
    }
    public void AnimateItemToTray(ItemData itemData, Transform sourceTransform)
    {
        _itemsInFlight++;

        // This triggers HandleLeap for existing items and OnNewItemReserved for the new one
        _slotManager.AddItem(itemData);

        if (sourceTransform.TryGetComponent<Renderer>(out var rend))
        {
            rend.sortingLayerName = "UI";
            rend.sortingOrder = 32767;
        }

        int targetIndex = FindFirstHiddenSlotOfItem(itemData);

        // Safety check if index is invalid
        if (targetIndex == -1) return;

        Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(
            _slots[targetIndex].transform.position.x,
            _slots[targetIndex].transform.position.y,
            cameraZOffset
        ));

        Sequence seq = DOTween.Sequence();
        Vector3 peak = Vector3.Lerp(sourceTransform.position, targetWorldPos, 0.5f) + Vector3.up * 3f;

        seq.Append(sourceTransform.DOMove(peak, flightDuration * 0.4f).SetEase(Ease.OutQuad));
        seq.Join(sourceTransform.DORotate(Vector3.zero, flightDuration * 0.6f));

        seq.Append(sourceTransform.DOMove(targetWorldPos, flightDuration * 0.6f).SetEase(Ease.InBack));
        seq.Join(sourceTransform.DOScale(Vector3.one * 0.3f, flightDuration * 0.6f));

        seq.OnComplete(() =>
        {
            _itemsInFlight--;
            Destroy(sourceTransform.gameObject);

            // Reveal the landing icon
            _slots[targetIndex].RevealIcon();

            // if (_itemsInFlight <= 0)
            {
                _slotManager.RequestMatchCheck();
            }
        });
    }

    private void HandleMatch(int startIndex)
    {
        StartCoroutine(PlayMergeAnimation(startIndex));
    }

    private IEnumerator PlayMergeAnimation(int startIndex)
    {
        ItemData[] itemsToMerge = new ItemData[3];
        for (int i = 0; i < 3; i++) itemsToMerge[i] = _slots[startIndex + i].CurrentItem;

        Vector3 centerSlotPos = _slots[startIndex + 1].transform.position;
        Vector3 highMergePoint = centerSlotPos + (_slots[startIndex + 1].transform.up * mergeUpHeight);
        Sequence mergeSeq = DOTween.Sequence();

        for (int i = 0; i < 3; i++)
        {
            Transform icon = _slots[startIndex + i].IconTransform;
            icon.DOKill();
            Vector3 peakPos = icon.position + (icon.up * mergeUpHeight);
            mergeSeq.Join(icon.DOMove(peakPos, 0.25f).SetEase(Ease.OutQuad));
        }

        mergeSeq.AppendInterval(0.05f);

        for (int i = 0; i < 3; i++)
        {
            Transform icon = _slots[startIndex + i].IconTransform;
            mergeSeq.Join(icon.DOMove(highMergePoint, 0.2f).SetEase(Ease.InBack));
            mergeSeq.Join(icon.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        }

        yield return mergeSeq.WaitForCompletion();

        if (matchParticlePrefab) Instantiate(matchParticlePrefab, highMergePoint, Quaternion.identity);

        // CLEANUP
        for (int i = 0; i < 3; i++)
        {
            // Use the safe clear: only wipe if the item hasn't been replaced by a leap
            _slots[startIndex + i].ClearIfMatch(itemsToMerge[i]);
        }
    }

    private int FindFirstHiddenSlotOfItem(ItemData item)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            // A slot is "hidden" if it has the data but its Image component is disabled
            if (_slots[i].CurrentItem == item && !_slots[i].IsImageEnabled)
                return i;
        }
        return -1;
    }
}