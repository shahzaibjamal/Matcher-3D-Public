public enum RewardType { DailyLogin, Achievement, SpecialOffer }

public class RewardData
{
    public string ID;
    public RewardType Type;
    public string Title;
    public int Amount;
    public bool IsClaimed;
}