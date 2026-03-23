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

    public void Initialize(int slotCount)
    {
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
        if (_slots == null || targetIdx < 0 || targetIdx >= _slots.Length)
        {
            Debug.LogError($"[TrayView] Target index {targetIdx} is out of bounds!");
            onComplete?.Invoke();
            return;
        }


        // UNDO
        if (!isAdded)
        {
            _slots[targetIdx].Clear();
            _slots[targetIdx].PlayPoof();
            _slots[targetIdx].Bounce();
            Scheduler.Instance.ExecuteAfterDelay(0.2f, () => onComplete?.Invoke());
            return;
        }

        Vector3 rotationVector = default;
        if (source.TryGetComponent<ClickableItem>(out var clickableItem))
        {
            clickableItem.Rigidbody.isKinematic = true;
            clickableItem.SetCollidersEnabled(false);
            // rotationVector = clickableItem.Rotation;
            // rotationVector = new Vector3(clickableItem.Rotation.x, clickableItem.Rotation.y - 180f, clickableItem.Rotation.z);
            Quaternion originalRotation = Quaternion.Euler(clickableItem.Rotation);

            // 2. Create the "Axis Flip" rotation (90 degrees around X)
            Quaternion axisFlip = Quaternion.AngleAxis(Angle, Axis);

            // 3. Combine them. 
            // Note: Multiplying 'axisFlip' on the left applies it in World Space.
            Quaternion topDownRotation = axisFlip * originalRotation;

            // 4. Extract the final angle for your use elsewhere
            rotationVector = clickableItem.IsUpright ? clickableItem.Rotation + new Vector3(0, 180, 0) : topDownRotation.eulerAngles + new Vector3(0, 180, 0);
        }

        SlotView targetSlot = _slots[targetIdx];
        targetSlot.SetItemDataOnly(data);

        // 1. Setup Positions
        Vector3 startPos = source.position;
        // Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        // Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 8f));
        float boardDepth = Mathf.Abs(Camera.main.transform.position.z - source.position.z);
        float verticalDepth = Mathf.Abs(Camera.main.transform.position.y - source.position.y);
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, verticalDepth));
        // worldTarget.y = source.position.y;
        // Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, boardDepth));
        worldTarget = Vector3.Lerp(startPos, worldTarget, percentage);

        // 2. Create projectile arc control points (axis swapped, 4 points)
        Vector3 midPoint25 = Vector3.Lerp(startPos, worldTarget, 0.25f);
        Vector3 midPoint75 = Vector3.Lerp(startPos, worldTarget, 0.75f);

        // Arc height in z
        midPoint25.z += _arcZOffset;
        midPoint75.z += _arcZOffset;

        // Forward push in y (constant)
        float forwardY = Mathf.Max(startPos.y, worldTarget.y) + _forwardYOffset;
        midPoint25.y = forwardY;
        midPoint75.y = forwardY;
        worldTarget.y = forwardY;

        // Exaggerate curve toward slot in x
        float xDir = Mathf.Sign(worldTarget.x - startPos.x); // direction toward slot
        midPoint25.x += xDir * _xExaggeration;
        midPoint75.x += xDir * _xExaggeration;

        // Build path with 4 points
        Vector3[] path = new Vector3[] { startPos, midPoint25, midPoint75, worldTarget };

        // 3. Sequence
        Sequence flightSeq = DOTween.Sequence();

        // Reset rotation cleanly
        source.DORotate(rotationVector, 0.3f, RotateMode.Fast);

        // Projectile-style move along arc, ignoring rotation
        flightSeq.Append(source.DOPath(path, _flightToTrayDuration, PathType.CatmullRom, PathMode.Ignore)
                .SetEase(Ease.Linear)
                .SetOptions(false)); // prevents orientation snapping

        // Scale effects
        flightSeq.Join(source.DOScale(Vector3.one * 1.2f, _flightToTrayDuration * 0.4f).SetEase(Ease.OutCubic));
        flightSeq.Insert(_flightToTrayDuration * 0.5f, source.DOScale(Vector3.one * 0.3f, _flightToTrayDuration * 0.5f).SetEase(Ease.InSine));
        flightSeq.Insert(_flightToTrayDuration * 0.9f, source.DOScaleY(0.1f, 0.1f));

        flightSeq.OnComplete(() =>
        {
            targetSlot.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);

            if (targetSlot.CurrentItem == data)
                targetSlot.RevealIcon(true);

            AssetLoader.Instance.ReleaseInstance(source.gameObject);

            onComplete?.Invoke();

            if (!FTUEManager.Instance.IsSequenceCompleted("Undo") && GameManager.Instance.SaveData.CurrentLevelID == "level_02")
            {
                FTUEManager.Instance.PlayTutorial("Undo");
            }
            if (targetIdx == GameManager.SLOT_COUNT - 2)
            {
                _slots[_slots.Length - 1].FlashErrorColor(isSound: false);
            }
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