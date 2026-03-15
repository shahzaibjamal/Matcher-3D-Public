using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("Data Source")]
    [SerializeField] private string fileName = "metadata";

    public Metadata Metadata { get; private set; }

    private Dictionary<string, ItemData> _itemCache = new Dictionary<string, ItemData>();
    private Dictionary<string, LevelData> _levelCache = new Dictionary<string, LevelData>();
    private Dictionary<int, DailyRewardData> _dailyRewardCache = new Dictionary<int, DailyRewardData>();

    // NEW: Cache for Spin Wheel rewards
    private Dictionary<int, RewardData> _spinWheelCache = new Dictionary<int, RewardData>();
    private Dictionary<string, StoreItemData> _storeCache = new Dictionary<string, StoreItemData>();

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
            Debug.Log($"[DataManager] Loaded {Metadata.Levels.Count} levels and {Metadata.SpinWheelRewards.Count} spin rewards.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataManager] Error parsing JSON: {e.Message}");
        }
    }

    private void InitializeCaches()
    {
        _itemCache = Metadata.Items?.ToDictionary(item => item.Id) ?? new Dictionary<string, ItemData>();
        _levelCache = Metadata.Levels?.ToDictionary(lvl => lvl.Id) ?? new Dictionary<string, LevelData>();
        _dailyRewardCache = Metadata.DailyRewards?.ToDictionary(d => d.Day) ?? new Dictionary<int, DailyRewardData>();

        // NEW: Initialize Spin Wheel cache
        _spinWheelCache = Metadata.SpinWheelRewards?
            .ToDictionary(sw => sw.ID, sw => sw.Reward) ?? new Dictionary<int, RewardData>();
        _storeCache = Metadata.StoreItems?.ToDictionary(s => s.Id) ?? new Dictionary<string, StoreItemData>();
    }

    // --- NEW HELPER FUNCTIONS ---

    /// <summary> Gets the reward data for a specific spin wheel segment ID </summary>
    public RewardData GetSpinWheelReward(int id)
    {
        if (_spinWheelCache.TryGetValue(id, out var reward)) return reward;
        Debug.LogWarning($"[DataManager] SpinWheel ID '{id}' not found.");
        return null;
    }

    /// <summary> Returns all available spin wheel rewards for UI generation </summary>
    public List<SpinWheelData> GetAllSpinWheelRewards()
    {
        return Metadata.SpinWheelRewards;
    }

    // --- Existing Helper Functions ---

    public ItemData GetItemByID(string id)
    {
        if (_itemCache.TryGetValue(id, out var item)) return item;
        return null;
    }

    public LevelData GetLevelByID(string id)
    {
        if (_levelCache.TryGetValue(id, out var lvl)) return lvl;
        return null;
    }

    public LevelData GetLevelByNumber(int number)
    {
        return Metadata.Levels.FirstOrDefault(l => l.Number == number);
    }

    public DailyRewardData GetDailyReward(int day)
    {
        if (_dailyRewardCache.TryGetValue(day, out var data)) return data;
        return null;
    }

    public MapThemeData GetThemeByLevelNumber(int levelNumber)
    {
        return Metadata.MapThemes
            .Where(t => t.StartLevel <= levelNumber)
            .OrderByDescending(t => t.StartLevel)
            .FirstOrDefault() ?? Metadata.MapThemes.FirstOrDefault();
    }
    public StoreItemData GetStoreItemByID(string id)
    {
        if (_storeCache.TryGetValue(id, out var item)) return item;
        return null;
    }

    public List<StoreItemData> GetStoreByCategory(StoreItemCategory category)
    {
        return Metadata.StoreItems.Where(s => s.Category == category).ToList();
    }
}