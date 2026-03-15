using TMPro;
using TS.LocalizationSystem;
using UnityEngine;
using UnityEngine.UI;

public class StoreRewardView : MonoBehaviour
{
    [SerializeField] private Image _rewardIcon;
    [SerializeField] private TMP_Text _rewardAmount;

    public void Initialize(Sprite rewardIconSpreite, int amount)
    {
        _rewardIcon.sprite = rewardIconSpreite;
        _rewardAmount.text = string.Format(LocaleManager.Localize(LocalizationKeys.reward_amount), amount);
    }
}