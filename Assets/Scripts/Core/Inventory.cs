using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class Inventory
{
    public Dictionary<PowerUpType, int> PowerUps = new Dictionary<PowerUpType, int>();

    [JsonProperty] public int Gold { get; private set; }
    [JsonProperty] public int CurrentLivesRaw { get; set; } = 5;
    public int Stars;

    public bool TryUpdateGoldAmount(int delta)
    {
        if (Gold + delta < 0) return false;
        Gold += delta;
        GameEvents.OnGoldUpdatedEvent?.Invoke(Gold);
        return true;
    }

    public void AddLives(int amount, int maxLimit)
    {
        CurrentLivesRaw = Mathf.Min(maxLimit, CurrentLivesRaw + amount);
        GameEvents.OnLivesChanged?.Invoke();
    }

    public void ConsumeLife()
    {
        if (CurrentLivesRaw > 0)
        {
            CurrentLivesRaw--;
            GameEvents.OnLivesChanged?.Invoke();
        }
    }

    public int GetPowerUpCount(PowerUpType type)
    {
        return PowerUps.ContainsKey(type) ? PowerUps[type] : 0;
    }
    public void AddPowerUp(PowerUpType type, int amount)
    {
        if (PowerUps.ContainsKey(type)) PowerUps[type] += amount;
        else PowerUps[type] = amount;
    }

    public void AddRewards(List<RewardData> rewards)
    {
        if (rewards == null) return;
        foreach (var reward in rewards)
        {
            if (reward.RewardType == RewardType.Gold)
                TryUpdateGoldAmount(reward.Amount);
            else if (reward.RewardType == RewardType.Heart)
                AddLives(reward.Amount, GameSaveData.MAX_LIVES);
            else
                AddPowerUp(ConvertToPowerUpType(reward.RewardType), reward.Amount);
        }
    }

    private PowerUpType ConvertToPowerUpType(RewardType rewardType)
    {
        return rewardType switch
        {
            RewardType.Hint => PowerUpType.Hint,
            RewardType.Magnet => PowerUpType.Magnet,
            RewardType.Shake => PowerUpType.Shake,
            RewardType.Undo => PowerUpType.Undo,
            _ => throw new ArgumentException("Invalid PowerUp Type")
        };
    }
}