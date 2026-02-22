using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Spawner : MonoBehaviour
{
    [Header("References")]
    public Transform Parent;
    public Camera MainCamera => Camera.main;

    [Header("Debug Settings")]
    public bool isDebug = false;
    public Material debugMaterial; // Assign a semi-transparent material

    [Header("Spawn Area Settings")]
    [Range(0.1f, 0.9f)] public float WidthPercent = 0.75f;
    [Range(0.1f, 0.9f)] public float HeightPercent = 0.5f;
    public float WallHeight = 2.0f; // How tall the invisible walls are
    public float VerticalOffset = 0.1f; // Ground level

    private float _spawnXMax;
    private float _spawnZMax;
    private List<ClickableItem> _itemClickables = new();
    private Action<ItemData, Transform> _onItemClicked;

    public void GenerateBounds()
    {
        // 1. Calculate world bounds based on camera
        float distanceToCamera = Math.Abs(MainCamera.transform.position.y - Parent.position.y);
        float frustumHeight = 2.0f * distanceToCamera * Mathf.Tan(MainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * MainCamera.aspect;

        _spawnXMax = (frustumWidth * WidthPercent) / 2f;
        _spawnZMax = (frustumHeight * HeightPercent) / 2f;

        // 2. Clear old walls if they exist
        Transform oldContainer = Parent.Find("LevelContainer");
        if (oldContainer != null) DestroyImmediate(oldContainer.gameObject);

        // 3. Create Container Parent
        GameObject container = new GameObject("LevelContainer");
        container.transform.SetParent(Parent);
        container.transform.localPosition = Vector3.zero;

        // 4. Spawn 4 Walls
        SpawnWall("Wall_Left", new Vector3(-_spawnXMax, WallHeight / 2, 0), new Vector3(0.1f, WallHeight, _spawnZMax * 2), container.transform);
        SpawnWall("Wall_Right", new Vector3(_spawnXMax, WallHeight / 2, 0), new Vector3(0.1f, WallHeight, _spawnZMax * 2), container.transform);
        SpawnWall("Wall_Top", new Vector3(0, WallHeight / 2, _spawnZMax), new Vector3(_spawnXMax * 2, WallHeight, 0.1f), container.transform);
        SpawnWall("Wall_Bottom", new Vector3(0, WallHeight / 2, -_spawnZMax), new Vector3(_spawnXMax * 2, WallHeight, 0.1f), container.transform);
    }

    private void SpawnWall(string wallName, Vector3 localPos, Vector3 size, Transform parent)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = localPos;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;

        if (isDebug)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "DebugVisual";
            visual.transform.SetParent(wall.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = size;

            if (debugMaterial != null)
                visual.GetComponent<Renderer>().material = debugMaterial;

            // Remove collider from the visual child so it doesn't double-up
            DestroyImmediate(visual.GetComponent<BoxCollider>());
        }
    }

    public void SpawnLevel(string levelUID, Action<ItemData, Transform> onItemClicked)
    {
        _onItemClicked = onItemClicked;
        // Always ensure bounds match the current resolution before spawning
        GenerateBounds();

        LevelData level = Metadata.Instance.levelDatabase.GetLevelByUID(levelUID);
        if (level == null) return;

        foreach (var entry in level.itemsToSpawn)
        {
            ItemData item = Metadata.Instance.itemDatabase.GetItemByUID(entry.itemUID);
            for (int i = 0; i < entry.count; i++)
            {
                // Toss items inside the generated bounds
                float x = UnityEngine.Random.Range(-_spawnXMax + 0.5f, _spawnXMax - 0.5f);
                float z = UnityEngine.Random.Range(-_spawnZMax + 0.5f, _spawnZMax - 0.5f);

                Vector3 spawnPos = Parent.position + new Vector3(x, VerticalOffset, z);
                GameObject go = Instantiate(item.Prefab, spawnPos, Quaternion.identity, Parent);

                // Setup Clickable logic...
                ClickableItem clickable = go.GetComponent<ClickableItem>();
                if (clickable != null)
                {
                    _itemClickables.Add(clickable);
                    clickable.ItemData = item;
                    clickable.OnItemClicked = onItemClicked;
                }
            }
        }

    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (_itemClickables == null || _itemClickables.Count == 0)
            {
                Debug.Log("No items left to click.");
                return;
            }
            _itemClickables.RemoveAll(i => i == null || i.ItemData == null);

            int index = UnityEngine.Random.Range(0, _itemClickables.Count);
            var item = _itemClickables[index];

            if (item != null && item.ItemData != null)
            {
                _onItemClicked?.Invoke(item.ItemData, item.transform);
            }
            else
            {
                Debug.LogWarning("Item was null or destroyed.");
            }

            _itemClickables.RemoveAt(index);
        }
    }

}