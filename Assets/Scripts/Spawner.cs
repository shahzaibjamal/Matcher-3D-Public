using System;
using UnityEngine;

public partial class Spawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform Parent;
    public float SpawnRadius = 2f;   // radius around parent to scatter items
    public float VerticalOffset = 0.5f; // slight lift so they don't overlap ground

    public string LevelUid = "level_01";
    public event Action<LevelData> OnLevelInitialized;

    public void SpawnLevel()
    {
        Cleanup();
        // SpawnLevel(LevelUid);
    }

    public void SpawnLevel(string levelUID, Action<ItemData, Transform> onItemClicked)
    {
        LevelData level = Metadata.Instance.levelDatabase.GetLevelByUID(levelUID);
        if (level == null)
        {
            Debug.LogError("Level not found: " + levelUID);
            return;
        }

        foreach (var entry in level.itemsToSpawn)
        {
            ItemData item = Metadata.Instance.itemDatabase.GetItemByUID(entry.itemUID);
            if (item == null)
            {
                Debug.LogWarning("Item not found in database: " + entry.itemUID);
                continue;
            }

            for (int i = 0; i < entry.count; i++)
            {
                // Pick a random position around Parent within a circle
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * SpawnRadius;
                Vector3 spawnPos = Parent.position + new Vector3(randomCircle.x, VerticalOffset, randomCircle.y);

                GameObject go = Instantiate(item.Prefab, spawnPos, Quaternion.identity, Parent);

                ClickableItem clickable = go.GetComponent<ClickableItem>();
                if (clickable != null)
                {
                    clickable.ItemData = item;
                    clickable.OnItemClicked = onItemClicked;
                }

                // Optional: give each item a slight random rotation so they look tossed
                go.transform.rotation = Quaternion.Euler(
                    UnityEngine.Random.Range(-15f, 15f),
                    UnityEngine.Random.Range(0f, 360f),
                    UnityEngine.Random.Range(-15f, 15f)
                );
            }
        }

        OnLevelInitialized?.Invoke(level);
    }

    public void Cleanup()
    {
        for (int i = Parent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(Parent.GetChild(i).gameObject);
        }
    }
}
