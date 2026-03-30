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
        // t is 0.0 to 1.0. 
        float t = (float)(lv - 1) / (MaxLevels - 1);

        // EXPONENTIAL CURVE: (t^0.7) makes it get harder FASTER in the beginning.
        // Use t^2 if you want it to start easy and get hard only at the end.
        float heatCurve = Mathf.Pow(t, 0.7f);

        // 1. DYNAMIC COLLECTIBLE COUNT
        int targetCollectables;
        if (lv % 5 == 0) targetCollectables = Mathf.Clamp(Mathf.CeilToInt(t * 8) + 3, 3, 8); // Spike on 5s
        else if (lv <= 5) targetCollectables = 3;
        else if (lv <= 15) targetCollectables = 4;
        else targetCollectables = 5;

        // 2. EXPONENTIAL PILE SIZE
        // We add a "Spike" multiplier for every 5th level
        float spikeMultiplier = (lv % 5 == 0) ? 1.3f : 1.0f;
        int targetTotalItems = Mathf.RoundToInt(Mathf.Lerp(MinItems, MaxItems, heatCurve) * spikeMultiplier);

        // Variety grows quickly to create visual mess
        int targetVariety = Mathf.RoundToInt(Mathf.Lerp(MinVariety, MaxVariety, Mathf.Sqrt(t)));

        // Ensure it's a multiple of 3 for match-3 logic
        targetTotalItems = (targetTotalItems / 3) * 3;

        LevelData data = new LevelData
        {
            Id = $"level_{lv:D2}",
            Number = lv,
            Name = (lv % 5 == 0) ? $"CHALLENGE {lv:D2}" : $"Level {lv:D2}"
        };

        // 3. RELAXED SIZE LOCKING
        // We now allow Small items as "clutter" from Level 3 onwards, 
        // but they might not be "Collectables" yet.
        List<ItemData> availablePool = master.Where(x => x.Enabled).ToList();
        if (lv < 3)
            availablePool = availablePool.Where(x => x.Size != ItemSize.Small).ToList();

        // 4. SELECT VARIETY (Weighted)
        List<ItemData> selectedPool = availablePool
            .OrderByDescending(x => GetSpawnWeight(x.Size, t, lv % 5 == 0) * UnityEngine.Random.value)
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

        // 6. ASSIGN COLLECTABLES (Aggressive Small-Item Targeting)
        data.ItemsToCollect = data.ItemsToSpawn
            .Select(x => selectedPool.Find(p => p.Id == x.Id))
            .OrderByDescending(x => GetCollectableWeight(x.Size, t, lv % 5 == 0) * UnityEngine.Random.value)
            .Take(targetCollectables)
            .Select(x => x.Id)
            .ToList();

        // 7. REWARDS
        int goldAmount = Mathf.FloorToInt(50 + (25 * Mathf.Pow(lv, 0.6f)));
        if (lv % 5 == 0) goldAmount = Mathf.RoundToInt(goldAmount * 1.5f); // Bonus gold for spikes

        data.Rewards.Add(new RewardData { RewardType = RewardType.Gold, Amount = goldAmount });
        HandlePowerupRewards(lv, data);

        return data;
    }
    private static float GetSpawnWeight(ItemSize size, float t, bool isSpikeLevel)
    {
        // On Spike levels, we flood the screen with small/medium items to create a mess
        float spikeBonus = isSpikeLevel ? 2.0f : 1.0f;

        switch (size)
        {
            case ItemSize.Large: return 3.0f; // Baseline clutter
            case ItemSize.Medium: return 2.0f + (t * 4.0f * spikeBonus);
            case ItemSize.Small: return 0.5f + (t * 8.0f * spikeBonus); // Small items increase rapidly
            default: return 1.0f;
        }
    }

    private static float GetCollectableWeight(ItemSize size, float t, bool isSpikeLevel)
    {
        // We want to force the user to find SMALL items as the level target
        // because they are harder to see under the large items.
        float weight = 1.0f;

        switch (size)
        {
            case ItemSize.Large: weight = 1.0f; break;
            case ItemSize.Medium: weight = 3.0f + (t * 2.0f); break;
            case ItemSize.Small: weight = 5.0f + (t * 10.0f); break; // High priority for targets
        }

        // If it's a spike level, aggressively target the smallest items
        if (isSpikeLevel && size == ItemSize.Small) weight *= 3.0f;

        return weight;
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