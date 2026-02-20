using System;
using UnityEngine;

[Serializable]
public class LevelItemEntry
{
    [Tooltip("UID of the item to spawn")]
    public string itemUID;

    [Tooltip("How many of this item to spawn")]
    public int count;
}
