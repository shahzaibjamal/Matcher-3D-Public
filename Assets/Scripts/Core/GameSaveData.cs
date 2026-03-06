using System;
using System.Collections.Generic;

[Serializable]
public class LevelProgress
{
    public string LevelUID;
    public bool IsCompleted;
    public float BestTime;
    public int StarRating; // Suggested: 1-3 stars
    public int HighScore;  // Suggested: Score tracking
    public DateTime LastPlayed;
}

[Serializable]
public class GameSaveData
{
    public string CurrentLevelID; // The last level the player was on
    public List<LevelProgress> LevelHistory = new List<LevelProgress>();

    public Inventory Inventory { get; private set; } = new Inventory();
    // Global player stats
    public int TotalCoins;
    public bool IsMusicMuted;
    public bool IsSoundMuted;
    public bool IsVibrateEnabled;
    public List<int> ClaimedDailyRewards = new List<int>();

    public DateTime SignUpDate;
    public List<RewardData> SavedPendingRewards = new List<RewardData>();
}