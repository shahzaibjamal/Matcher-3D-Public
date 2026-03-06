using UnityEngine;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    private Button _button;
    void Awake()
    {
        _button = GetComponent<Button>();
    }
    void OnEnable()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(OnClick);
        }
    }

    void OnDisable()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnClick);
        }
    }

    private void OnClick()
    {
        SoundController.instance.PlaySoundEffect("btn");
    }
}