using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

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
    public bool IsAppReviewed;
    public int AppReviewReminderLevel;
    public string languageName;
    public List<int> ClaimedDailyRewards = new List<int>();

    public DateTime SignUpDate;
    public string LastSpinDateString; // Store as string for easier JSON serialization
    public List<RewardData> SavedPendingRewards = new List<RewardData>();


    #region Lives
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
        int maxLives = DataManager.Instance.Metadata.Settings.MaxLives;
        int secondsPerLife = DataManager.Instance.Metadata.Settings.SecondsToRecover;

        if (Inventory.CurrentLivesRaw >= maxLives || string.IsNullOrEmpty(LastLifeLostTime))
        {
            LastLifeLostTime = string.Empty;
            return;
        }

        if (DateTime.TryParse(LastLifeLostTime, out DateTime lastLost))
        {
            double secondsPassed = (DateTime.Now - lastLost).TotalSeconds;
            int livesToRestore = Mathf.FloorToInt((float)(secondsPassed / secondsPerLife));

            if (livesToRestore > 0)
            {
                // --- STEP 1: Update the State FIRST ---
                if (Inventory.CurrentLivesRaw + livesToRestore >= maxLives)
                {
                    LastLifeLostTime = string.Empty;
                }
                else
                {
                    // Move anchor forward to account for consumed lives
                    // We use the original lastLost + used time to be frame-perfect
                    DateTime newAnchor = lastLost.AddSeconds(livesToRestore * secondsPerLife);
                    LastLifeLostTime = newAnchor.ToString("o");
                }

                // --- STEP 2: Trigger the Event LAST ---
                // Now, if this triggers a re-entry to this function, 
                // the 'if (livesToRestore > 0)' check will be false because 
                // the timestamp has already been moved forward.
                Inventory.AddLives(livesToRestore, maxLives);
            }
        }
    }
    public void UseLife()
    {
        UpdateLivesLogic();
        if (Inventory.CurrentLivesRaw <= 0) return;

        Inventory.ConsumeLife();

        if (Inventory.CurrentLivesRaw < DataManager.Instance.Metadata.Settings.MaxLives && string.IsNullOrEmpty(LastLifeLostTime))
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