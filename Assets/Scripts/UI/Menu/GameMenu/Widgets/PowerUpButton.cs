using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PowerUpButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private Image _icon;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Sprite _sprite;
    private int _amount;
    private PowerUpType _type;
    Color _dimmedColor = Color.gray;
    public void Initialize(PowerUpType powerUpType, int amount, Sprite sprite)
    {
        _button.onClick.AddListener(OnButtonClicked);

        _sprite = sprite;
        _amount = amount;
        _type = powerUpType;

        RefreshUI();

        GameEvents.OnPowerUpSuccessEvent += OnPowerUpSuccess;
        GameEvents.OnPowerUpEnableEvent += OnPowerUpEnable;
    }

    void OnDestroy()
    {
        _button.onClick.RemoveListener(OnButtonClicked);
        GameEvents.OnPowerUpSuccessEvent -= OnPowerUpSuccess;
        GameEvents.OnPowerUpEnableEvent -= OnPowerUpEnable;
    }

    public void RefreshUI()
    {
        _countText.text = _amount.ToString();
        _button.interactable = _amount > 0;
        _button.image.color = _amount > 0 ? Color.white : _dimmedColor;
        _icon.sprite = _sprite;
        // You could also change the icon based on the type here

        _canvasGroup.alpha = _amount < 0 ? 0 : 1;
    }

    private void OnButtonClicked()
    {
        if (_amount > 0)
        {
            // 2. Trigger the specific gameplay logic
            RefreshUI();
            TriggerPowerUpLogic();
        }
    }

    private void TriggerPowerUpLogic()
    {
        GameEvents.OnPowerUpEnableEvent?.Invoke(false);
        switch (_type)
        {
            case PowerUpType.Magnet:
                GameEvents.OnMagnetPowerupEvent?.Invoke();
                SoundController.Instance.PlaySoundEffect("magnet");
                break;
            case PowerUpType.Shake:
                GameEvents.OnShakePowerupEvent?.Invoke();
                break;
            case PowerUpType.Hint:
                SoundController.Instance.PlaySoundEffect("hint");
                GameEvents.OnHintPowerupEvent?.Invoke();
                break;
            case PowerUpType.Undo:
                GameEvents.OnUndoPowerupEvent?.Invoke(true);
                break;
        }
    }

    private void OnPowerUpSuccess(PowerUpType type, bool success)
    {
        if (_type == type)
        {
            if (success)
            {
                _amount--;
            }
            // 1. Deduct via delta logic
            GameEvents.OnPowerUpAmountChangeEvent?.Invoke(_type, -1);
            GameEvents.OnPowerUpEnableEvent?.Invoke(true);
            switch (_type)
            {
                case PowerUpType.Magnet:
                    break;
                case PowerUpType.Shake:
                    break;
                case PowerUpType.Hint:
                    break;
                case PowerUpType.Undo:
                    if (success)
                        SoundController.Instance.PlaySoundEffect("undo");
                    break;
            }
        }
    }
    private void OnPowerUpEnable(bool enable)
    {
        if (!enable)
        {
            _button.interactable = enable;
        }
        else
        {
            Scheduler.Instance.ExecuteAfterDelay(0.5f, () =>
            {
                RefreshUI();
            });
        }
    }
}