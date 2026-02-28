using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
}