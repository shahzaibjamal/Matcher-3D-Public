using System;
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
    [JsonConverter(typeof(StringEnumConverter))]
    public RewardType RewardType;
    public int Amount;
}
public enum RewardType
{
    Gold = 1,
    Magnet = 2,
    Hint = 3,

    Shake = 4,

    Undo = 5
}