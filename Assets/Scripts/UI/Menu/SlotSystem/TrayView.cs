using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;
using System.Linq;

public class TrayView : MonoBehaviour
{
    [SerializeField] private SlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Image ghostIconPrefab;
    [SerializeField] private GameData gameData;

    private SlotView[] _slots;

    public void Initialize(int slotCount)
    {
        // Kill existing slots if any (Cleanup)
        foreach (Transform child in slotParent) Destroy(child.gameObject);

        _slots = new SlotView[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            _slots[i] = Instantiate(slotPrefab, slotParent);
            _slots[i].SetIndex(i);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnRequestFlight += HandleFlight;
        GameEvents.OnRequestSteppedLeap += (d, from, to, cb) => StartCoroutine(SteppedLeapRoutine(d, from, to, cb));
        GameEvents.OnRequestMatchResolve += (idx, data, cb) => StartCoroutine(MatchGhostSequence(idx, data, cb));
    }

    private void OnDisable()
    {
        GameEvents.OnRequestFlight -= HandleFlight;
        // Clean up coroutine references if needed
    }

    private void HandleFlight(ItemData data, int targetIdx, Transform source, Action onComplete)
    {
        // SAFETY CHECK: Line 46 fix
        if (_slots == null || targetIdx < 0 || targetIdx >= _slots.Length)
        {
            Debug.LogError($"[TrayView] Target index {targetIdx} is out of bounds or _slots is null!");
            onComplete?.Invoke();
            return;
        }

        if (source == null) return;
        if (source.TryGetComponent<ClickableItem>(out var clickableItem))
        {
            clickableItem.Rigidbody.isKinematic = false;
            clickableItem.Collider.enabled = false;
        }

        SlotView targetSlot = _slots[targetIdx];
        targetSlot.SetItemDataOnly(data);

        // Convert UI position to World Space for the 3D item to fly to
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 8f));
        Sequence flightSeq = DOTween.Sequence();
        flightSeq.Append(source.DOMove(source.position + Vector3.up * 2f, gameData.FlightUpDuration).SetEase(Ease.OutQuad));
        flightSeq.Join(source.DORotate(Vector3.zero, gameData.FlightUpDuration, RotateMode.FastBeyond360));
        flightSeq.Join(source.DOScale(source.localScale * 1.2f, gameData.FlightUpDuration));

        // This triggers HandleLeap for existing items and OnNewItemReserved for the new one
        // STAGE B: The Toss (Move to UI Slot)
        flightSeq.Append(source.DOMove(worldTarget, gameData.FlightToTrayDuration).SetEase(Ease.InBack));
        flightSeq.Join(source.DOScale(Vector3.one * 0.3f, gameData.FlightToTrayDuration)); // Shrink to fit UI
        flightSeq.OnComplete(() =>
        {
            // Double check slot still holds this data (in case of rapid shifts)
            if (targetSlot.CurrentItem == data)
                targetSlot.RevealIcon();
            else
                RevealCorrectDataSlot(data);

            Destroy(source.gameObject);
            onComplete?.Invoke();
        });
    }

    private void RevealCorrectDataSlot(ItemData data)
    {
        var actualSlot = _slots.FirstOrDefault(s => s.CurrentItem == data);
        actualSlot?.RevealIcon();
    }

    private IEnumerator SteppedLeapRoutine(ItemData data, int from, int targetIdx, Action onComplete)
    {
        if (from < 0 || from >= _slots.Length) { onComplete?.Invoke(); yield break; }

        SlotView fromSlot = _slots[from];

        // 1. Clear the visual immediately
        fromSlot.Clear();

        // 2. Setup Ghost
        Image ghost = Instantiate(ghostIconPrefab, transform.parent);
        ghost.sprite = data.UISprite;
        ghost.transform.position = fromSlot.transform.position;

        // 3. Preparation: The target slot should know it's getting this data
        // so that if a match triggers mid-leap, the logic stays consistent.
        _slots[targetIdx].SetItemDataOnly(data);

        // 4. STEPPING LOGIC
        int currentIdx = from;
        int direction = (targetIdx > from) ? 1 : -1;

        while (currentIdx != targetIdx)
        {
            currentIdx += direction;

            // Jump to the NEXT neighbor slot
            yield return ghost.transform.DOJump(_slots[currentIdx].transform.position, 40f, 1, gameData.LeapDuration)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }

        // 5. Finalize Reveal
        // We check UniqueId because the item might have been matched/cleared 
        // while the Coroutine was yielding during the jumps.
        if (_slots[targetIdx].CurrentItem?.UniqueId == data.UniqueId)
        {
            _slots[targetIdx].RevealIcon();
        }

        Destroy(ghost.gameObject);
        onComplete?.Invoke();
    }
    private IEnumerator MatchGhostSequence(int startIdx, ItemData[] data, Action onComplete)
    {
        yield return new WaitForSeconds(0.05f);

        Image[] ghosts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            ghosts[i] = Instantiate(ghostIconPrefab, transform.parent);
            ghosts[i].sprite = data[i].UISprite;
            ghosts[i].transform.position = _slots[startIdx + i].transform.position;
            _slots[startIdx + i].Clear();
        }

        Vector3 centerPoint = _slots[startIdx + 1].transform.position;
        Vector3 peakPoint = centerPoint + Vector3.up * 100;
        Sequence mergeSeq = DOTween.Sequence();
        foreach (var g in ghosts)
        {
            Transform icon = g.transform;
            mergeSeq.Join(icon.DOMove(peakPoint, gameData.MergeDuration).SetEase(Ease.InBack));
            mergeSeq.Join(icon.DOScale(Vector3.zero, gameData.MergeDuration).SetEase(Ease.InBack));
        }

        yield return mergeSeq.WaitForCompletion();
        foreach (var g in ghosts) if (g) Destroy(g.gameObject);
        onComplete?.Invoke();
    }
}