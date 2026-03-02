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
                Amount = 300,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Magnet
            },
            new RewardData
            {
                Amount = 2,
                RewardType = RewardType.Undo
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Hint
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Shake
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Magnet
            },
            new RewardData
            {
                Amount = 200,
                RewardType = RewardType.Gold
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Shake
            },
            new RewardData
            {
                Amount = 1,
                RewardType = RewardType.Undo
            },
            new RewardData
            {
                Amount = 100,
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