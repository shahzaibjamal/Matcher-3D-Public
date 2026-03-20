using System;
using System.Collections.Generic;


[Serializable]
public class Metadata
{
    // Initialize lists to prevent null returns
    public GameConfig Settings; // This maps to "Settings" in your JSON  
    public List<ItemData> Items = new List<ItemData>();
    public List<LevelData> Levels = new List<LevelData>();
    public List<DailyRewardData> DailyRewards = new List<DailyRewardData>();
    public List<MapThemeData> MapThemes = new List<MapThemeData>(); // New List
    public List<SpinWheelData> SpinWheelRewards = new List<SpinWheelData>();
    public List<StoreItemData> StoreItems = new List<StoreItemData>(); // New
}

