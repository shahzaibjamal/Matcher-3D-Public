using System.Collections.Generic;

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
        View.SpinWheelController.Setup(list);

    }

    private void OnSpinwheelButtonClick()
    {
        View.SpinWheelController.TurnWheel();
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