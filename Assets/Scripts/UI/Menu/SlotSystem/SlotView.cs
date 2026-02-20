using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlotView : MonoBehaviour
{
    [SerializeField] private Image icon;

    public ItemData CurrentItem { get; private set; }
    public bool IsOccupied => CurrentItem != null;
    public Transform IconTransform => icon.transform;
    public bool IsImageEnabled => icon.enabled;
    public int Index => _index;
    private int _index = -1;

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        icon.sprite = itemData != null ? itemData.UISprite : null;
        icon.enabled = false; // Always start hidden for the animation to take over
    }

    public void HideIcon()
    {
        icon.enabled = false;

    }
    public void RevealIcon()
    {
        if (CurrentItem == null) return; // Safety: Don't reveal if empty
        icon.sprite = CurrentItem.UISprite;
        icon.enabled = true;
        // Add a small scale punch for "juice"
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }
    public void ClearIfMatch(ItemData itemToClear)
    {
        // If the logical item has already changed (because a leap filled this slot),
        // DO NOT clear it!
        if (CurrentItem == itemToClear)
        {
            Clear();
        }
    }

    public void Clear()
    {
        CurrentItem = null;
        icon.sprite = null;
        icon.enabled = false;
        // Reset transforms in case a merge animation left them scaled/moved
        icon.transform.localScale = Vector3.one;
        icon.transform.localPosition = Vector3.zero;
    }

    public void ClearDataOnly()
    {
        CurrentItem = null;
        // Keep icon visible for a split second so the Ghost can "copy" its position
    }
}