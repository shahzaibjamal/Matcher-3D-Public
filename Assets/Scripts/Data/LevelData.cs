using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelData
{
    [Header("Identification")]
    public string levelUID;   // unique level identifier
    public string levelName;  // display name

    [Header("Items to Spawn")]
    public List<LevelItemEntry> itemsToSpawn;
    [Header("Items to Spawn")]
    public List<string> itemsToCollect;
}
