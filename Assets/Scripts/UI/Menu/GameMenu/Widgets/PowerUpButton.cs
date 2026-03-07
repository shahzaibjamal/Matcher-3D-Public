using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PowerUpButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Image _icon;

    private Sprite _sprite;
    private int _amount;
    private PowerUpType _type;
    public void Initialize(PowerUpType powerUpType, int amount, Sprite sprite)
    {
        _button.onClick.AddListener(OnButtonClicked);

        _sprite = sprite;
        _amount = amount;
        _type = powerUpType;

        RefreshUI();

        GameEvents.OnPowerUpSuccessEvent += OnPowerUpSuccess;
    }

    void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
        GameEvents.OnPowerUpSuccessEvent -= OnPowerUpSuccess;
    }

    public void RefreshUI()
    {
        _countText.text = _amount.ToString();
        _button.interactable = _amount > 0;
        _icon.sprite = _sprite;
        // You could also change the icon based on the type here
    }

    private void OnButtonClicked()
    {
        if (_amount > 0)
        {
            // 2. Trigger the specific gameplay logic
            TriggerPowerUpLogic();

            RefreshUI();
        }
    }

    private void TriggerPowerUpLogic()
    {
        switch (_type)
        {
            case PowerUpType.Magnet:
                GameEvents.OnMagnetPowerupEvent?.Invoke();
                break;
            case PowerUpType.Shake:
                GameEvents.OnShakePowerupEvent?.Invoke();
                break;
            case PowerUpType.Hint:
                GameEvents.OnHintPowerupEvent?.Invoke();
                break;
            case PowerUpType.Undo:
                GameEvents.OnUndoPowerupEvent?.Invoke(true);
                break;
        }
        _button.interactable = false;
    }

    private void OnPowerUpSuccess(PowerUpType type)
    {
        _button.interactable = true;

        if (_type == type)
        {
            _amount--;
            // 1. Deduct via delta logic
            GameEvents.OnPowerUpAmountChangeEvent?.Invoke(_type, -1);
            RefreshUI();
            switch (_type)
            {
                case PowerUpType.Magnet:
                    SoundController.instance.PlaySoundEffect("magnet");
                    break;
                case PowerUpType.Shake:
                    break;
                case PowerUpType.Hint:
                    SoundController.instance.PlaySoundEffect("hint");
                    break;
                case PowerUpType.Undo:
                    SoundController.instance.PlaySoundEffect("undo");
                    break;
            }
        }

    }

}