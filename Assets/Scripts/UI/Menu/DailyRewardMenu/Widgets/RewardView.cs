using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardView : MonoBehaviour
{
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private TMP_Text _rewardAmount;

    public void Initialize(Sprite rewardIconSpreite, int amount)
    {
        _rewardIcon.sprite = rewardIconSpreite;
        _rewardAmount.text = amount.ToString();
    }
}