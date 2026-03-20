using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class Inventory
{
    // Dictionary to store powerup counts. 
    // Key: PowerUpType (Enum), Value: Quantity (int)
    public Dictionary<PowerUpType, int> PowerUps = new Dictionary<PowerUpType, int>();

    [JsonProperty]
    public int Gold { get; private set; }
    public int Stars;


    /// <summary>
    /// Updates gold by a delta. Returns false if result would be negative.
    /// </summary>
    public bool TryUpdateGoldAmount(int delta)
    {
        if (Gold + delta < 0)
        {
            return false; // Insufficient gold
        }

        Gold += delta;
        EventDebugger.LogSubscribers(GameEvents.OnGoldUpdatedEvent, "Gold Event");

        // This is where you'd likely trigger an event like:
        GameEvents.OnGoldUpdatedEvent(Gold);

        return true;
    }
    public int GetPowerUpCount(PowerUpType type)
    {
        return PowerUps.ContainsKey(type) ? PowerUps[type] : 0;
    }

    public void AddPowerUp(PowerUpType type, int amount)
    {
        if (PowerUps.ContainsKey(type))
            PowerUps[type] += amount;
        else
            PowerUps[type] = amount;
    }
    public void AddPowerUp(RewardType type, int amount)
    {
        PowerUpType pType = ConvertToPowerUpType(type);
        if (PowerUps.ContainsKey(pType))
            PowerUps[pType] += amount;
        else
            PowerUps[pType] = amount;
    }

    public void AddRewards(List<RewardData> rewards)
    {
        if (rewards == null) return;

        foreach (var reward in rewards)
        {
            if (reward.RewardType == RewardType.Gold)
            {
                // We use the existing logic to update gold
                TryUpdateGoldAmount(reward.Amount);
            }
            else if (reward.RewardType == RewardType.Heart)
            {

            }
            else
            {
                // Convert RewardType to PowerUpType
                PowerUpType pType = ConvertToPowerUpType(reward.RewardType);

                // If the reward is actually a powerup (not None), add it
                AddPowerUp(pType, reward.Amount);
            }
        }
    }

    /// <summary>
    /// Helper to map RewardType enum to PowerUpType enum.
    /// </summary>
    private PowerUpType ConvertToPowerUpType(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Hint => PowerUpType.Hint,
            RewardType.Magnet => PowerUpType.Magnet,
            RewardType.Shake => PowerUpType.Shake,
            RewardType.Undo => PowerUpType.Undo,
            RewardType.Gold => throw new NotImplementedException(),
        };
    }
}