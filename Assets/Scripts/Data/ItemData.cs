using UnityEngine;

[System.Serializable]
public class ItemData
{
    [Header("Identification")]
    [SerializeField] private string uid;       // Unique identifier
    [SerializeField] private string itemName;  // Display name

    [Header("References")]
    [SerializeField] private GameObject prefab; // 3D prefab reference
    [SerializeField] private Sprite uiSprite;   // UI icon

    // Properties for safe access
    public string UID => uid;
    public string ItemName => itemName;
    public GameObject Prefab => prefab;
    public Sprite UISprite => uiSprite;
}
