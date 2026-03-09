using System.Collections.Generic;
using TS.LocalizationSystem;

public class SpinWheelMenuController : MenuController<SpinWheelMenuView, SpinWheelMenuData>
{
    public override void OnEnter()
    {
        SetState(new SpinWheelMenuBaseState(this));
        View.SpinButton.onClick.AddListener(OnSpinwheelButtonClick);

        View.SpinWheelController.Setup(DataManager.Instance.Metadata.SpinWheelRewards, OnSpinWheelRewardComplete);
        UpdateSpinButton();
    }

    private void OnSpinwheelButtonClick()
    {
        if (GameManager.Instance.SaveData.CanSpin())
        {
            View.SpinButton.interactable = false;
            View.SpinWheelController.TurnWheel();
            return;
        }

        MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(Menus.Type.GenericPopup, new GenericPopupMenuData
        (
            LocalizationKeys.show_ad,
            LocalizationKeys.spin_ad_message,
            LocaleManager.Localize(LocalizationKeys.yes),
            ShowAd,
            LocaleManager.Localize(LocalizationKeys.no)
        ));
    }

    private void ShowAd()
    {
        // show ad and then turn the wheel
        AdManager.Instance.ShowRewarded(OnRewardAdComplete, OnRewardAdFailed);
    }
    private void OnRewardAdComplete()
    {
        View.SpinButton.interactable = false;
        View.SpinWheelController.TurnWheel();
    }
    private void OnRewardAdFailed()
    {
        MenuManager.Instance.OpenMenu<GenericPopupMenuView, GenericPopupMenuController, GenericPopupMenuData>(Menus.Type.GenericPopup, new GenericPopupMenuData
        (
            LocalizationKeys.no,
            LocalizationKeys.no_ads_message,
            LocaleManager.Localize(LocalizationKeys.ok)
        ));
    }
    private void OnSpinWheelRewardComplete(SpinWheelData spinWheelRewardData)
    {
        View.SpinButton.interactable = true;
        GameManager.Instance.SaveData.RecordSpin();
        RewardManager.Instance.AddRewardToQueue(spinWheelRewardData.Reward);
        GameManager.Instance.SaveData.Inventory.AddRewards(new List<RewardData> { spinWheelRewardData.Reward });

        Scheduler.Instance.ExecuteAfterDelay(0.5f, () => RewardManager.Instance.CheckAndShowNext(UpdateSpinButton));
    }

    private void UpdateSpinButton()
    {
        bool canSpin = GameManager.Instance.SaveData.CanSpin();
        View.AdImage.gameObject.SetActive(!canSpin);
        View.SpinButtonText.gameObject.SetActive(canSpin);
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    public override void OnPause()
    {
    }

    public override void OnResume()
    {
    }

    public override void HandleBackInput()
    {
        // base.HandleBackInput();
    }
}