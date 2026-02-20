using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;

    public int CurrentCount { get; private set; }
    private ItemData itemData;

    public void SetItem(ItemData item, int startCount)
    {
        itemData = item;
        icon.sprite = item.UISprite;
        CurrentCount = startCount;
        UpdateUI();
    }

    public void UpdateCount(int delta)
    {
        CurrentCount += delta;
        UpdateUI();
    }

    private void UpdateUI()
    {
        countText.text = CurrentCount.ToString();
    }
}
