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
    private const int MinItems = 12;
    private const int MaxItems = 48; // Updated to your preferred cap
    private const int MinVariety = 2;
    private const int MaxVariety = 10;
    private const int MaxLevels = 35;
    private const int DifficultyPivot = 15;
    private const int MaxCollectibles = 7;

    // --- THE SLIDER (0-10) ---
    // 0 = Very Easy (Mostly Large items, easy targets)
    // 5 = Balanced (Your current logic)
    // 10 = Max Difficulty (Aggressive Small items, tiny targets)
    private const float DifficultyScalar = 10.0f;

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

        Debug.Log($"<color=green>PIPELINE COMPLETE:</color> Difficulty Scalar: {DifficultyScalar}/10. Max Items: {MaxItems}.");
    }

    private static LevelData CalculateBudgetedLevel(int lv, List<ItemData> master)
    {
        float t;
        if (lv <= DifficultyPivot)
        {
            float pivotT = (float)(lv - 1) / (DifficultyPivot - 1);
            t = Mathf.Pow(pivotT, 2.0f);
        }
        else
        {
            t = UnityEngine.Random.Range(0.9f, 1.0f);
        }

        // Apply DifficultyScalar to the progression 't' (Normalizes 0-10 to 0-2 range modifier)
        float diffMod = DifficultyScalar / 5.0f;
        float weightedT = Mathf.Clamp01(t * diffMod);

        int targetCollectables = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(3, 6, weightedT)), 3, 6);
        if (lv % 5 == 0) targetCollectables = Mathf.Min(targetCollectables + 1, MaxCollectibles);

        int targetTotalItems = Mathf.RoundToInt(Mathf.Lerp(MinItems, MaxItems, weightedT));
        targetTotalItems = (targetTotalItems / 3) * 3;

        int targetVariety = Mathf.RoundToInt(Mathf.Lerp(MinVariety, MaxVariety, weightedT));

        LevelData data = new LevelData
        {
            Id = $"level_{lv:D2}",
            Number = lv,
            Name = (lv % 5 == 0) ? $"CHALLENGE {lv:D2}" : $"Level {lv:D2}"
        };

        List<ItemData> availablePool = master.Where(x => x.Enabled).ToList();

        // The Scalar is passed into weights to favor Small vs Large
        List<ItemData> selectedPool = availablePool
            .OrderByDescending(x => GetSpawnWeight(x.Size, t, DifficultyScalar) * UnityEngine.Random.value)
            .Take(targetVariety)
            .ToList();

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

        data.ItemsToCollect = data.ItemsToSpawn
            .Select(x => selectedPool.Find(p => p.Id == x.Id))
            .OrderByDescending(x => GetCollectableWeight(x.Size, t, DifficultyScalar) * UnityEngine.Random.value)
            .Take(targetCollectables)
            .Select(x => x.Id)
            .ToList();

        int gold = Mathf.FloorToInt(50 + (25 * Mathf.Pow(lv, 0.7f)));
        if (lv % 5 == 0) gold = Mathf.RoundToInt(gold * 1.5f);
        data.Rewards.Add(new RewardData { RewardType = RewardType.Gold, Amount = gold });

        HandlePowerupRewards(lv, data);

        return data;
    }

    private static void HandlePowerupRewards(int lv, LevelData data)
    {
        if (lv == 1) data.Rewards.Add(new RewardData { RewardType = RewardType.Undo, Amount = 6 });
        else if (lv == 2) data.Rewards.Add(new RewardData { RewardType = RewardType.Hint, Amount = 3 });
        else if (lv == 3) data.Rewards.Add(new RewardData { RewardType = RewardType.Shake, Amount = 3 });
        else if (lv == 4) data.Rewards.Add(new RewardData { RewardType = RewardType.Magnet, Amount = 3 });
        else if (lv % 5 == 0)
        {
            RewardType randomType = (RewardType)UnityEngine.Random.Range(2, 6);
            int amount = 1;
            switch (randomType)
            {
                case RewardType.Undo: amount = UnityEngine.Random.Range(1, 4); break;
                case RewardType.Hint: amount = UnityEngine.Random.Range(1, 3); break;
                default: amount = 1; break;
            }
            data.Rewards.Add(new RewardData { RewardType = randomType, Amount = amount });
        }
    }

    private static float GetSpawnWeight(ItemSize size, float t, float scalar)
    {
        // Adjusts weights based on 0-10 scalar. 
        // Higher scalar = Lower weight for Large, Higher weight for Small.
        float sMod = scalar / 5.0f;

        switch (size)
        {
            case ItemSize.Large: return Mathf.Lerp(6.0f, 2.5f / sMod, t);
            case ItemSize.Medium: return 3.0f + (t * 2.0f);
            case ItemSize.Small: return 1.0f + (t * 5.0f * sMod);
            default: return 1.0f;
        }
    }

    private static float GetCollectableWeight(ItemSize size, float t, float scalar)
    {
        float sMod = scalar / 5.0f;

        switch (size)
        {
            case ItemSize.Large: return 0.5f / sMod;
            case ItemSize.Medium: return 3.0f;
            case ItemSize.Small: return 4.0f + (t * 4.0f * sMod);
            default: return 1.0f;
        }
    }
}