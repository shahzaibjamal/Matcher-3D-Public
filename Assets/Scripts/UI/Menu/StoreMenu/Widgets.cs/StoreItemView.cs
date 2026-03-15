using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreItemView : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _quantityText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private Image _currencyIcon; // Change icon based on GOLD vs USD
    [SerializeField] private Button _buyButton;

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
    }

    private void OnBuyButtonClicked()
    {
        PurchaseManager.Instance.PurchaseItem(_state.ItemID);
    }
}