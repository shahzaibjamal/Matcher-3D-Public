using System;

public class RewardMenuData : MenuData
{
    public Action Callback { get; private set; }
    public RewardData RewardData { get; private set; }

    public RewardMenuData() { }

    public RewardMenuData(RewardData rewardData, Action callback)
    {
        Callback = callback;
        RewardData = rewardData;
    }
}