using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;

public class TrayView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private SlotView slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private Image ghostIconPrefab;

    [Header("Animation Settings")]
    [SerializeField] private float flightDuration = 0.5f;
    [SerializeField] private float cameraZOffset = 10.0f;
    [SerializeField] private float mergeUpHeight = 100f;
    [SerializeField] private ParticleSystem matchParticlePrefab;

    private SlotView[] _slots;

    public void Initialize(int slotCount)
    {
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
        // Listen for direct commands from SlotManager
        GameEvents.OnRequestFlight += HandleFlightRequest;
        GameEvents.OnRequestLeap += HandleLeapRequest;
        GameEvents.OnRequestMatchResolve += HandleMatchRequest;
    }

    private void OnDisable()
    {
        GameEvents.OnRequestFlight -= HandleFlightRequest;
        GameEvents.OnRequestLeap -= HandleLeapRequest;
        GameEvents.OnRequestMatchResolve -= HandleMatchRequest;
    }

    // --- 1. FLY FROM WORLD TO TRAY ---
    private void HandleFlightRequest(ItemData data, int targetIdx, Transform source, Action onComplete)
    {
        if (source.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        if (source.TryGetComponent<Collider>(out var col)) col.enabled = false;

        SlotView targetSlot = _slots[targetIdx];
        targetSlot.SetItemDataOnly(data);

        // 1. Prepare Coordinates
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);
        // Depth (Z) should be roughly 5-10 units in front of the camera
        Vector3 worldTarget = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 8f));

        // 2. Multi-Stage Animation Sequence
        Sequence flightSeq = DOTween.Sequence();

        // STAGE A: Pick Up (Move up slightly and rotate)
        flightSeq.Append(source.DOMove(source.position + Vector3.up * 2f, 0.2f).SetEase(Ease.OutQuad));
        flightSeq.Join(source.DORotate(Vector3.zero, 0.2f, RotateMode.FastBeyond360));
        flightSeq.Join(source.DOScale(source.localScale * 1.2f, 0.2f));

        // STAGE B: The Toss (Move to UI Slot)
        flightSeq.Append(source.DOMove(worldTarget, 0.5f).SetEase(Ease.InBack));
        flightSeq.Join(source.DOScale(Vector3.one * 0.3f, 0.5f)); // Shrink to fit UI

        // Ensure it renders on top of everything
        if (source.TryGetComponent<Renderer>(out var rend))
        {
            rend.sortingLayerName = "UI";
            rend.sortingOrder = 32767;
        }

        flightSeq.OnComplete(() =>
        {
            targetSlot.RevealIcon();
            Destroy(source.gameObject);

            // Tell SlotManager we are done
            onComplete?.Invoke();
        });
    }
    private void HandleLeapRequest(int from, int to, ItemData data, Action onComplete)
    {
        // Source is currently visible, Target is currently empty.
        Image ghost = Instantiate(ghostIconPrefab, transform.parent);
        ghost.sprite = data.UISprite;
        ghost.transform.position = _slots[from].IconTransform.position;

        // Visual handoff
        _slots[to].SetItemDataOnly(data); // Target gets data but stays hidden
        _slots[from].Clear();            // Source wiped immediately

        ghost.transform.DOJump(_slots[to].transform.position, 60f, 1, 0.35f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                _slots[to].RevealIcon();
                Destroy(ghost.gameObject);

                // NOTIFY CONDUCTOR: Leap finished, move to next item in queue
                onComplete?.Invoke();
            });
    }

    // --- 3. RESOLVE MATCH-3 ---
    private void HandleMatchRequest(int startIndex, Action onComplete)
    {
        StartCoroutine(MatchSequence(startIndex, onComplete));
    }

    private IEnumerator MatchSequence(int startIdx, Action onComplete)
    {
        // The slots at startIdx, +1, and +2 are the ones to merge
        Vector3 centerPoint = _slots[startIdx + 1].transform.position;
        Vector3 peakPoint = centerPoint + Vector3.up * mergeUpHeight;

        Sequence mergeSeq = DOTween.Sequence();

        for (int i = 0; i < 3; i++)
        {
            Transform icon = _slots[startIdx + i].IconTransform;
            mergeSeq.Join(icon.DOMove(peakPoint, 0.3f).SetEase(Ease.InBack));
            mergeSeq.Join(icon.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack));
        }

        yield return mergeSeq.WaitForCompletion();

        if (matchParticlePrefab)
            Instantiate(matchParticlePrefab, peakPoint, Quaternion.identity);

        // Wipe the 3 matched slots visually
        for (int i = 0; i < 3; i++) _slots[startIdx + i].Clear();

        // NOTIFY CONDUCTOR: Match resolved, safe to start compaction leaps
        onComplete?.Invoke();
    }
}