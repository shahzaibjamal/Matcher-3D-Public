using System.Collections.Generic;

public class MatchResultMenuData : MenuData
{
    public bool IsWin;
    public LevelData LevelData;
    public List<RewardData> Rewards;
    public float MatchRate;

    public int Score;
}