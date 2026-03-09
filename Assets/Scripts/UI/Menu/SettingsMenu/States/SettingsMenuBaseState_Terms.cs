using TS.LocalizationSystem;

public class SettingsMenuBaseState_Terms : SettingsMenuBaseState
{
    public SettingsMenuBaseState_Terms(SettingsMenuController controller) : base(controller)
    {
    }

    public override void Enter()
    {
        base.Enter();
        View.TermsContainer.gameObject.SetActive(true);
        View.CloseButton.gameObject.SetActive(true);
        View.TitleText.text = LocaleManager.Localize(LocalizationKeys.terms_condition);
        SoundController.Instance.PlaySoundEffect("menu_click");
    }

    public override void Exit()
    {
        base.Exit();
        View.CloseButton.gameObject.SetActive(false);
        View.TermsContainer.gameObject.SetActive(false);
    }
}
