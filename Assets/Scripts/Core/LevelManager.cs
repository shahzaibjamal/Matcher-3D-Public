using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private GameSaveData _saveData;

    // Theme data can still be set in the inspector
    public List<MapThemeData> MapThemes;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(GameSaveData data)
    {
        _saveData = data;
    }

    // --- Retrieval Logic ---

    /// <summary>
    /// Gets the LevelData for the furthest reached level based on metadata JSON.
    /// </summary>
    public LevelData GetCurrentProgressLevel()
    {
        // 1. If brand new game, return the first level in the metadata
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID))
        {
            return DataManager.Instance.Metadata.Levels.FirstOrDefault();
        }

        // 2. Find the bookmarked level from our DataManager dictionary
        var currentLevel = DataManager.Instance.GetLevelByID(_saveData.CurrentLevelID);

        // 3. Check if this bookmarked level is already completed in SaveData
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == _saveData.CurrentLevelID);
        bool isCompleted = progress != null && progress.IsCompleted;

        if (isCompleted)
        {
            // Check if there's a next level after this one in the JSON
            var nextLevel = GetNextLevelInDatabase(_saveData.CurrentLevelID);
            if (nextLevel != null)
            {
                _saveData.CurrentLevelID = nextLevel.Id;
                return nextLevel;
            }
        }

        // 4. Return bookmark, or fallback to level 1 if something went wrong
        return currentLevel ?? DataManager.Instance.Metadata.Levels.FirstOrDefault();
    }

    public List<LevelDisplayData> GetLevelSelectData()
    {
        var displayList = new List<LevelDisplayData>();
        var allLevels = DataManager.Instance.Metadata.Levels;

        for (int i = 0; i < allLevels.Count; i++)
        {
            var level = allLevels[i];
            var progress = _saveData.LevelHistory.Find(p => p.LevelUID == level.Id);

            bool isFirstLevel = (i == 0);
            bool isCompleted = (progress != null && progress.IsCompleted);
            bool prevLevelCompleted = false;

            if (i > 0)
            {
                var prevId = allLevels[i - 1].Id;
                var prevProgress = _saveData.LevelHistory.Find(p => p.LevelUID == prevId);
                prevLevelCompleted = (prevProgress != null && prevProgress.IsCompleted);
            }

            displayList.Add(new LevelDisplayData
            {
                StaticData = level,
                ProgressData = progress,
                // Unlocked if: it's level 1, or you've already finished it, or the previous one is done
                IsUnlocked = isFirstLevel || isCompleted || prevLevelCompleted
            });
        }

        return displayList;
    }

    public List<LevelDisplayData> GetLevelBatch(int startIndex, int count)
    {
        List<LevelDisplayData> allData = GetLevelSelectData();

        if (startIndex < 0 || startIndex >= allData.Count) return new List<LevelDisplayData>();

        int actualCount = Mathf.Min(count, allData.Count - startIndex);
        return allData.GetRange(startIndex, actualCount);
    }

    /// <summary>
    /// Simple wrapper to get static data from the DataManager.
    /// </summary>
    public LevelData GetLevelByID(string id)
    {
        return DataManager.Instance.GetLevelByID(id);
    }

    // --- Persistence & Game State ---

    public void MarkLevelComplete(string id, float timeTaken, int score, int stars)
    {
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == id);
        if (progress == null)
        {
            progress = new LevelProgress { LevelUID = id };
            _saveData.LevelHistory.Add(progress);
        }

        progress.IsCompleted = true;
        progress.LastPlayed = DateTime.Now;

        // Update high scores/stars
        if (timeTaken < progress.BestTime || progress.BestTime <= 0) progress.BestTime = timeTaken;
        if (score > progress.HighScore) progress.HighScore = score;
        if (stars > progress.StarRating) progress.StarRating = stars;

        // Progression: Move bookmark if we are finishing the current furthest level
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID) || id == _saveData.CurrentLevelID)
        {
            var nextLevel = GetNextLevelInDatabase(id);
            if (nextLevel != null)
            {
                _saveData.CurrentLevelID = nextLevel.Id;
            }
            else
            {
                Debug.Log("No more levels found in metadata. Bookmark remains at: " + id);
            }
        }

        SaveSystem.Save(_saveData);
    }

    /// <summary>
    /// Finds the next level in the sequential list from Metadata.
    /// </summary>


    public LevelData GetNextLevelInDatabase(string currentId)
    {
        var allLevels = DataManager.Instance.Metadata.Levels;

        // Safety check: if there are no levels at all
        if (allLevels == null || allLevels.Count == 0) return null;

        // 1. If currentId is empty, the "next" level is technically the first level (Level 1)
        if (string.IsNullOrEmpty(currentId))
        {
            return allLevels[0];
        }

        // 2. Otherwise, find the index of the specific ID
        int currentIndex = allLevels.FindIndex(l => l.Id == currentId);

        if (currentIndex >= 0 && currentIndex < allLevels.Count - 1)
        {
            return allLevels[currentIndex + 1];
        }

        return null;
    }
    public bool HasMoreContent()
    {
        // 1. If the current ID is empty, we check if there are ANY levels in the database.
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID))
        {
            return DataManager.Instance.Metadata.Levels.Count > 0;
        }

        // 2. Otherwise, check if there is a level after the current one.
        return GetNextLevelInDatabase(_saveData.CurrentLevelID) != null;
    }
}

public struct LevelDisplayData
{
    public LevelData StaticData;
    public LevelProgress ProgressData;
    public bool IsUnlocked;
}