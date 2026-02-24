using UnityEngine;
using System.Linq;
using System;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private LevelDatabase levelDatabase;
    private GameSaveData _saveData; // Reference shared from GameManager

    public void Initialize(GameSaveData data)
    {
        _saveData = data;
    }
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // --- Retrieval Logic ---

    public LevelData GetNextLevel()
    {
        // LevelManager uses the SaveData reference to figure out what comes next
        string currentUid = _saveData.CurrentLevelUID;

        int index = levelDatabase.levels.FindIndex(l => l.levelUID == currentUid);
        if (index < levelDatabase.levels.Count - 1)
        {
            return levelDatabase.levels[index + 1];
        }
        return null;
    }
    // --- Persistence Logic ---

    public void MarkLevelComplete(string uid, float timeTaken, int score)
    {
        // 1. Find or create the progress entry
        var progress = _saveData.LevelHistory.Find(p => p.LevelUID == _saveData.CurrentLevelUID);
        if (progress == null)
        {
            progress = new LevelProgress { LevelUID = uid };
            _saveData.LevelHistory.Add(progress);
        }

        // 2. Update stats
        progress.IsCompleted = true;
        progress.LastPlayed = DateTime.Now;
        if (timeTaken < progress.BestTime || progress.BestTime == 0) progress.BestTime = timeTaken;
        if (score > progress.HighScore) progress.HighScore = score;

        // 3. Update "Current Level" to the NEXT one in the SO
        var nextLevel = GetNextLevelInDatabase(uid);
        if (nextLevel != null)
        {
            _saveData.CurrentLevelUID = nextLevel.levelUID;
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
        return null; // No more levels
    }
}