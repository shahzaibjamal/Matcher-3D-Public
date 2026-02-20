using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance;

    [Header("Base Settings")]
    [SerializeField] private SlotUI slotPrefab;
    [SerializeField] private Transform slotParent;
    [SerializeField] private int slotCount = 7;
    [SerializeField] private Image ghostIconPrefab;

    [Header("Juice & Camera")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float cameraZOffset = 12.0f;
    [SerializeField] private ParticleSystem matchParticlePrefab;

    private SlotUI[] slots;
    private bool isProcessingMatch = false;
    private int itemsInFlight = 0;

    void Awake() => Instance = this;

    public void InitializeLevel(LevelData level) => SpawnSlots();

    private void SpawnSlots()
    {
        foreach (Transform child in slotParent) Destroy(child.gameObject);
        slots = new SlotUI[slotCount];
        for (int i = 0; i < slotCount; i++) slots[i] = Instantiate(slotPrefab, slotParent);
    }

    public void AnimateItemToSlot(ItemData itemData, Transform sourceTransform)
    {
        int targetIndex = GetInsertionIndex(itemData);
        if (targetIndex >= slots.Length) { CheckGameOver(); return; }

        itemsInFlight++;
        ShiftDataWithLeap(targetIndex, itemData);

        // 1. 3D Setup & Rotation Fix
        if (sourceTransform.TryGetComponent<Renderer>(out var rend))
        {
            rend.sortingLayerName = "UI";
            rend.sortingOrder = 32767;
        }

        Vector3 targetWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(
            slots[targetIndex].transform.position.x,
            slots[targetIndex].transform.position.y,
            cameraZOffset
        ));

        // 2. The Flight Sequence (Including Orientation Tween)
        Sequence seq = DOTween.Sequence();
        Vector3 peak = Vector3.Lerp(sourceTransform.position, targetWorldPos, 0.5f) + Vector3.up * 3f;

        // Move to peak and start rotating to Zero
        seq.Append(sourceTransform.DOMove(peak, animationDuration * 0.4f).SetEase(Ease.OutQuad));
        seq.Join(sourceTransform.DORotate(Vector3.zero, animationDuration * 0.6f)); // ROTATION RESTORED

        // Move to tray and shrink
        seq.Append(sourceTransform.DOMove(targetWorldPos, animationDuration * 0.6f).SetEase(Ease.InBack));
        seq.Join(sourceTransform.DOScale(Vector3.one * 0.3f, animationDuration * 0.6f));

        seq.OnComplete(() =>
        {
            itemsInFlight--;

            // IMPORTANT: We find the FIRST slot that is occupied by this data but NOT yet enabled.
            SlotUI actualSlot = FindFirstHiddenSlotOfItem(itemData);
            if (actualSlot != null) actualSlot.RevealIcon();

            Destroy(sourceTransform.gameObject);
            if (!isProcessingMatch) StartCoroutine(MatchSequenceCheck());
        });
    }

    private void CreateGhostLeap(SlotUI fromSlot, SlotUI toSlot, ItemData item)
    {
        if (ghostIconPrefab == null) return;

        Image ghost = Instantiate(ghostIconPrefab, transform.parent);
        ghost.sprite = item.UISprite;
        ghost.transform.position = fromSlot.IconTransform.position;

        // Immediately hide the source so the ghost is the only thing visible
        fromSlot.Clear();

        float duration = 0.3f;
        // Parabolic Leap
        ghost.transform.DOJump(toSlot.transform.position, 60f, 1, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Reveal the destination
                if (toSlot != null) toSlot.RevealIcon();
                Destroy(ghost.gameObject);
            });
    }

    // NEW HELPER: Finds the correct slot to reveal even if multiple of the same item exist
    private SlotUI FindFirstHiddenSlotOfItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsOccupied && slots[i].CurrentItem == item)
            {
                // If the icon is not yet enabled, this is our landing target!
                if (!slots[i].IconTransform.GetComponent<Image>().enabled)
                    return slots[i];
            }
        }
        return null;
    }
    private void ShiftDataWithLeap(int targetIndex, ItemData data)
    {
        // Shift from right to left
        for (int i = slots.Length - 1; i > targetIndex; i--)
        {
            if (slots[i - 1].IsOccupied)
            {
                ItemData movingItem = slots[i - 1].CurrentItem;

                // This method now handles the "Baton Pass" correctly
                CreateGhostLeap(slots[i - 1], slots[i], movingItem);

                slots[i].SetItemDataOnly(movingItem);
                // We don't need to ClearDataOnly here because CreateGhostLeap calls Clear()
            }
        }
        // Set data for the incoming 3D object (it stays hidden until landing)
        slots[targetIndex].SetItemDataOnly(data);
    }
    private IEnumerator MatchSequenceCheck()
    {
        if (isProcessingMatch || itemsInFlight > 0) yield break;

        for (int i = 0; i <= slots.Length - 3; i++)
        {
            if (!slots[i].IsOccupied) continue;

            string id = slots[i].CurrentItem.UID;
            if (slots[i + 1].IsOccupied && slots[i + 1].CurrentItem.UID == id &&
                slots[i + 2].IsOccupied && slots[i + 2].CurrentItem.UID == id)
            {
                isProcessingMatch = true;
                yield return new WaitForSeconds(0.1f);
                yield return StartCoroutine(PlayMergeAnimation(i));
                ResolveMatch(i);
                isProcessingMatch = false;

                // Chain reaction check
                StartCoroutine(MatchSequenceCheck());
                yield break;
            }
        }
        CheckGameOver();
    }

    private IEnumerator PlayMergeAnimation(int startIdx)
    {
        // The middle slot (startIdx + 1) is the "Black Hole" center
        Vector3 centerWorldPos = slots[startIdx + 1].transform.position;
        Sequence mergeSeq = DOTween.Sequence();
        float upValue = 60f;
        Vector3 highMergePoint = centerWorldPos + (slots[startIdx + 1].transform.up * upValue);

        // 1. THE "CHEER" - Jump up together in Local Space
        for (int i = 0; i < 3; i++)
        {
            Transform icon = slots[startIdx + i].IconTransform;
            // Kill any previous leaping tweens to take control
            icon.DOKill();

            // Jump up relative to their current position
            Vector3 peakPos = icon.position + (icon.up * upValue);
            mergeSeq.Join(icon.DOMove(peakPos, 0.25f).SetEase(Ease.OutQuad));
        }

        // Small "hang time" at the top
        mergeSeq.AppendInterval(0.05f);

        // 2. THE MERGE - Move to the World Position of the center slot
        for (int i = 0; i < 3; i++)
        {
            Transform icon = slots[startIdx + i].IconTransform;

            // Use DOMove for World Space targeting
            mergeSeq.Join(icon.DOMove(highMergePoint, 0.25f).SetEase(Ease.InBack));
            mergeSeq.Join(icon.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));
        }

        yield return mergeSeq.WaitForCompletion();

        // 3. THE POOF
        if (matchParticlePrefab)
        {
            Instantiate(matchParticlePrefab, centerWorldPos, Quaternion.identity);
        }

        // 4. RESET - Hard reset for future reuse
        for (int i = 0; i < 3; i++)
        {
            slots[startIdx + i].IconTransform.localPosition = Vector3.zero;
            slots[startIdx + i].IconTransform.localScale = Vector3.one;
            // Clear() in ResolveMatch will handle the icon.enabled = false
        }
    }
    private void ResolveMatch(int startIdx)
    {
        // 1. Logical Wipe
        for (int i = 0; i < 3; i++)
        {
            slots[startIdx + i].Clear();
        }

        // 2. Compact Left with Leaps
        // We start from the index to the right of the matched group
        for (int i = startIdx; i < slots.Length - 3; i++)
        {
            if (slots[i + 3].IsOccupied)
            {
                ItemData movingItem = slots[i + 3].CurrentItem;

                // Visual Leap from i+3 to i
                CreateGhostLeap(slots[i + 3], slots[i], movingItem);

                // Data Handover
                slots[i].SetItemDataOnly(movingItem);
                slots[i + 3].Clear();
            }
        }
    }
    private int GetInsertionIndex(ItemData newItem)
    {
        // Find last existing twin
        for (int i = slots.Length - 1; i >= 0; i--)
            if (slots[i].IsOccupied && slots[i].CurrentItem.UID == newItem.UID) return i + 1;

        // Else find first empty
        for (int i = 0; i < slots.Length; i++)
            if (!slots[i].IsOccupied) return i;

        return slots.Length;
    }

    private int FindCurrentSlotOfItem(ItemData item)
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].IsOccupied && slots[i].CurrentItem == item) return i;
        return -1;
    }

    private void CheckGameOver()
    {
        if (itemsInFlight > 0 || isProcessingMatch) return;
        foreach (var s in slots) if (!s.IsOccupied) return;
        Debug.Log("Game Over!");
    }
}