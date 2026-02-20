using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;

    public ItemData CurrentItem { get; private set; }
    public bool IsOccupied => CurrentItem != null;
    public Transform IconTransform => icon.transform;

    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        icon.sprite = itemData != null ? itemData.UISprite : null;
        icon.enabled = false; // Always start hidden for the animation to take over
    }

    public void RevealIcon()
    {
        if (CurrentItem != null)
        {
            icon.enabled = true;
            icon.transform.localScale = Vector3.one;
            icon.transform.localPosition = Vector3.zero;
        }
    }
    public void Clear()
    {
        CurrentItem = null;
        icon.sprite = null;
        icon.enabled = false;
    }

    public void ClearDataOnly()
    {
        CurrentItem = null;
        // Keep icon visible for a split second so the Ghost can "copy" its position
    }
}