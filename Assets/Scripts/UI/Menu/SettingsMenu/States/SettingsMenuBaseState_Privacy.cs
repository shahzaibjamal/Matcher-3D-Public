using TS.LocalizationSystem;

public class SettingsMenuBaseState_Privacy : SettingsMenuBaseState
{
    public SettingsMenuBaseState_Privacy(SettingsMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.PrivacyContainer.gameObject.SetActive(true);
        View.CloseButton.gameObject.SetActive(true);
        View.TitleText.text = LocaleManager.Localize(LocalizationKeys.privacy);
        SoundController.instance.PlaySoundEffect("menu_click");
    }

    public override void Exit()
    {
        base.Exit();
        View.CloseButton.gameObject.SetActive(false);
        View.PrivacyContainer.gameObject.SetActive(false);
    }

}
