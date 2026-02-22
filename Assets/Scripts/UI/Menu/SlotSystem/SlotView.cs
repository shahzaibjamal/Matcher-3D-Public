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

    public void SetItemDataOnly(ItemData itemData)
    {
        CurrentItem = itemData;
        if (itemData != null)
        {
            icon.sprite = itemData.UISprite;
        }
        // Keep it hidden; the TrayView will call RevealIcon after the animation lands
        icon.enabled = false;
    }

    public void RevealIcon()
    {
        if (CurrentItem == null) return;

        icon.transform.DOKill(); // Stop any pending match-scales
        icon.enabled = true;
        icon.sprite = CurrentItem.UISprite;

        // Ensure we are at full scale and visible
        icon.transform.localScale = Vector3.one;
        icon.canvasRenderer.SetAlpha(1f);

        // Add the landing juice
        icon.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
    }
    public void Clear()
    {
        // Important: Only clear if we aren't currently "Reserved" by a landing flight
        // But for now, let's keep it simple:
        CurrentItem = null;
        icon.enabled = false;
        icon.transform.DOKill();
    }
}