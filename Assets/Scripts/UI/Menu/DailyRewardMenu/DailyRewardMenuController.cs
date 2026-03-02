using System.Collections.Generic;

public class DailyRewardMenuController : MenuController<DailyRewardMenuView, DailyRewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new DailyRewardMenuBaseState_Main(this));

        View.DailyRewardsWindow.Initialize(new List<DailyRewardData>
        {
            new DailyRewardData
            {
                Day = 1,
                Amount = 121,
                RewardType = RewardType.Gold
            },
            new DailyRewardData
            {
                Day = 2,
                Amount = 2,
                RewardType = RewardType.Hint
            },
            new DailyRewardData
            {
                Day = 3,
                Amount = 1,
                RewardType = RewardType.Shake
            },
            new DailyRewardData
            {
                Day = 4,
                Amount = 2,
                RewardType = RewardType.Undo
            },
            new DailyRewardData
            {
                Day = 5,
                Amount = 2,
                RewardType = RewardType.Magnet
            },
            new DailyRewardData
            {
                Day = 6,
                Amount = 5,
                RewardType = RewardType.Undo
            },
            new DailyRewardData
            {
                Day = 7,
                Amount = 120,
                RewardType = RewardType.Gold
            },
        }, OnRewardClaimedCallback);
    }

    private void OnRewardClaimedCallback(RewardData rewardData)
    {
        GameManager.Instance.SaveGame(); ;
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
        base.HandleBackInput();
    }
}