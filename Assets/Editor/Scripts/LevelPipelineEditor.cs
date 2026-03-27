using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class LevelPipelineEditor : Editor
{
    // --- TWEAKABLE SETTINGS ---
    private const int MinItems = 6;      // Level 1 total objects (e.g., 2 types * 3)
    private const int MaxItems = 90;     // Level 35 total objects
    private const int MinVariety = 2;    // Level 1 unique item types
    private const int MaxVariety = 12;   // Level 35 unique item types
    private const int MaxLevels = 35;

    [MenuItem("Tools/Pipeline/Generate and Export to Sheets")]
    public static void GenerateAndExport()
    {
        string metadataPath = Path.Combine(Application.dataPath, "Resources/metadata.json");
        string exportPath = Path.Combine(Application.dataPath, "LevelExport_ForSheets.txt");

        if (!File.Exists(metadataPath)) { Debug.LogError("metadata.json not found!"); return; }

        string json = File.ReadAllText(metadataPath);
        var data = JsonConvert.DeserializeObject<Metadata>(json);

        data.Levels = new List<LevelData>();
        for (int i = 1; i <= MaxLevels; i++)
        {
            data.Levels.Add(CalculateBudgetedLevel(i, data.Items));
        }

        File.WriteAllText(metadataPath, JsonConvert.SerializeObject(data, Formatting.Indented));

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Id\tNumber\tName\tItemsToSpawn\tItemQuantities\tItemsToCollect\tRewards\tRewardQuantities");

        foreach (var lv in data.Levels)
        {
            string spawnIds = "\"" + string.Join(",\n", lv.ItemsToSpawn.Select(x => x.Id)) + "\"";
            string spawnQtys = "\"" + string.Join(",\n", lv.ItemsToSpawn.Select(x => x.Count)) + "\"";
            string collectIds = "\"" + string.Join(",\n", lv.ItemsToCollect) + "\"";
            string rewardTypes = "\"" + string.Join(",\n", lv.Rewards.Select(x => x.RewardType.ToString())) + "\"";
            string rewardQtys = "\"" + string.Join(",\n", lv.Rewards.Select(x => x.Amount)) + "\"";

            sb.Append($"{lv.Id}\t{lv.Number}\t{lv.Name}\t{spawnIds}\t{spawnQtys}\t{collectIds}\t{rewardTypes}\t{rewardQtys}");
            sb.AppendLine();
        }

        File.WriteAllText(exportPath, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"<color=green>GENERATION COMPLETE:</color> Normalized scaling applied (Max Items: {MaxItems})");
    }

    private static LevelData CalculateBudgetedLevel(int lv, List<ItemData> master)
    {
        float t = (float)(lv - 1) / (MaxLevels - 1);

        // 1. STEPPED COLLECTIBLE LOGIC
        // Precise control: 1-5 (1), 6-10 (2), 11-20 (3), 21-40 (4), 41+ (5-6)
        int targetCollectables;
        if (lv <= 5) targetCollectables = 1;
        else if (lv <= 10) targetCollectables = 2;
        else if (lv <= 15) targetCollectables = 3;
        else if (lv <= 20) targetCollectables = 4;
        else targetCollectables = UnityEngine.Random.Range(5, 7); // Max spike

        // 2. PILE SIZE (Linear growth is fine for the "mess")
        int targetTotalItems = Mathf.RoundToInt(Mathf.Lerp(MinItems, MaxItems, t));
        int targetVariety = Mathf.RoundToInt(Mathf.Lerp(MinVariety, MaxVariety, Mathf.Clamp01(t * 1.5f)));

        targetTotalItems = (targetTotalItems / 3) * 3;

        LevelData data = new LevelData
        {
            Id = $"level_{lv:D2}",
            Number = lv,
            Name = $"Level {lv:D2}"
        };

        // 3. STRICT SIZE LOCKING
        List<ItemData> availablePool = master.Where(x => x.Enabled).ToList();

        // Level 1-5: ONLY Large items exist in the game.
        if (lv <= 5)
            availablePool = availablePool.Where(x => x.Size == ItemSize.Large).ToList();
        // Level 6-15: Large and Medium only.
        else if (lv <= 15)
            availablePool = availablePool.Where(x => x.Size != ItemSize.Small).ToList();
        // Level 16+: The "Toy Box" is fully open.

        // 4. SELECT VARIETY (Weighted to prefer Large as background clutter)
        List<ItemData> selectedPool = availablePool
            .OrderByDescending(x => GetSpawnWeight(x.Size, t) * UnityEngine.Random.value)
            .Take(targetVariety)
            .ToList();

        // 5. FILL SPAWNS
        int currentCount = 0;
        foreach (var item in selectedPool)
        {
            data.ItemsToSpawn.Add(new LevelItemEntry { Id = item.Id, Count = 3 });
            currentCount += 3;
        }

        int safety = 0;
        while (currentCount < targetTotalItems && safety < 500)
        {
            safety++;
            var randomEntry = data.ItemsToSpawn[UnityEngine.Random.Range(0, data.ItemsToSpawn.Count)];
            randomEntry.Count += 3;
            currentCount += 3;
        }

        // 6. ASSIGN COLLECTABLES (Size-Targeting)
        // Ensures the 1 target you find in Lv 1-5 is always a Large item.
        data.ItemsToCollect = data.ItemsToSpawn
            .Select(x => selectedPool.Find(p => p.Id == x.Id))
            .OrderByDescending(x => GetCollectableWeight(x.Size, t) * UnityEngine.Random.value)
            .Take(targetCollectables)
            .Select(x => x.Id)
            .ToList();

        // 7. REWARDS & POWERUPS
        int goldAmount = Mathf.FloorToInt(50 + (15 * Mathf.Sqrt(lv)));
        data.Rewards.Add(new RewardData { RewardType = RewardType.Gold, Amount = goldAmount });

        HandlePowerupRewards(lv, data);

        return data;
    }

    // Higher value = More likely to be in the level as clutter/background
    private static float GetSpawnWeight(ItemSize size, float t)
    {
        switch (size)
        {
            case ItemSize.Large: return 5.0f; // Always prefer Large to fill the screen
            case ItemSize.Medium: return 2.0f + (t * 3.0f);
            case ItemSize.Small: return 0.1f + (t * 5.0f);
            default: return 1.0f;
        }
    }

    // Higher value = More likely to be chosen as a REQUIRED target to find
    private static float GetCollectableWeight(ItemSize size, float t)
    {
        if (t < 0.2f) // Early Levels: Force Large targets
        {
            return (size == ItemSize.Large) ? 10f : 0.1f;
        }

        // Late Levels: Prefer Small/Medium as targets
        switch (size)
        {
            case ItemSize.Large: return 1.0f;
            case ItemSize.Medium: return 2.0f + t;
            case ItemSize.Small: return 5.0f * t;
            default: return 1.0f;
        }
    }
    private static void HandlePowerupRewards(int lv, LevelData data)
    {
        if (lv == 1) data.Rewards.Add(new RewardData { RewardType = RewardType.Undo, Amount = 5 });
        else if (lv == 2) data.Rewards.Add(new RewardData { RewardType = RewardType.Hint, Amount = 3 });
        else if (lv == 3) data.Rewards.Add(new RewardData { RewardType = RewardType.Shake, Amount = 2 });
        else if (lv == 4) data.Rewards.Add(new RewardData { RewardType = RewardType.Magnet, Amount = 2 });
        else if (lv % 5 == 0)
        {
            RewardType randomType = (RewardType)UnityEngine.Random.Range(2, 6);
            int amount = 1;
            switch (randomType)
            {
                case RewardType.Undo: amount = UnityEngine.Random.Range(1, 6); break;
                case RewardType.Hint: amount = UnityEngine.Random.Range(1, 4); break;
                case RewardType.Shake: amount = UnityEngine.Random.Range(1, 3); break;
                case RewardType.Magnet: amount = 1; break;
            }
            data.Rewards.Add(new RewardData { RewardType = randomType, Amount = amount });
        }
    }
}