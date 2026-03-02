using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Game/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Tooltip("List of all levels in the game")]
    public List<LevelData> levels;
    public int baseGold = 100;
    public int goldPerLevel = 50;
    [Range(1f, 2f)] public float difficultyCurve = 1.1f;

    [Header("World Themes")]
    public List<MapTheme> mapThemes; // Add this here!


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
    private void OnValidate()
    {
        if (levels == null) return;

        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] != null)
            {
                // 1. Sync Level Number
                int currentLevel = i + 1;
                levels[i].LevelNumber = currentLevel;
            }
        }
    }

    /// <summary>
    /// Finds the theme for a specific level index.
    /// </summary>
    public MapTheme GetThemeByMapIndex(int mapIndex)
    {
        if (mapThemes == null || mapThemes.Count == 0) return null;

        // Use modulo (%) so if you have 3 themes but 10 maps, 
        // it loops: 0, 1, 2, 0, 1, 2...
        int index = mapIndex % mapThemes.Count;
        return mapThemes[index];
    }
}
