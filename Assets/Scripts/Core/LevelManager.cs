using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelDatabase levelDatabase;

    private GameSaveData _saveData; // Shared reference from GameManager

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Injected by GameManager at startup after loading the SaveData.
    /// </summary>
    public void Initialize(GameSaveData data)
    {
        _saveData = data;
    }

    // --- Retrieval Logic ---

    /// <summary>
    /// Gets the LevelData for the furthest reached level (for a 'Continue' button).
    /// </summary>
    public LevelData GetCurrentProgressLevel()
    {
        return levelDatabase.levels.FirstOrDefault(l => l.levelUID == _saveData.CurrentLevelUID) ?? levelDatabase.levels.FirstOrDefault();
    }

    /// <summary>
    /// Returns a list of all levels paired with their save progress and unlock status for UI display.
    /// </summary>
    public List<LevelDisplayData> GetLevelSelectData()
    {
        var displayList = new List<LevelDisplayData>();

        for (int i = 0; i < levelDatabase.levels.Count; i++)
        {
            var level = levelDatabase.levels[i];
            var progress = _saveData.LevelHistory.Find(p => p.LevelUID == level.levelUID);

            // Logic: Unlocked if it's the first level, or if the player has reached it,
            // or if the previous level in the list is marked as completed.
            bool isFirstLevel = (i == 0);
            bool hasProgress = (progress != null && progress.IsCompleted);
            bool prevLevelCompleted = false;

            if (i > 0)
            {
                var prevProgress = _saveData.LevelHistory.Find(p => p.LevelUID == levelDatabase.levels[i - 1].levelUID);
                prevLevelCompleted = (prevProgress != null && prevProgress.IsCompleted);
            }

            displayList.Add(new LevelDisplayData
            {
                StaticData = level,
                ProgressData = progress, // null if never played
                IsUnlocked = isFirstLevel || hasProgress || prevLevelCompleted
            });
        }

        return displayList;
    }

    // --- Persistence & Game State ---

    /// <summary>
    /// Updates progress for a level. Handles high scores and prevents replay resets.
    /// </summary>
    public void MarkLevelComplete(string uid, float timeTaken, int score, int stars)
    {
        // 1. Find or create the progress entry for the specific level finished
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == uid);
        if (progress == null)
        {
            progress = new LevelProgress { LevelUID = uid };
            _saveData.LevelHistory.Add(progress);
        }

        // 2. Update stats only if the new run is better
        progress.IsCompleted = true;
        progress.LastPlayed = DateTime.Now;

        if (timeTaken < progress.BestTime || progress.BestTime <= 0) progress.BestTime = timeTaken;
        if (score > progress.HighScore) progress.HighScore = score;
        if (stars > progress.StarRating) progress.StarRating = stars;

        // 3. Update "CurrentLevelUID" (Progression)
        // ONLY advance if the player completed the level they were actually "on".
        // This prevents replaying Level 1 from moving your "Continue" marker backward.
        if (uid == _saveData.CurrentLevelUID)
        {
            var nextLevel = GetNextLevelInDatabase(uid);
            if (nextLevel != null)
            {
                _saveData.CurrentLevelUID = nextLevel.levelUID;
            }
        }

        // 4. Save to Disk
        SaveSystem.Save(_saveData);
    }

    private LevelData GetNextLevelInDatabase(string currentUid)
    {
        int currentIndex = levelDatabase.levels.FindIndex(l => l.levelUID == currentUid);
        if (currentIndex >= 0 && currentIndex < levelDatabase.levels.Count - 1)
        {
            return levelDatabase.levels[currentIndex + 1];
        }
        return null;
    }
}

/// <summary>
/// Helper struct to pass data to the Level Select UI.
/// </summary>
public struct LevelDisplayData
{
    public LevelData StaticData;
    public LevelProgress ProgressData;
    public bool IsUnlocked;
}