using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Tooltip("List of all levels in the game")]
    public List<LevelData> levels;

    public LevelData GetLevelByUID(string uid)
    {
        var level = levels.Find(l => l.levelUID == uid);
        if (level != null)
        {
            foreach (var entry in level.itemsToSpawn)
            {
                if (entry.count % 3 != 0)
                {
                    entry.count = (entry.count / 3) * 3; // round down
                }
            }
        }
        return level;
    }
}
