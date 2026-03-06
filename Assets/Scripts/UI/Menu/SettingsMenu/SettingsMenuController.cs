using System.Collections.Generic;
using TS.LocalizationSystem;
using Unity.VisualScripting;
using UnityEngine;

public class SettingsMenuController : MenuController<SettingsMenuView, SettingsMenuData>
{
    public override void OnEnter()
    {
        SetState(new SettingsMenuBaseState_Main(this));
        View.CloseButton.onClick.AddListener(OnCloseButtonClicked);
        View.vibrateToggle.OnValueChanged += OnVibrateValueChanged;
        View.soundToggle.OnValueChanged += OnSoundValueChanged;
        View.musicToggle.OnValueChanged += OnMusicValueChanged;

        View.musicToggle.SetIsOn(GameManager.Instance.SaveData.IsVibrateEnabled, false);
        View.soundToggle.SetIsOn(GameManager.Instance.SaveData.IsVibrateEnabled, false);
        View.vibrateToggle.SetIsOn(GameManager.Instance.SaveData.IsVibrateEnabled, false);
        PopulateLocales();
    }
    public override void OnExit()
    {
        base.OnExit();
        View.CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        View.vibrateToggle.OnValueChanged -= OnVibrateValueChanged;
        View.soundToggle.OnValueChanged -= OnSoundValueChanged;
        View.musicToggle.OnValueChanged -= OnMusicValueChanged;
    }

    private void OnVibrateValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsVibrateEnabled = value;
    }

    private void OnSoundValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsSoundMuted = value;
        SoundController.instance.ToggleSfx(value);

    }

    private void OnMusicValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsMusicMuted = value;
        SoundController.instance.ToggleMusic(value);
    }

    private void PopulateLocales()
    {
        View.LocaleDropdown.ClearOptions();

        var options = new List<TMPro.TMP_Dropdown.OptionData>();

        foreach (var locale in LocaleSettings.Locales.Values)
        {
            // Add to dropdown
            options.Add(new TMPro.TMP_Dropdown.OptionData(locale.Name));
        }

        View.LocaleDropdown.AddOptions(options);

        // Hook selection event
        View.LocaleDropdown.onValueChanged.AddListener(OnLocaleSelected);
    }

    private void OnLocaleSelected(int index)
    {
        Debug.LogError(index);
        foreach (var locale in LocaleSettings.Locales.Values)
        {
            Debug.LogError((int)locale.ReferenceType + " " + locale.ReferenceType);

            if ((int)locale.ReferenceType == index + 1)
            {
                LocaleManager.SetLocale(locale.ReferenceType);
            }
        }
    }
    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    private void OnCloseButtonClicked()
    {
        MenuManager.Instance.GoBack();
    }
}