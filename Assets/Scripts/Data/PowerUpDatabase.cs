using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PowerUpVisualMapping
{
    public PowerUpType Type;
    public Sprite Icon;

    public Color Color;
}

// You would create this as a separate file, but here is the structure:
[CreateAssetMenu(fileName = "PowerUpVisualDatabase", menuName = "Game/PowerUp Database")]
public class PowerUpVisualDatabase : ScriptableObject
{
    public List<PowerUpVisualMapping> mappings;

    public Sprite GetIcon(PowerUpType type)
    {
        var mapping = mappings.Find(m => m.Type == type);
        return mapping.Icon;
    }
}