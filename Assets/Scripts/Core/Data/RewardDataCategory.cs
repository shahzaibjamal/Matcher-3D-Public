public enum RewardCategoryType { DailyLogin, Achievement, SpecialOffer, DailySpin }

public class RewardDataCategory
{
    public string ID;
    public RewardCategoryType Type;
    public string Title;
    public int Amount;
    public bool IsClaimed;
}