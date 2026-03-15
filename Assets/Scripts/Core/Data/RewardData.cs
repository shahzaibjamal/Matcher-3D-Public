using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public class RewardData
{
    [JsonConverter(typeof(StringEnumConverter))]
    public RewardType RewardType;
    public int Amount;
}

[Serializable]
public class DailyRewardData
{
    public int Day;
    // Changed from single RewardType/Amount to a List
    public List<RewardData> Rewards;
}
public enum RewardType
{
    Gold = 1,
    Magnet = 2,
    Hint = 3,

    Shake = 4,

    Undo = 5,
    Heart = 6
}