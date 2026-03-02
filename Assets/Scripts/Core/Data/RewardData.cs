public class RewardData
{
    public RewardType RewardType;
    public int Amount;
}

[System.Serializable]
public class DailyRewardData : RewardData
{
    public int Day;
}
public enum RewardType
{
    Gold = 1,
    Magnet = 2,
    Hint = 3,

    Shake = 4,

    Undo = 5
}