public enum RewardType { DailyLogin, Achievement, SpecialOffer, DailySpin }

public class RewardData
{
    public string ID;
    public RewardType Type;
    public string Title;
    public int Amount;
    public bool IsClaimed;
}