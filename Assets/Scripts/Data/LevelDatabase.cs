using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Tooltip("List of all levels in the game")]
    public List<LevelData> levels;

    public LevelData GetLevelByUID(string uid)
    {
        return levels.Find(l => l.levelUID == uid);
    }
}
