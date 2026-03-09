using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LanguageView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private GameObject _active;
    [SerializeField] private TMP_Text _languageName;

    private Action<string> _onLanguageSelect;

    public string Name { get; private set; }

    void OnEnable()
    {
        _button.onClick.AddListener(OnClick);
    }

    void OnDisable()
    {
        _button.onClick.RemoveListener(OnClick);
    }

    public void Init(string languageName, Action<string> onLanguageSelect)
    {
        Name = languageName;
        _onLanguageSelect = onLanguageSelect;
        _languageName.text = languageName;
    }

    private void OnClick()
    {
        _onLanguageSelect?.Invoke(Name);
        SoundController.Instance.PlaySoundEffect("menu_click");
    }

    public void SetSelected(bool isSelected)
    {
        _active.SetActive(isSelected);
    }
}