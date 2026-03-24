using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private GameSaveData _saveData;
    public List<MapThemeData> MapThemes;

    // --- CACHES ---
    // Maps ID to Index so we never have to use FindIndex() again
    private Dictionary<string, int> _idToIndexCache = new Dictionary<string, int>();
    // Reusable list to prevent Garbage Collection (GC) spikes
    private List<LevelDisplayData> _cachedDisplayList = new List<LevelDisplayData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to your data loaded event
        DataManager.OnDataLoaded += BuildLevelCache;
    }

    private void OnDestroy()
    {
        DataManager.OnDataLoaded -= BuildLevelCache;
    }

    private void BuildLevelCache()
    {
        _idToIndexCache.Clear();
        var levels = DataManager.Instance.Metadata.Levels;
        for (int i = 0; i < levels.Count; i++)
        {
            _idToIndexCache[levels[i].Id] = i;
        }
        Debug.Log($"[LevelManager] Cached {levels.Count} level indices.");
    }

    public void Initialize(GameSaveData data) => _saveData = data;

    // --- OPTIMIZED RETRIEVAL ---

    public List<LevelDisplayData> GetLevelSelectData()
    {
        var allLevels = DataManager.Instance.Metadata.Levels;

        // Clear instead of 'new' to reuse memory and prevent GC stutter
        _cachedDisplayList.Clear();

        for (int i = 0; i < allLevels.Count; i++)
        {
            var level = allLevels[i];

            // Optimization: Inline the search logic to avoid extra method overhead
            LevelProgress progress = null;
            foreach (var p in _saveData.LevelHistory)
            {
                if (p.LevelUID == level.Id) { progress = p; break; }
            }

            bool isCompleted = progress != null && progress.IsCompleted;
            bool isUnlocked = (i == 0) || isCompleted;

            // Check previous level completion for unlocking
            if (!isUnlocked && i > 0)
            {
                var prevId = allLevels[i - 1].Id;
                foreach (var p in _saveData.LevelHistory)
                {
                    if (p.LevelUID == prevId && p.IsCompleted) { isUnlocked = true; break; }
                }
            }

            _cachedDisplayList.Add(new LevelDisplayData
            {
                StaticData = level,
                ProgressData = progress,
                IsUnlocked = isUnlocked
            });
        }
        return _cachedDisplayList;
    }

    // --- INSTANT INDEX LOOKUPS ---

    public LevelData GetNextLevelInDatabase(string currentId)
    {
        if (string.IsNullOrEmpty(currentId)) return DataManager.Instance.Metadata.Levels.FirstOrDefault();

        if (_idToIndexCache.TryGetValue(currentId, out int index))
        {
            int nextIndex = index + 1;
            if (nextIndex < DataManager.Instance.Metadata.Levels.Count)
                return DataManager.Instance.Metadata.Levels[nextIndex];
        }
        return null;
    }

    public LevelData GetPrevLevelInDatabase(string currentId)
    {
        if (string.IsNullOrEmpty(currentId) || !_idToIndexCache.TryGetValue(currentId, out int index))
            return null;

        if (index > 0)
            return DataManager.Instance.Metadata.Levels[index - 1];

        return null;
    }

    // --- FASTER STATE MANAGEMENT ---

    public void MarkLevelComplete(string id, float timeTaken, int score, int stars)
    {
        // 1. Find or Create Progress
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == id);
        if (progress == null)
        {
            progress = new LevelProgress { LevelUID = id };
            _saveData.LevelHistory.Add(progress);
        }

        // 2. Update Stats
        progress.IsCompleted = true;
        progress.LastPlayed = DateTime.Now;
        if (timeTaken < progress.BestTime || progress.BestTime <= 0) progress.BestTime = timeTaken;
        if (score > progress.HighScore) progress.HighScore = score;
        if (stars > progress.StarRating) progress.StarRating = stars;

        // 3. Move Bookmark using the Cache
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID) || id == _saveData.CurrentLevelID)
        {
            var nextLevel = GetNextLevelInDatabase(id);
            if (nextLevel != null) _saveData.CurrentLevelID = nextLevel.Id;
        }

        SaveSystem.Save(_saveData);
    }
    // --- Missing Logic Bridges ---

    /// <summary>
    /// Returns the actual LevelData object the player is currently on.
    /// Uses the Cache for O(1) speed.
    /// </summary>
    public LevelData GetCurrentProgressLevel()
    {
        // Fallback to first level if ID is missing
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID))
        {
            return DataManager.Instance.Metadata.Levels.FirstOrDefault();
        }

        return GetLevelByID(_saveData.CurrentLevelID);
    }

    /// <summary>
    /// Instant lookup for LevelData by ID using the Dictionary.
    /// </summary>
    public LevelData GetLevelByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        if (_idToIndexCache.TryGetValue(id, out int index))
        {
            return DataManager.Instance.Metadata.Levels[index];
        }

        // Fallback to DataManager's internal search if cache isn't ready
        return DataManager.Instance.GetLevelByID(id);
    }

    /// <summary>
    /// Returns a segment of levels for the Map Generator.
    /// Optimized to prevent creating new lists every frame.
    /// </summary>
    public List<LevelDisplayData> GetLevelBatch(int startIndex, int count)
    {
        // Get the full list (which is already cached/optimized)
        List<LevelDisplayData> allData = GetLevelSelectData();

        if (startIndex < 0 || startIndex >= allData.Count)
            return new List<LevelDisplayData>();

        int actualCount = Mathf.Min(count, allData.Count - startIndex);

        // GetRange creates a new list, but only for the small 'batch' size (e.g., 10 nodes)
        return allData.GetRange(startIndex, actualCount);
    }

    /// <summary>
    /// Checks if there is a level after the current bookmark.
    /// </summary>
    public bool HasMoreContent()
    {
        if (string.IsNullOrEmpty(_saveData.CurrentLevelID))
        {
            return DataManager.Instance.Metadata.Levels.Count > 0;
        }

        return GetNextLevelInDatabase(_saveData.CurrentLevelID) != null;
    }
}
public struct LevelDisplayData
{
    public LevelData StaticData;
    public LevelProgress ProgressData;
    public bool IsUnlocked;
}