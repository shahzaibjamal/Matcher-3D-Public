using System.Collections.Generic;
using TS.LocalizationSystem;

public class SpinWheelMenuController : MenuController<SpinWheelMenuView, SpinWheelMenuData>
{
    public override void OnEnter()
    {
        SetState(new SpinWheelMenuBaseState(this));
        View.SpinButton.onClick.AddListener(OnSpinwheelButtonClick);

        var list = new List<RewardData>
        {
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 2,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 3,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 4,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 5,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 6,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 7,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 8,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 9,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 10,
                RewardType = RewardType.Gold
            }
        };
        View.SpinWheelController.Setup(DataManager.Instance.Metadata.SpinWheelRewards, OnSpinWheelRewardComplete);
        UpdateSpinButton();
    }

    private void OnSpinwheelButtonClick()
    {
        if (GameManager.Instance.SaveData.CanSpin())
        {
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
        View.SpinWheelController.TurnWheel();
    }

    private void OnSpinWheelRewardComplete(SpinWheelData spinWheelRewardData)
    {
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