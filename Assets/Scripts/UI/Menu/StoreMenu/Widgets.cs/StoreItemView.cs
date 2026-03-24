using System;
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
    [SerializeField] private IAPIconMapper _iapIconMapper;

    public StoreItemUIState State { private set; get; }
    private Action<bool> _onPurchaseCallback;

    public void Awake()
    {
        _buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    void OnDestroy()
    {
        _buyButton.onClick.RemoveAllListeners();
    }
    public void Setup(StoreItemUIState state, Action<bool> onPurchaseCallback)
    {
        State = state;
        _onPurchaseCallback = onPurchaseCallback;
        _titleText.text = State.Name;
        _quantityText.text = "+" + State.DisplayQuantity;
        _priceText.text = State.CurrencyType == StoreCurrencyType.Gold ?
                        State.DisplayCost.ToString() :
                        $"${State.DisplayCost:F2}";
        _currencyIcon.gameObject.SetActive(State.CurrencyType == StoreCurrencyType.Gold);
        LoadRewards();
        RefreshUI();
    }

    private void OnBuyButtonClicked()
    {
        _buyButton.interactable = false;
        PurchaseManager.Instance.PurchaseItem(State.ItemID, OnPurchaseCallback);
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
        else
        {
            _onPurchaseCallback?.Invoke(success);
        }
    }

    public void RefreshUI()
    {
        if (State.CurrencyType == StoreCurrencyType.Gold)
        {
            _buyButton.interactable = GameManager.Instance.SaveData.Inventory.Gold >= State.DisplayCost;
        }
        else
        {
            _buyButton.interactable = true;

        }
    }
    public void LoadRewards()
    {
        while (_rewardsContainer.childCount > 0)
        {
            DestroyImmediate(_rewardsContainer.GetChild(0).gameObject);
        }

        for (int i = 0; i < State.ProcessedRewards.Count; i++)
        {
            var rewardData = State.ProcessedRewards[i];
            if (i == 0)
            {
                _quantityText.text = "+" + State.DisplayQuantity;
                _priceText.text = State.CurrencyType == StoreCurrencyType.Gold ?
                                State.DisplayCost.ToString() :
                                $"${State.DisplayCost:F2}";
                _itemIcon.sprite = State.CurrencyType == StoreCurrencyType.USD ?
                _iapIconMapper.GetIcon(State.ItemID) :
                _rewardIconMapper.GetIcon(rewardData.RewardType);
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