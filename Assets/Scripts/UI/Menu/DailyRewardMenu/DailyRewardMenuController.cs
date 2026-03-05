using System.Collections.Generic;

public class DailyRewardMenuController : MenuController<DailyRewardMenuView, DailyRewardMenuData>
{
    public override void OnEnter()
    {
        SetState(new DailyRewardMenuBaseState_Main(this));

        View.DailyRewardsWindow.Initialize(DataManager.Instance.Metadata.DailyRewards, OnRewardClaimedCallback);
    }

    private void OnRewardClaimedCallback(List<RewardData> rewards)
    {
        GameManager.Instance.SaveData.Inventory.AddRewards(rewards);
        GameManager.Instance.SaveGame();
        RewardManager.Instance.AddRewardToQueue(rewards);
    }

    public override void OnExit()
    {
        base.OnExit();
        RewardManager.Instance.CheckAndShowNext();
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