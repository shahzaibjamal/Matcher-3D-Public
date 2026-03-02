using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapChunk : MonoBehaviour
{
    public RectTransform rectTransform;
    public MapPath path; // The waypoints for this specific map segment
    private List<LevelNode> _activeNodes = new List<LevelNode>();
    // Inside MapChunk.cs
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image topFog;
    [SerializeField] private Image bottomFog;

    // Call this when the chunk is "Recycled" to the top or bottom
    public void Configure(List<LevelDisplayData> levelBatch, int startLevelIndex, GameObject prefab, Sprite bg, Color themeColor)
    {
        // Clear old nodes
        foreach (Transform child in transform)
        {
            if (child.GetComponent<LevelNode>()) Destroy(child.gameObject);
        }

        topFog.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.8f);
        bottomFog.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.8f);
        backgroundImage.sprite = bg;

        if (levelBatch.Count == 0) return;

        for (int i = 0; i < levelBatch.Count; i++)
        {
            float t;
            if (levelBatch.Count > 1)
            {
                t = (float)i / (levelBatch.Count - 1);
            }
            else
            {
                // FIX: If it's the only node in the batch, 
                // put it at the START (0) of the map, not the middle (0.5).
                t = 0f;
            }

            Vector3 pos = path.GetPointOnPath(t);
            GameObject go = Instantiate(prefab, transform);
            go.transform.position = pos;

            go.GetComponent<LevelNode>().Setup(levelBatch[i], startLevelIndex + i);
        }
    }
}