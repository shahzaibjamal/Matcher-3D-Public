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
    [SerializeField] private ParticleSystem PoofParticle;
    [SerializeField] private float _flightUpDuration = 0.5f;
    [SerializeField] private float _flightToTrayDuration = 0.5f;
    [SerializeField] private float _leapDuration = 0.5f;

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
        GameEvents.OnItemAddedToSlotEvent += HandleItemAddedToSlot;
        // USE NAMED METHODS HERE
        GameEvents.OnRequestSteppedLeapEvent += HandleSteppedLeapRequest;
        GameEvents.OnRequestMatchResolveEvent += HandleMatchResolveRequest;
    }

    private void OnDisable()
    {
        GameEvents.OnItemAddedToSlotEvent -= HandleItemAddedToSlot;
        // NOW THESE WILL ACTUALLY UNSUBSCRIBE
        GameEvents.OnRequestSteppedLeapEvent -= HandleSteppedLeapRequest;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatchResolveRequest;
    }

    // Wrapper methods to bridge the Event to the Coroutine
    private void HandleSteppedLeapRequest(ItemData d, int from, int to, Action cb)
    {
        // Safety check: Unity objects can be "fake null" when destroyed
        if (this == null) return;
        StartCoroutine(SteppedLeapRoutine(d, from, to, cb));
    }

    private void HandleMatchResolveRequest(int idx, ItemData[] data, Action cb)
    {
        if (this == null) return;
        StartCoroutine(MatchGhostSequence(idx, data, cb));
    }

    private void HandleItemAddedToSlot(ItemData data, int targetIdx, Transform source, bool isAdded, Action onComplete)
    {
        if (_slots == null || targetIdx < 0 || targetIdx >= _slots.Length)
        {
            Debug.LogError($"[TrayView] Target index {targetIdx} is out of bounds or _slots is null!");
            onComplete?.Invoke();
            return;
        }
        if (!isAdded)
        {
            // removed via undo or cleansweep
            // add tween for removing and the call onCompelte
            _slots[targetIdx].Clear();
            _slots[targetIdx].PlayPoof();

            Scheduler.Instance.ExecuteAfterDelay(0.2f, () =>
            {
                onComplete?.Invoke();
            });
            return;
        }
        SoundController.instance.PlaySoundEffect("pick");

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
        flightSeq.SetId("TrayView: item Flight");
        flightSeq.Append(source.DOMove(source.position + Vector3.up * 2f, _flightUpDuration).SetEase(Ease.OutQuad));
        flightSeq.Join(source.DORotate(Vector3.zero, _flightUpDuration, RotateMode.FastBeyond360));
        flightSeq.Join(source.DOScale(source.localScale * 1.2f, _flightUpDuration));

        // This triggers HandleLeap for existing items and OnNewItemReserved for the new one
        // STAGE B: The Toss (Move to UI Slot)
        flightSeq.Append(source.DOMove(worldTarget, _flightToTrayDuration).SetEase(Ease.InBack));
        flightSeq.Join(source.DOScale(Vector3.one * 0.3f, _flightToTrayDuration)); // Shrink to fit UI
        flightSeq.OnComplete(() =>
        {
            // Double check slot still holds this data (in case of rapid shifts)
            if (targetSlot.CurrentItem == data)
                targetSlot.RevealIcon(true);
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
        AssetLoader.Instance.LoadIcon(data.IconName, (sprite) =>
        {
            ghost.sprite = sprite;
        });

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
            yield return ghost.transform.DOJump(_slots[currentIdx].transform.position, 40f, 1, _leapDuration)
                .SetEase(Ease.OutQuad).SetId("TrayView: ghost Leap Jump")
                .WaitForCompletion();
        }

        // 5. Finalize Reveal
        // We check UniqueId because the item might have been matched/cleared 
        // while the Coroutine was yielding during the jumps.
        if (_slots[targetIdx].CurrentItem?.UId == data.UId)
        {
            _slots[targetIdx].RevealIcon(true);
        }

        Destroy(ghost.gameObject);
        onComplete?.Invoke();
    }
    private IEnumerator MatchGhostSequence(int startIdx, ItemData[] data, Action onComplete)
    {
        yield return null;

        Image[] ghosts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            ghosts[i] = Instantiate(ghostIconPrefab, transform.parent);
            AssetLoader.Instance.LoadIcon(data[i].IconName, (sprite) =>
            {
                ghosts[i].sprite = sprite;
            });
            ghosts[i].transform.position = _slots[startIdx + i].transform.position;
            _slots[startIdx + i].Clear();
        }

        Vector3 up = new Vector3(0, 40f, 0);
        // Center slot position
        Vector3 centerSlotPos = _slots[startIdx + 1].transform.position + up;

        Sequence mainSeq = DOTween.Sequence();
        mainSeq.SetId("TrayView: Matched Ghost");
        // --- STAGE 1: Lift all three slightly ---
        foreach (var g in ghosts)
        {
            mainSeq.Join(g.transform.DOMoveY(g.transform.position.y + up.y, 0.15f).SetEase(Ease.OutQuint));
        }

        // --- STAGE 2: Merge 1st and 3rd into 2nd ---
        // Ghosts[0] and Ghosts[2] move toward Ghosts[1] with elastic bounce
        mainSeq.Append(ghosts[0].transform.DOMove(centerSlotPos, 0.4f).SetEase(Ease.InOutElastic));
        mainSeq.Join(ghosts[2].transform.DOMove(centerSlotPos, 0.4f).SetEase(Ease.InOutElastic));

        // Optionally shrink them as they merge
        mainSeq.Join(ghosts[0].transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InQuad));
        mainSeq.Join(ghosts[2].transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InQuad));

        // --- STAGE 3: Impact shake when they collide ---
        mainSeq.InsertCallback(0.4f, () =>
        {
            transform.DOPunchPosition(Vector3.down * 15f, 0.3f, 15, 0.5f);
            // ParticleManager.Instance.Play("MergePoof", centerSlotPos);
            PoofParticle.transform.position = ghosts[1].transform.position;
            PoofParticle.Play();
            SoundController.instance.PlaySoundEffect("poof");

        });

        yield return mainSeq.WaitForCompletion();

        foreach (var g in ghosts) if (g) Destroy(g.gameObject);
        onComplete?.Invoke();
    }

}