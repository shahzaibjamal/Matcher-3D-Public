using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public class Metadata
{
    // Initialize lists to prevent null returns
    public List<ItemData> Items = new List<ItemData>();
    public List<LevelData> Levels = new List<LevelData>();
    public List<DailyRewardData> DailyRewards = new List<DailyRewardData>();
    public List<MapThemeData> MapThemes = new List<MapThemeData>(); // New List
}

