using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpinRewardView : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text amountText;

    public void SetData(Sprite sprite, int amount)
    {
        iconImage.sprite = sprite;
        amountText.text = amount.ToString();
    }
}