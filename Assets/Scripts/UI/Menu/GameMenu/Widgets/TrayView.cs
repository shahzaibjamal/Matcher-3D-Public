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
    [SerializeField] private float _flightToTrayDuration = 0.5f;
    [SerializeField] private float _leapDuration = 0.5f;

    private SlotView[] _slots;
    private Vector3[] _slotWorldTargets;
    private int _groundLayerMask;

    public void Initialize(int slotCount)
    {
        _groundLayerMask = LayerMask.GetMask("Ground");
        // Kill existing slots if any (Cleanup)
        foreach (Transform child in slotParent)
        {
            AssetLoader.Instance.ReleaseInstance(child.gameObject);
        }

        _slots = new SlotView[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            _slots[i] = Instantiate(slotPrefab, slotParent);
            _slots[i].SetIndex(i);
        }
        Scheduler.Instance.ExecuteAfterDelay(0.1f, InitializeSlotTargets);
    }
    private void OnEnable()
    {
        GameEvents.OnItemAddedToSlotEvent += HandleItemAddedToSlot;
        GameEvents.OnRequestSteppedLeapEvent += HandleSteppedLeapRequest;
        GameEvents.OnRequestMatchResolveEvent += HandleMatchResolveRequest;
        GameEvents.OnUndoInvalidEvent += HandleUndoInvalid;
    }

    private void OnDisable()
    {
        GameEvents.OnItemAddedToSlotEvent -= HandleItemAddedToSlot;
        GameEvents.OnRequestSteppedLeapEvent -= HandleSteppedLeapRequest;
        GameEvents.OnRequestMatchResolveEvent -= HandleMatchResolveRequest;
        GameEvents.OnUndoInvalidEvent -= HandleUndoInvalid;
    }

    private void InitializeSlotTargets()
    {
        _slotWorldTargets = new Vector3[_slots.Length];
        Camera mainCam = Camera.main;

        for (int i = 0; i < _slots.Length; i++)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, _slots[i].transform.position);
            Ray ray = mainCam.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _groundLayerMask))
            {
                _slotWorldTargets[i] = hit.point;
            }
            else
            {
                // Fallback to average depth if ray misses
                _slotWorldTargets[i] = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            }
        }
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
    [SerializeField] private float _arcZOffset = 1f;     // arc height in z
    [SerializeField] private float _forwardYOffset = 1f; // push forward toward camera (+y)
    [SerializeField] private float _xExaggeration = 2f;  // exaggeration toward slot in x

    [Range(0, 1)]
    [SerializeField] private float percentage = 0.9f;  // exaggeration toward slot in x
    public Vector3 Axis = Vector3.right;
    public float Angle = 90;
    private void HandleItemAddedToSlot(ItemData data, int targetIdx, Transform source, bool isAdded, Action onComplete)
    {
        if (!IsValidSlot(targetIdx))
        {
            onComplete?.Invoke();
            return;
        }

        if (!isAdded)
        {
            PerformUndoVisuals(targetIdx, onComplete);
            return;
        }

        // 1. Setup Data & Rotation
        _slots[targetIdx].SetItemDataOnly(data);
        Vector3 targetRotation = CalculateLandingRotation(source);
        Vector3 targetWorldPos = _slotWorldTargets[targetIdx];

        // 2. Execute the "Toss" Animation
        AnimateItemToTray(source, targetWorldPos, targetRotation, () =>
        {
            FinalizeLanding(targetIdx, data, source);
            onComplete?.Invoke();
        });
    }
    private bool IsValidSlot(int idx) => _slots != null && idx >= 0 && idx < _slots.Length;
    private void PerformUndoVisuals(int targetIdx, Action onComplete)
    {
        SlotView slot = _slots[targetIdx];

        // 1. Visual feedback on the slot itself
        slot.Clear(); // Clears the icon/data
        slot.PlayPoof(); // Your particle/vfx for removal

        // 2. Physical feedback
        slot.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0.1f), 0.2f, 5, 0.5f);

        // 3. Delay the callback slightly so the "poof" can be seen
        Scheduler.Instance.ExecuteAfterDelay(0.2f, () => onComplete?.Invoke());
    }
    private Vector3 CalculateLandingRotation(Transform source)
    {
        if (!source.TryGetComponent<ClickableItem>(out var clickableItem))
            return source.eulerAngles;

        clickableItem.Rigidbody.isKinematic = true;
        clickableItem.SetCollidersEnabled(false);

        Quaternion originalRotation = Quaternion.Euler(clickableItem.Rotation);
        Quaternion axisFlip = Quaternion.AngleAxis(Angle, Axis);
        Quaternion topDownRotation = axisFlip * originalRotation;

        return clickableItem.IsUpright ?
            clickableItem.Rotation + new Vector3(0, 180, 0) :
            topDownRotation.eulerAngles + new Vector3(0, 180, 0);
    }
    private void AnimateItemToTray(Transform item, Vector3 targetPos, Vector3 targetRot, Action onDone)
    {
        Vector3 startPos = item.position;

        // Adjusting peak height for a better "Toss"
        float peakHeight = Mathf.Max(startPos.y, targetPos.y) + _forwardYOffset;

        // Control points for the CatmullRom path
        Vector3 mid1 = Vector3.Lerp(startPos, targetPos, 0.3f);
        Vector3 mid2 = Vector3.Lerp(startPos, targetPos, 0.7f);

        mid1.y = peakHeight;
        mid2.y = peakHeight;
        mid1.z += _arcZOffset;
        mid2.z += _arcZOffset;

        Sequence s = DOTween.Sequence();

        // Reset rotation cleanly
        item.DORotate(targetRot, 0.3f, RotateMode.Fast);

        // Projectile-style move along arc, ignoring rotation
        s.Append(item.DOPath(new Vector3[] { startPos, mid1, mid2, targetPos }, _flightToTrayDuration, PathType.CatmullRom, PathMode.Ignore)
                .SetEase(Ease.Linear)
                .SetOptions(false)); // prevents orientation snapping

        // Scale effects
        s.Join(item.DOScale(Vector3.one * 1.2f, _flightToTrayDuration * 0.4f).SetEase(Ease.OutCubic));
        s.Insert(_flightToTrayDuration * 0.5f, item.DOScale(Vector3.one * 0.4f, _flightToTrayDuration * 0.5f).SetEase(Ease.InSine));
        s.Insert(_flightToTrayDuration * 0.9f, item.DOScaleY(0.1f, 0.1f));

        s.OnComplete(() => onDone?.Invoke());
    }
    private void FinalizeLanding(int targetIdx, ItemData data, Transform source)
    {
        SlotView targetSlot = _slots[targetIdx];
        targetSlot.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

        if (targetSlot.CurrentItem == data)
            targetSlot.RevealIcon(true);

        AssetLoader.Instance.ReleaseInstance(source.gameObject);
        CheckFTUEConditions(targetIdx);
    }

    private void CheckFTUEConditions(int targetIdx)
    {
        // 1. Handle the Undo Tutorial trigger for Level 2
        bool isUndoTutorialDone = FTUEManager.Instance.IsSequenceCompleted("Undo");
        bool isLevelTwo = GameManager.Instance.SaveData.CurrentLevelID == "level_02";

        if (!isUndoTutorialDone && isLevelTwo)
        {
            FTUEManager.Instance.PlayTutorial("Undo");
        }
        if (targetIdx == GameManager.SLOT_COUNT - 2)
        {
            _slots[_slots.Length - 1].FlashErrorColor(isSound: false);
        }
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

        ValidateSlots();

        onComplete?.Invoke();
    }

    /// <summary>
    /// Forces the visual state of all slots to match their logical data.
    /// Useful for cleaning up after complex movement sequences.
    /// </summary>
    public void ValidateSlots()
    {
        if (_slots == null) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            SlotView slot = _slots[i];

            // If there is no logical data, the slot should be visually empty
            if (slot.CurrentItem == null)
            {
                slot.Clear();
            }
            else
            {
                // If there is data, ensure the icon is active and showing the right sprite
                // Using true here forces the icon to appear immediately without a fade if preferred
                slot.RevealIcon(true);
            }
        }
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
            SoundController.Instance.PlaySoundEffect("snap");
        });

        yield return mainSeq.WaitForCompletion();

        foreach (var g in ghosts) if (g) Destroy(g.gameObject);
        ValidateSlots();
        onComplete?.Invoke();
    }

    private void HandleUndoInvalid()
    {
        if (_slots[0] != null)
        {
            _slots[0].FlashErrorColor();
        }
    }
}