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
    [SerializeField] private float mergeHeight = 120f;

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
        GameEvents.OnRequestSteppedLeap += (d, to, cb) => StartCoroutine(SteppedLeapRoutine(d, to, cb));
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
        if (source.TryGetComponent<Collider>(out var col)) col.enabled = false;

        SlotView targetSlot = _slots[targetIdx];
        targetSlot.SetItemDataOnly(data);

        // Convert UI position to World Space for the 3D item to fly to
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 8f));

        source.DOMove(worldTarget, gameData.FlightToTrayDuration)
            .SetEase(Ease.InSine)
            .OnComplete(() =>
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

    private IEnumerator SteppedLeapRoutine(ItemData data, int targetIdx, Action onComplete)
    {
        SlotView fromSlot = _slots.FirstOrDefault(s => s.IsImageEnabled && s.CurrentItem == data);
        if (fromSlot == null || targetIdx >= _slots.Length) { onComplete?.Invoke(); yield break; }

        int startIdx = Array.IndexOf(_slots, fromSlot);
        if (startIdx == targetIdx) { onComplete?.Invoke(); yield break; }

        fromSlot.Clear();

        Image ghost = Instantiate(ghostIconPrefab, transform.parent);
        ghost.sprite = data.UISprite;
        ghost.transform.position = fromSlot.transform.position;

        int current = startIdx;
        int dir = (targetIdx > startIdx) ? 1 : -1;

        while (current != targetIdx)
        {
            current += dir;
            yield return ghost.transform.DOJump(_slots[current].transform.position, 40f, 1, 0.12f)
                .SetEase(Ease.OutQuad).WaitForCompletion();
        }

        _slots[targetIdx].SetItemDataOnly(data);
        _slots[targetIdx].RevealIcon();
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

        Vector3 peak = _slots[startIdx + 1].transform.position + Vector3.up * mergeHeight;
        Sequence s = DOTween.Sequence();
        foreach (var g in ghosts)
        {
            s.Join(g.transform.DOMove(peak, 0.4f).SetEase(Ease.InBack));
            s.Join(g.transform.DOScale(0, 0.4f));
        }

        yield return s.WaitForCompletion();
        foreach (var g in ghosts) if (g) Destroy(g.gameObject);
        onComplete?.Invoke();
    }
}