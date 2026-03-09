using TS.LocalizationSystem;

public class SettingsMenuBaseState_Main : SettingsMenuBaseState
{
    public SettingsMenuBaseState_Main(SettingsMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.MainView.gameObject.SetActive(true);

        View.vibrateToggle.OnValueChanged += OnVibrateValueChanged;
        View.soundToggle.OnValueChanged += OnSoundValueChanged;
        View.musicToggle.OnValueChanged += OnMusicValueChanged;

        View.musicToggle.SetIsOn(GameManager.Instance.SaveData.IsMusicMuted, false);
        View.soundToggle.SetIsOn(GameManager.Instance.SaveData.IsSoundMuted, false);
        View.vibrateToggle.SetIsOn(GameManager.Instance.SaveData.IsVibrateEnabled, false);

        View.PrivacyButton.onClick.AddListener(OnPrivacyButtonClicked);
        View.TermsButton.onClick.AddListener(OnTermsButtonClicked);
        View.LanguageButton.onClick.AddListener(OnLanguageButtonClicked);
        View.TitleText.text = LocaleManager.Localize(LocalizationKeys.settings);
    }

    public override void Exit()
    {
        base.Exit();
        View.MainView.gameObject.SetActive(false);
        View.vibrateToggle.OnValueChanged -= OnVibrateValueChanged;
        View.soundToggle.OnValueChanged -= OnSoundValueChanged;
        View.musicToggle.OnValueChanged -= OnMusicValueChanged;
        View.PrivacyButton.onClick.RemoveListener(OnPrivacyButtonClicked);
        View.TermsButton.onClick.RemoveListener(OnTermsButtonClicked);
        View.LanguageButton.onClick.RemoveListener(OnLanguageButtonClicked);
    }

    private void OnVibrateValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsVibrateEnabled = value;
        if (value)
            GameManager.Instance.Vibrate();
    }

    private void OnSoundValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsSoundMuted = value;
        SoundController.Instance.ToggleSfx(value);
    }

    private void OnMusicValueChanged(bool value)
    {
        GameManager.Instance.SaveData.IsMusicMuted = value;
        SoundController.Instance.ToggleMusic(value);
    }

    private void OnLanguageButtonClicked()
    {
        Controller.SetState(new SettingsMenuBaseState_Language(Controller));
    }
    private void OnPrivacyButtonClicked()
    {
        Controller.SetState(new SettingsMenuBaseState_Privacy(Controller));
    }
    private void OnTermsButtonClicked()
    {
        Controller.SetState(new SettingsMenuBaseState_Terms(Controller));
    }
}
