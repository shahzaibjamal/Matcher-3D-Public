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
    public int MaxLives = 5;
    public const int SECONDS_TO_RECOVER_LIFE = 1800; // 30 minutes

    public int CurrentLivesRaw = 5; // The last "known" life count saved to disk
    public string LastLifeLostTime; // ISO 8601 string

    public int CurrentLives
    {
        get
        {
            UpdateLivesLogic();
            return CurrentLivesRaw;
        }
    }

    private void UpdateLivesLogic()
    {
        // If we are already full or no life was ever lost, there's nothing to calculate
        if (CurrentLivesRaw >= MaxLives || string.IsNullOrEmpty(LastLifeLostTime)) return;

        if (DateTime.TryParse(LastLifeLostTime, out DateTime lastLost))
        {
            double secondsPassed = (DateTime.Now - lastLost).TotalSeconds;

            if (secondsPassed >= SECONDS_TO_RECOVER_LIFE)
            {
                int recovered = (int)(secondsPassed / SECONDS_TO_RECOVER_LIFE);
                int previousCount = CurrentLivesRaw;

                CurrentLivesRaw = Math.Min(MaxLives, CurrentLivesRaw + recovered);

                // If we hit Max, reset the timestamp. 
                // Otherwise, move the anchor forward by exactly the number of lives recovered.
                if (CurrentLivesRaw >= MaxLives)
                    LastLifeLostTime = string.Empty;
                else
                    LastLifeLostTime = lastLost.AddSeconds(recovered * SECONDS_TO_RECOVER_LIFE).ToString("o");

                // Fire event if the integer value actually stepped up
                if (previousCount != CurrentLivesRaw)
                {
                    GameEvents.OnLivesChanged?.Invoke();
                }
            }
        }
    }

    public void UseLife()
    {
        UpdateLivesLogic();

        if (CurrentLivesRaw <= 0) return;

        // 1. Spend the life first
        CurrentLivesRaw--;

        // 2. Now check: Did we just drop below Max? 
        // If we were at Max (or above) and are now at Max-1, start the clock.
        // We also check if the timer isn't already running (string is null/empty).
        if (CurrentLivesRaw < MaxLives && string.IsNullOrEmpty(LastLifeLostTime))
        {
            LastLifeLostTime = DateTime.Now.ToString("o");
            // Debug.Log("Timer Started: Dropped below Max Lives.");
        }

        GameEvents.OnLivesChanged?.Invoke();
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