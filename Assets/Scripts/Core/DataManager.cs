using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("Data Source")]
    [SerializeField] private string fileName = "metadata";

    // The deserialized JSON data
    public Metadata Metadata { get; private set; }

    // Fast lookup caches
    private Dictionary<string, ItemData> _itemCache = new Dictionary<string, ItemData>();
    private Dictionary<string, LevelData> _levelCache = new Dictionary<string, LevelData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadMetadata();
    }

    public void LoadMetadata()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(fileName);

        if (jsonFile == null)
        {
            Debug.LogError($"[DataManager] {fileName}.json not found in Resources!");
            return;
        }

        try
        {
            Metadata = JsonConvert.DeserializeObject<Metadata>(jsonFile.text);
            InitializeCaches();
            Debug.Log($"[DataManager] Successfully loaded {Metadata.Levels.Count} levels and {Metadata.Items.Count} items.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataManager] Error parsing JSON: {e.Message}");
        }
    }

    private void InitializeCaches()
    {
        _itemCache = Metadata.Items.ToDictionary(item => item.Id);
        _levelCache = Metadata.Levels.ToDictionary(lvl => lvl.Id);
    }

    // --- Helper Functions ---

    /// <summary> Gets item data by its string ID (e.g., "apple_01") </summary>
    public ItemData GetItemByID(string id)
    {
        if (_itemCache.TryGetValue(id, out var item)) return item;
        Debug.LogWarning($"[DataManager] Item ID '{id}' not found.");
        return null;
    }

    /// <summary> Gets level data by its string ID (e.g., "lvl_101") </summary>
    public LevelData GetLevelByID(string id)
    {
        if (_levelCache.TryGetValue(id, out var lvl)) return lvl;
        Debug.LogWarning($"[DataManager] Level ID '{id}' not found.");
        return null;
    }

    /// <summary> Gets level data by its Level Number (sequential) </summary>
    public LevelData GetLevelByNumber(int number)
    {
        return Metadata.Levels.FirstOrDefault(l => l.Number == number);
    }

    /// <summary> Returns all rewards for a specific day </summary>
    public DailyRewardData GetDailyReward(int day)
    {
        return Metadata.DailyRewards.FirstOrDefault(d => d.Day == day);
    }

    public LevelData GetDefaultLevel()
    {
        return Metadata.Levels.FirstOrDefault();
    }
    public MapThemeData GetThemeByLevelNumber(int levelNumber)
    {
        // Find themes where StartLevel <= levelNumber, then take the one with the highest StartLevel
        return Metadata.MapThemes
            .Where(t => t.StartLevel <= levelNumber)
            .OrderByDescending(t => t.StartLevel)
            .FirstOrDefault() ?? Metadata.MapThemes.FirstOrDefault(); // Fallback to first theme
    }
}