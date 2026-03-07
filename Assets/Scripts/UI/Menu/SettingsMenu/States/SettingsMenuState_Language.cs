using System.Collections.Generic;
using System.Xml.Serialization;
using TS.LocalizationSystem;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SettingsMenuBaseState_Language : SettingsMenuBaseState
{

    private List<LanguageView> _languageViews = new List<LanguageView>();
    public SettingsMenuBaseState_Language(SettingsMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.LanguageContainer.gameObject.SetActive(true);
        View.CloseButton.gameObject.SetActive(true);
        View.TitleText.text = LocaleManager.Localize(LocalizationKeys.language);
        PopulateLanguages();
        SoundController.instance.PlaySoundEffect("menu_click");
    }

    public override void Exit()
    {
        base.Exit();
        View.CloseButton.gameObject.SetActive(false);
        View.LanguageContainer.gameObject.SetActive(false);
        foreach (var languageView in _languageViews)
        {
            GameObject.Destroy(languageView.gameObject);
        }
        _languageViews.Clear();
    }

    private void PopulateLanguages()
    {
        foreach (var locale in LocaleSettings.Locales.Values)
        {
            var go = GameObject.Instantiate(View.LanguageViewPrefab, View.LanguageContainer);
            if (go.TryGetComponent<LanguageView>(out LanguageView languageView))
            {
                languageView.Init(locale.Name, OnLanguageSelect);
                languageView.SetSelected(LocaleManager.Current.Name == locale.Name);
                _languageViews.Add(languageView);
            }
        }
    }

    private void OnLanguageSelect(string name)
    {
        foreach (var locale in LocaleSettings.Locales.Values)
        {
            if (locale.Name == name)
            {
                LocaleManager.SetLocale(locale.ReferenceType);
                break;
            }
        }
        foreach (var languageView in _languageViews)
        {
            languageView.SetSelected(name == languageView.Name);
        }
        GameManager.Instance.SaveData.languageName = name;
    }
}
