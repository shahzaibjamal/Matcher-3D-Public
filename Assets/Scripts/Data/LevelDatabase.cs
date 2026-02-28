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

                // 2. Calculate Gold Amount
                // Formula: Base + (Level * Step) multiplied by a slight curve
                float calculatedGold = (baseGold + (currentLevel * goldPerLevel)) * Mathf.Pow(currentLevel, difficultyCurve - 1);

                // Round to nearest 10 for "clean" looking numbers (e.g., 150 instead of 153)
                levels[i].GoldAmount = Mathf.RoundToInt(calculatedGold / 10f) * 10;

            }
        }
    }
}
