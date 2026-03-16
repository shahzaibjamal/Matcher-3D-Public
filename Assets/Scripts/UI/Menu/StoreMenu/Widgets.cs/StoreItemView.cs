using TMPro;
using TS.LocalizationSystem;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Image _itemIcon; // Change icon based on GOLD vs USD
    [SerializeField] private Image _currencyIcon; // Change icon based on GOLD vs USD
    [SerializeField] private Button _buyButton;
    [SerializeField] private RectTransform _rewardsContainer;
    [SerializeField] private GameObject _storeRewardViewPreafb;
    [SerializeField] private RewardIconMapper _rewardIconMapper;

    private StoreItemUIState _state;
    public void Awake()
    {
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    void OnDestroy()
    {
        _buyButton.onClick.RemoveAllListeners();
    }
    public void Setup(StoreItemUIState state)
    {
        _state = state;
        _titleText.text = _state.Name;
        _quantityText.text = "+" + _state.DisplayQuantity;
        _priceText.text = _state.CurrencyType == StoreCurrencyType.Gold ?
                        _state.DisplayCost.ToString() :
                        $"${_state.DisplayCost:F2}";
        _currencyIcon.gameObject.SetActive(_state.CurrencyType == StoreCurrencyType.Gold);
        LoadRewards();
        RefreshUI();
    }

    private void OnBuyButtonClicked()
    {
        _buyButton.interactable = false;
        PurchaseManager.Instance.PurchaseItem(_state.ItemID, OnPurchaseCallback);
    }

    private void OnPurchaseCallback(bool success)
    {
        RefreshUI();
        if (!success)
        {
            MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(
                        Menus.Type.GenericPopup,
                        new GenericPopupMenuData(
                            LocalizationKeys.store,
                            LocalizationKeys.iap_error_message,
                            LocalizationKeys.cancel
                        )
                    );
        }
    }


    private void RefreshUI()
    {
        if (_state.CurrencyType == StoreCurrencyType.Gold)
        {
            _buyButton.interactable = GameManager.Instance.SaveData.Inventory.Gold >= _state.DisplayCost;
        }
        else
        {
            _buyButton.interactable = true;

        }
    }
    public void LoadRewards()
    {

        for (int i = 0; i < _state.ProcessedRewards.Count; i++)
        {
            var rewardData = _state.ProcessedRewards[i];
            if (i == 0)
            {
                _quantityText.text = "+" + _state.DisplayQuantity;
                _priceText.text = _state.CurrencyType == StoreCurrencyType.Gold ?
                                _state.DisplayCost.ToString() :
                                $"${_state.DisplayCost:F2}";
            }
            else
            {
                GameObject go = Instantiate(_storeRewardViewPreafb, _rewardsContainer);
                var storeRewardView = go.GetComponent<StoreRewardView>();
                storeRewardView.Initialize(_rewardIconMapper.GetIcon(rewardData.RewardType), rewardData.Amount);
            }
        }
    }
}