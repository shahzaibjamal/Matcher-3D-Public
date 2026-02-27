using System;
using System.Collections.Generic;

[Serializable]
public class Inventory
{
    // Dictionary to store powerup counts. 
    // Key: PowerUpType (Enum), Value: Quantity (int)
    public Dictionary<PowerUpType, int> PowerUps = new Dictionary<PowerUpType, int>();

    public int Gold;
    public int Stars;

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