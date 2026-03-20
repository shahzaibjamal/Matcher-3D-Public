using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "IAPIconMapper", menuName = "Game/IAP Icon Mapper")]
public class IAPIconMapper : ScriptableObject
{
    [System.Serializable]
    public struct IAPMapping
    {
        public string Name;
        public Sprite Icon;
    }

    public List<IAPMapping> mappings;

    public Sprite GetIcon(string name)
    {
        var mapping = mappings.FirstOrDefault(m => m.Name == name);
        return mapping.Icon != null ? mapping.Icon : null;
    }
}