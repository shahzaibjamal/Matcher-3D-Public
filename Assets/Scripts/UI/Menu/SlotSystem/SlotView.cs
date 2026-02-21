using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SlotView : MonoBehaviour
{
    [SerializeField] private Image icon;

    public ItemData CurrentItem { get; private set; }
    public bool IsImageEnabled => icon.enabled;
    public Transform IconTransform => icon.transform;

    private int _index = -1;

    public void SetIndex(int index) => _index = index;

    // Sets the data and prepares the sprite, but keeps it invisible
    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        if (itemData != null)
        {
            icon.sprite = itemData.UISprite;
        }
        icon.enabled = false;
    }

    public void RevealIcon()
    {
        if (CurrentItem == null) return;

        icon.sprite = CurrentItem.UISprite;
        icon.enabled = true; // FORCE ENABLE

        // Ensure Alpha is 1 and Scale is 1
        icon.canvasRenderer.SetAlpha(1f);
        icon.transform.localScale = Vector3.one;
        icon.transform.localPosition = Vector3.zero;

        // Visual feedback
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }
    public void Clear()
    {
        CurrentItem = null;
        icon.sprite = null;
        icon.enabled = false;
        icon.transform.localScale = Vector3.one;
        icon.transform.localPosition = Vector3.zero;
    }
}