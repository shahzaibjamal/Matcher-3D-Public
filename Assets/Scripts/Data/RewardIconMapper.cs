using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "RewardIconMapper", menuName = "Game/Icon Mapper")]
public class RewardIconMapper : ScriptableObject
{
    [System.Serializable]
    public struct RewardMapping
    {
        public RewardType type;
        public Sprite icon;
    }

    public List<RewardMapping> mappings;

    public Sprite GetIcon(RewardType type)
    {
        var mapping = mappings.FirstOrDefault(m => m.type == type);
        return mapping.icon != null ? mapping.icon : null;
    }
}