using System.Collections.Generic;

public class DailyRewardMenuController : MenuController<DailyRewardMenuView, DailyRewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new DailyRewardMenuBaseState_Main(this));

        View.DailyRewardsWindow.Initialize(DataManager.Instance.Metadata.DailyRewards, OnRewardClaimedCallback);
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