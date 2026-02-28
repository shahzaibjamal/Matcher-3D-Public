using System.Collections.Generic;

public class SpinWheelMenuController : MenuController<SpinWheelMenuView, SpinWheelMenuData>
{
    public override void OnEnter()
    {
        SetState(new SpinWheelMenuBaseState(this));
        View.SpinButton.onClick.AddListener(OnSpinwheelButtonClick);

        var list = new List<SpinRewardData>
        {
            new SpinRewardData
            {
                Amount = 300,
                SpinRewardType = SpinRewardType.Gold
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Magnet
            },
            new SpinRewardData
            {
                Amount = 2,
                SpinRewardType = SpinRewardType.Undo
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Hint
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Shake
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Magnet
            },
            new SpinRewardData
            {
                Amount = 200,
                SpinRewardType = SpinRewardType.Gold
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Shake
            },
            new SpinRewardData
            {
                Amount = 1,
                SpinRewardType = SpinRewardType.Undo
            },
            new SpinRewardData
            {
                Amount = 100,
                SpinRewardType = SpinRewardType.Gold
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