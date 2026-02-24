using System.Collections.Generic;
using TS.LocalizationSystem;
using UnityEngine;

public class SettingsMenuController : MenuController<SettingsMenuView, SettingsMenuData>
{

    // private readonly List<Locale> localeList = new List<Locale>();
    public override void OnEnter()
    {
        SetState(new SettingsMenuBaseState_Main(this));
        View.CloseButton.onClick.AddListener(OnCloseButtonClicked);
        PopulateLocales();
    }
    public override void OnExit()
    {
        base.OnExit();
        View.CloseButton.onClick.RemoveListener(OnCloseButtonClicked);

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
        Debug.LogError(options.Count);
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