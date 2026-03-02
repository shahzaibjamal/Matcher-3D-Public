using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapChunk : MonoBehaviour
{
    public RectTransform RectTransform;
    public MapPath path; // The waypoints for this specific map segment
    private List<LevelNode> _activeNodes = new List<LevelNode>();
    // Inside MapChunk.cs
    [SerializeField] private Transform _nodeParent;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _topFog;
    [SerializeField] private Image _bottomFog;
    [SerializeField] private GameObject _lockOverlay;
    [SerializeField] private TMP_Text _starRequirementText;
    [SerializeField] private CanvasGroup _canvasGroup;


    // Call this when the chunk is "Recycled" to the top or bottom
    public void Configure(List<LevelDisplayData> levelBatch, int startLevelIndex, GameObject prefab, string bgName, string themeColor)
    {
        // Clear old nodes
        foreach (Transform child in transform)
        {
            if (child.GetComponent<LevelNode>()) Destroy(child.gameObject);
        }
        Color fogColor;
        if (ColorUtility.TryParseHtmlString(themeColor, out fogColor))
        {
            _topFog.color = fogColor;
            _bottomFog.color = fogColor;
        }
        else
        {
            Debug.LogError("Invalid hex color string");
        }

        AssetLoader.Instance.LoadIcon(bgName, (sprite) =>
        {
            _backgroundImage.sprite = sprite;
        });

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
            GameObject go = Instantiate(prefab, _nodeParent);
            go.transform.position = pos;

            go.GetComponent<LevelNode>().Setup(levelBatch[i], startLevelIndex + i);
        }
    }

    public void ShowLockedOverlay(bool show, string text)
    {
        _lockOverlay.SetActive(show);
        _starRequirementText.text = text;
        // _canvasGroup.alpha = show ? 0.6f : 1.0f;
    }
}