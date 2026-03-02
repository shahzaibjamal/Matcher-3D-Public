using System;
using System.Collections.Generic;

[Serializable]
public class LevelData
{
    public string Id;
    public int Number;
    public string Name;
    public List<LevelItemEntry> ItemsToSpawn = new List<LevelItemEntry>();
    public List<string> ItemsToCollect = new List<string>();
    public List<RewardData> Rewards = new List<RewardData>();
}