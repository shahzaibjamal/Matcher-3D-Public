using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteMapManager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private MapChunk[] chunks; // The 2 map objects you have
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private int nodesPerMap = 15;

    private int _topChunkDataIndex = 0; // The level index the top-most map starts with
    private float _chunkHeight;

    void Start()
    {
        _chunkHeight = chunks[0].rectTransform.rect.height;
        RefreshAllChunks();
    }
    private void RefreshAllChunks()
    {
        int totalLevels = LevelManager.Instance.GetLevelSelectData().Count;
        float totalPages = Mathf.Ceil((float)totalLevels / nodesPerMap);

        // Only allow the content to be big enough for the levels you HAVE
        content.sizeDelta = new Vector2(content.sizeDelta.x, totalPages * _chunkHeight);

        // If levels fit on one map, we don't need the second chunk active
        if (totalLevels <= nodesPerMap)
        {
            chunks[1].gameObject.SetActive(false);
        }
        // 3. Position the chunks so they are stacked, not overlapping
        for (int i = 0; i < chunks.Length; i++)
        {
            // Chunk 0 stays at 0. Chunk 1 starts at _chunkHeight (e.g., 1920).
            chunks[i].rectTransform.anchoredPosition = new Vector2(0, i * _chunkHeight);

            int startIndex = i * nodesPerMap;
            var data = LevelManager.Instance.GetLevelBatch(startIndex, nodesPerMap);

            if (data.Count > 0)
            {

                // Get theme based on the Page Number
                var theme = Metadata.Instance.levelDatabase.GetThemeByMapIndex(startIndex);
                chunks[i].gameObject.SetActive(true);
                // Reusing your existing logic but passing null/current sprite for now
                chunks[i].Configure(data, startIndex, nodePrefab, theme.backgroundSprite, theme.fogColor);
            }
            else
            {
                // If there's no data even for the second chunk, hide it
                chunks[i].gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        float buffer = _chunkHeight * 0.1f; // 10% overlap safety

        foreach (var chunk in chunks)
        {
            float localY = content.anchoredPosition.y + chunk.rectTransform.anchoredPosition.y;

            // If scrolling UP: Move to TOP only when it is WELL below the screen
            if (localY < -(_chunkHeight + buffer))
            {
                if (CanScrollFurther(chunk, true))
                    RepositionChunk(chunk, true);
            }
            // If scrolling DOWN: Move to BOTTOM only when it is WELL above the screen
            else if (localY > (_chunkHeight + buffer))
            {
                if (CanScrollFurther(chunk, false))
                    RepositionChunk(chunk, false);
            }
        }
    }

    private bool CanScrollFurther(MapChunk chunk, bool goingUp)
    {
        float currentY = chunk.rectTransform.anchoredPosition.y;
        if (goingUp)
        {
            // Don't teleport up if we are already at the very top of the Content
            return (currentY + (_chunkHeight * chunks.Length)) < content.sizeDelta.y;
        }
        else
        {
            // Don't teleport down if we are already at the bottom (0)
            return currentY > 0;
        }
    }

    private void RepositionChunk(MapChunk chunk, bool toTop)
    {
        float moveAmount = _chunkHeight * chunks.Length;
        float newY = chunk.rectTransform.anchoredPosition.y + (toTop ? moveAmount : -moveAmount);

        // Set the position FIRST
        chunk.rectTransform.anchoredPosition = new Vector2(0, newY);

        // Then calculate data based on that NEW position
        int newStartIndex = CalculateNewIndex(chunk);
        var data = LevelManager.Instance.GetLevelBatch(newStartIndex, nodesPerMap);

        // chunk.Configure(data, newStartIndex, nodePrefab);

        int mapIndex = Mathf.RoundToInt(chunk.rectTransform.anchoredPosition.y / _chunkHeight);

        // Get theme based on the Page Number
        var theme = Metadata.Instance.levelDatabase.GetThemeByMapIndex(mapIndex);

        // Get the levels for this specific page
        // int startIndex = mapIndex * nodesPerMap;
        // var data = LevelManager.Instance.GetLevelBatch(startIndex, nodesPerMap);

        chunk.Configure(data, newStartIndex, nodePrefab, theme.backgroundSprite, theme.fogColor);
    }
    private int CalculateNewIndex(MapChunk chunk)
    {
        // Now using positive Y because we are building UP
        float yPos = chunk.rectTransform.anchoredPosition.y;
        int mapSequenceIndex = Mathf.RoundToInt(yPos / _chunkHeight);

        return mapSequenceIndex * nodesPerMap;
    }
}