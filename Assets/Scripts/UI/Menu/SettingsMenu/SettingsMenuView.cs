using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuView : MenuView
{
    public Button CloseButton;
    [Header("Main")]
    public Button LanguageButton;
    public TMP_Text TitleText;

    public SmartToggle soundToggle;
    public SmartToggle musicToggle;
    public SmartToggle vibrateToggle;
    public Button TermsButton;
    public Button PrivacyButton;


    public GameObject MainView;
    [Header("Language")]
    public Transform LanguageContainer;
    public GameObject LanguageViewPrefab;
    [Header("Privacy")]
    public TMP_Text PrivacyText;
    public Transform PrivacyContainer;
    [Header("Terms")]
    public TMP_Text TermsText;
    public Transform TermsContainer;

}