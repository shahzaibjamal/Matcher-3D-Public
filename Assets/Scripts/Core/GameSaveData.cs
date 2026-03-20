using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
    public List<string> CompletedFTUESequences = new List<string>();
    public Inventory Inventory { get; private set; } = new Inventory();
    // Global player stats
    public int TotalCoins;
    public bool IsMusicMuted;
    public bool IsSoundMuted;
    public bool IsVibrateEnabled;
    public string languageName;
    public List<int> ClaimedDailyRewards = new List<int>();

    public DateTime SignUpDate;
    public string LastSpinDateString; // Store as string for easier JSON serialization
    public List<RewardData> SavedPendingRewards = new List<RewardData>();


    #region Lives
    public const int MAX_LIVES = 5;
    public const int SECONDS_TO_RECOVER_LIFE = 1800;
    public string LastLifeLostTime;

    // Logic-driven property
    [JsonIgnore]
    public int CurrentLives
    {
        get
        {
            UpdateLivesLogic();
            return Inventory.CurrentLivesRaw;
        }
    }

    public void UpdateLivesLogic()
    {
        if (Inventory.CurrentLivesRaw >= MAX_LIVES || string.IsNullOrEmpty(LastLifeLostTime)) return;

        if (DateTime.TryParse(LastLifeLostTime, out DateTime lastLost))
        {
            double secondsPassed = (DateTime.Now - lastLost).TotalSeconds;

            if (secondsPassed >= SECONDS_TO_RECOVER_LIFE)
            {
                int recovered = (int)(secondsPassed / SECONDS_TO_RECOVER_LIFE);

                Inventory.AddLives(recovered, MAX_LIVES);

                if (Inventory.CurrentLivesRaw >= MAX_LIVES)
                    LastLifeLostTime = string.Empty;
                else
                    LastLifeLostTime = lastLost.AddSeconds(recovered * SECONDS_TO_RECOVER_LIFE).ToString("o");
            }
        }
    }

    public void UseLife()
    {
        UpdateLivesLogic();
        if (Inventory.CurrentLivesRaw <= 0) return;

        Inventory.ConsumeLife();

        if (Inventory.CurrentLivesRaw < MAX_LIVES && string.IsNullOrEmpty(LastLifeLostTime))
        {
            LastLifeLostTime = DateTime.Now.ToString("o");
        }
    }
    #endregion
    /// <summary>
    /// Checks if the player is eligible for a new spin based on the calendar day.
    /// </summary>
    public bool CanSpin()
    {
        if (string.IsNullOrEmpty(LastSpinDateString)) return true;

        if (DateTime.TryParse(LastSpinDateString, out DateTime lastSpin))
        {
            // Returns true if the current date is strictly after the last spin date
            return DateTime.Now.Date > lastSpin.Date;
        }

        return true;
    }

    /// <summary>
    /// Updates the save data to record that a spin was used today.
    /// </summary>
    public void RecordSpin()
    {
        LastSpinDateString = DateTime.Now.ToString("yyyy-MM-dd");
    }
}