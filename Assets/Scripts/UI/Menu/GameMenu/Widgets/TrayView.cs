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
            Debug.LogError($"[TrayView] Target index {targetIdx} is out of bounds!");
            onComplete?.Invoke();
            return;
        }

        if (!isAdded)
        {
            _slots[targetIdx].Clear();
            _slots[targetIdx].PlayPoof();
            Scheduler.Instance.ExecuteAfterDelay(0.2f, () => onComplete?.Invoke());
            return;
        }

        SoundController.instance.PlaySoundEffect("pick");

        if (source.TryGetComponent<ClickableItem>(out var clickableItem))
        {
            clickableItem.Rigidbody.isKinematic = true;
            clickableItem.Collider.enabled = false;
        }

        SlotView targetSlot = _slots[targetIdx];
        targetSlot.SetItemDataOnly(data);

        // 1. Setup Positions
        Vector3 startPos = source.position;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 8f));

        // 2. Create the "Mid-Air" Control Point
        // This is the peak of the arc. We move it toward the center of the screen and UP (Y)
        Vector3 midPoint = Vector3.Lerp(startPos, worldTarget, 0.5f);
        midPoint.y += 2f; // High altitude for the "pop" toward camera
        midPoint.z += 5f;  // Subtle shift to make the curve "round"

        Vector3[] path = new Vector3[] { startPos, midPoint, worldTarget };

        // 3. The Sequence
        Sequence flightSeq = DOTween.Sequence();

        // RESET ROTATION: Just a clean snap to zero
        source.DORotate(Vector3.zero, _flightToTrayDuration, RotateMode.Fast);

        // THE MOVE: Follow the Bezier Path
        flightSeq.Append(source.DOPath(path, _flightToTrayDuration, PathType.CatmullRom)
                .SetEase(Ease.OutQuad)); // OutQuad makes it feel "thrown"

        // THE SCALE (The Waterfall Effect)
        // Scale UP as it nears the camera (midpoint), then scale small for the UI slot
        flightSeq.Join(source.DOScale(Vector3.one * 2.5f, _flightToTrayDuration * 0.4f).SetEase(Ease.OutCubic));
        flightSeq.Insert(_flightToTrayDuration * 0.5f, source.DOScale(Vector3.one * 0.3f, _flightToTrayDuration * 0.5f).SetEase(Ease.InSine));

        // SQUASH ON IMPACT: Purely visual juice
        flightSeq.Insert(_flightToTrayDuration * 0.9f, source.DOScaleY(0.1f, 0.1f));



        flightSeq.OnComplete(() =>
        {
            // UI Feedback
            targetSlot.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

            if (targetSlot.CurrentItem == data)
                targetSlot.RevealIcon(true);

            Destroy(source.gameObject);
            onComplete?.Invoke();
        });
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