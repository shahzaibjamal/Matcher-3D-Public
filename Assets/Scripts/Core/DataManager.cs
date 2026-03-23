using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("Data Source")]
    [SerializeField] private string fileName = "metadata";

    public Metadata Metadata { get; private set; }
    // This allows other scripts to 'await' the initialization
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
    }

    public async Task LoadMetadataAsync()
    {
        // 1. Still on Main Thread: Load the asset
        ResourceRequest request = Resources.LoadAsync<TextAsset>(fileName);
        while (!request.isDone) await Task.Yield();

        TextAsset jsonFile = request.asset as TextAsset;

        if (jsonFile == null)
        {
            Debug.LogError($"[DataManager] {fileName}.json not found!");
            return;
        }

        // 2. Still on Main Thread: Grab the string data
        // This is the "get_bytes" part that was causing the error
        string rawJson = jsonFile.text;

        try
        {
            // 3. Move to Background Thread: Heavy Parsing
            // We pass the string 'rawJson', NOT the 'jsonFile' object
            Metadata = await Task.Run(() => JsonConvert.DeserializeObject<Metadata>(rawJson));

            // 4. Back on Main Thread: Finalize
            InitializeCaches();
            Debug.Log("[DataManager] Async Load Complete.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DataManager] Error: {e.Message}");
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