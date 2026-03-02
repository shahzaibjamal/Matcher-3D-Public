using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private LevelDatabase levelDatabase;

    private GameSaveData _saveData;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(GameSaveData data)
    {
        _saveData = data;
    }

    public List<MapTheme> mapThemes;
    // --- Retrieval Logic ---

    /// <summary>
    /// Gets the LevelData for the furthest reached level.
    /// Logic: If the current bookmarked level is finished AND a next level exists in the DB, 
    /// it automatically returns the new level (handling content updates).
    /// </summary>
    public LevelData GetCurrentProgressLevel()
    {
        // 1. If brand new game, return first level
        if (string.IsNullOrEmpty(_saveData.CurrentLevelUID))
        {
            return levelDatabase.levels.FirstOrDefault();
        }

        // 2. Find the bookmarked level
        var currentLevel = levelDatabase.levels.FirstOrDefault(l => l.levelUID == _saveData.CurrentLevelUID);

        // 3. Check if this bookmarked level is already completed
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == _saveData.CurrentLevelUID);
        bool isCompleted = progress != null && progress.IsCompleted;

        if (isCompleted)
        {
            // If completed, check if there's a new level after it (Content Update check)
            var nextLevel = GetNextLevelInDatabase(_saveData.CurrentLevelUID);
            if (nextLevel != null)
            {
                // Move bookmark forward automatically for the GameManager to load
                _saveData.CurrentLevelUID = nextLevel.levelUID;
                return nextLevel;
            }
        }

        // 4. Return the bookmark (either the current unfinished level or the last finished level)
        return currentLevel ?? levelDatabase.levels.FirstOrDefault();
    }

    public List<LevelDisplayData> GetLevelSelectData()
    {
        var displayList = new List<LevelDisplayData>();

        for (int i = 0; i < levelDatabase.levels.Count; i++)
        {
            var level = levelDatabase.levels[i];
            var progress = _saveData.LevelHistory.Find(p => p.LevelUID == level.levelUID);

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
                ProgressData = progress,
                IsUnlocked = isFirstLevel || hasProgress || prevLevelCompleted
            });
        }

        return displayList;
    }

    public List<LevelDisplayData> GetLevelBatch(int startIndex, int count)
    {
        List<LevelDisplayData> allData = GetLevelSelectData();

        // FIX: If index is negative or beyond total levels, return empty list instead of crashing
        if (startIndex < 0 || startIndex >= allData.Count) return new List<LevelDisplayData>();

        int actualCount = Mathf.Min(count, allData.Count - startIndex);
        return allData.GetRange(startIndex, actualCount);
    }

    /// <summary>
    /// Called by the LevelNode button. Returns the static data for a specific UID.
    /// </summary>
    public LevelData GetLevelByUID(string uid)
    {
        return levelDatabase.levels.FirstOrDefault(l => l.levelUID == uid);
    }
    // --- Persistence & Game State ---

    public void MarkLevelComplete(string uid, float timeTaken, int score, int stars)
    {
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == uid);
        if (progress == null)
        {
            progress = new LevelProgress { LevelUID = uid };
            _saveData.LevelHistory.Add(progress);
        }

        progress.IsCompleted = true;
        progress.LastPlayed = DateTime.Now;

        if (timeTaken < progress.BestTime || progress.BestTime <= 0) progress.BestTime = timeTaken;
        if (score > progress.HighScore) progress.HighScore = score;
        if (stars > progress.StarRating) progress.StarRating = stars;

        // SMART PROGRESSION: Only move the bookmark if there is actually a next level.
        // If not, the bookmark stays on the current level.
        if (string.IsNullOrEmpty(_saveData.CurrentLevelUID) || uid == _saveData.CurrentLevelUID)
        {
            var nextLevel = GetNextLevelInDatabase(uid);
            if (nextLevel != null)
            {
                _saveData.CurrentLevelUID = nextLevel.levelUID;
            }
            else
            {
                // Bookmark stays here. In the next app launch, GetCurrentProgressLevel 
                // will check the DB again to see if new levels were added in an update.
                Debug.Log("Current content finished. Bookmark saved at: " + uid);
            }
        }

        SaveSystem.Save(_saveData);
    }

    public LevelData GetNextLevelInDatabase(string currentUid)
    {
        int currentIndex = levelDatabase.levels.FindIndex(l => l.levelUID == currentUid);
        if (currentIndex >= 0 && currentIndex < levelDatabase.levels.Count - 1)
        {
            return levelDatabase.levels[currentIndex + 1];
        }
        return null;
    }

    /// <summary>
    /// Helper for UI to know if we should show a "Coming Soon" or "Credits" screen.
    /// </summary>
    public bool HasMoreContent()
    {
        return GetNextLevelInDatabase(_saveData.CurrentLevelUID) != null;
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

